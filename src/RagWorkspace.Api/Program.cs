using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RagWorkspace.Api.Data;
using RagWorkspace.Api.Models;
using RagWorkspace.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=ragworkspace;Username=rag;Password=rag";
var provider = builder.Configuration["Database:Provider"] ?? "InMemory";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(connectionString, npgsql => npgsql.UseVector());
    else
        options.UseInMemoryDatabase("rag-workspace");
});
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
}).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "development-only-key-change-me-32-characters";
if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 characters.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<DocumentProcessingService>();
builder.Services.AddScoped<IEmbeddingService, DeterministicEmbeddingService>();
builder.Services.AddHttpClient("gemini", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<AnswerGenerationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").GetChildren()
    .Select(section => section.Value).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins.Length == 0 ? ["http://localhost:5173"] : allowedOrigins)
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        await db.Database.MigrateAsync();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", database = provider }));

app.MapPost("/api/auth/register", async (RegisterRequest request, UserManager<ApplicationUser> users, JwtTokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 8)
        return Results.BadRequest(new { error = "A valid email and password of at least 8 characters are required." });
    var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = request.Email, Email = request.Email };
    var result = await users.CreateAsync(user, request.Password);
    if (!result.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(error => error.Description)) });
    return Results.Ok(new AuthResponse(tokens.Create(user), user.Id, user.Email));
});

app.MapPost("/api/auth/login", async (LoginRequest request, UserManager<ApplicationUser> users, JwtTokenService tokens) =>
{
    var user = await users.FindByEmailAsync(request.Email);
    if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        return Results.Unauthorized();
    return Results.Ok(new AuthResponse(tokens.Create(user), user.Id, user.Email));
});

app.MapGet("/api/auth/me", async (ClaimsPrincipal principal, UserManager<ApplicationUser> users) =>
{
    var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var user = Guid.TryParse(id, out var userId) ? await users.FindByIdAsync(userId.ToString()) : null;
    return user is null ? Results.Unauthorized() : Results.Ok(new { user.Id, user.Email });
}).RequireAuthorization();

app.MapPost("/api/workspaces", async (CreateWorkspaceRequest request, ClaimsPrincipal principal, ApplicationDbContext db) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Workspace name is required." });
    var workspace = new Workspace { Id = Guid.NewGuid(), Name = request.Name.Trim() };
    workspace.Members.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = userId, Role = "Owner" });
    db.Workspaces.Add(workspace);
    await db.SaveChangesAsync();
    return Results.Created($"/api/workspaces/{workspace.Id}", new { workspace.Id, workspace.Name });
}).RequireAuthorization();

app.MapGet("/api/workspaces/{workspaceId:guid}/documents", async (Guid workspaceId, string? search, ClaimsPrincipal principal, ApplicationDbContext db) =>
{
    if (!await IsMemberAsync(workspaceId, principal, db)) return Results.Unauthorized();
    var query = db.Documents.Where(document => document.WorkspaceId == workspaceId);
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(document => document.FileName.Contains(search));
    var documents = await query
        .Select(document => new DocumentResponse(document.Id, document.FileName, document.ContentType, document.ProcessingStatus, document.UploadedAt))
        .ToListAsync();
    return Results.Ok(documents);
}).RequireAuthorization();

app.MapPost("/api/workspaces/{workspaceId:guid}/documents", async (
    Guid workspaceId, HttpRequest request, ClaimsPrincipal principal, ApplicationDbContext db,
    DocumentProcessingService processing, IEmbeddingService embeddings, CancellationToken cancellationToken) =>
{
    if (!await IsMemberAsync(workspaceId, principal, db)) return Results.Unauthorized();
    if (!request.HasFormContentType) return Results.BadRequest(new { error = "multipart/form-data is required." });
    var file = (await request.ReadFormAsync(cancellationToken)).Files.GetFile("file");
    if (file is null || file.Length == 0) return Results.BadRequest(new { error = "A non-empty file is required." });
    if (file.Length > 10 * 1024 * 1024) return Results.BadRequest(new { error = "The maximum document size is 10 MB." });
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (extension is not (".pdf" or ".docx" or ".txt" or ".md" or ".csv"))
        return Results.BadRequest(new { error = "Only PDF, DOCX, TXT, MD, and CSV files are supported." });
    ExtractedDocument extracted;
    try { await using var stream = file.OpenReadStream(); extracted = await processing.ExtractAsync(file.FileName, stream, cancellationToken); }
    catch (Exception exception) when (exception is InvalidDataException or IOException)
    { return Results.BadRequest(new { error = $"Document processing failed: {exception.Message}" }); }
    if (string.IsNullOrWhiteSpace(extracted.Text)) return Results.BadRequest(new { error = "The document contains no text." });
    var hash = DocumentProcessingService.Hash(extracted.Text);
    if (await db.Documents.AnyAsync(document => document.WorkspaceId == workspaceId && document.ContentHash == hash, cancellationToken))
        return Results.Conflict(new { error = "This document already exists in the workspace." });
    var document = new Document { Id = Guid.NewGuid(), WorkspaceId = workspaceId, FileName = Path.GetFileName(file.FileName), ContentType = file.ContentType ?? "application/octet-stream", ContentHash = hash };
    var chunks = DocumentProcessingService.Chunk(extracted.Text);
    for (var index = 0; index < chunks.Count; index++)
    {
        var embedding = await embeddings.CreateAsync(chunks[index], cancellationToken);
        document.Chunks.Add(new DocumentChunk { Id = Guid.NewGuid(), DocumentId = document.Id, ChunkIndex = index, Content = chunks[index], Embedding = embedding, EmbeddingData = string.Join(",", embedding.ToArray()) });
    }
    db.Documents.Add(document);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/workspaces/{workspaceId}/documents/{document.Id}", new DocumentResponse(document.Id, document.FileName, document.ContentType, document.ProcessingStatus, document.UploadedAt));
}).RequireAuthorization();

app.MapPost("/api/workspaces/{workspaceId:guid}/ask", async (
    Guid workspaceId, AskRequest request, ClaimsPrincipal principal, ApplicationDbContext db,
    IEmbeddingService embeddings, AnswerGenerationService answerGeneration, CancellationToken cancellationToken) =>
{
    if (!await IsMemberAsync(workspaceId, principal, db)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Question)) return Results.BadRequest(new { error = "Question is required." });
    var query = await embeddings.CreateAsync(request.Question, cancellationToken);
    var chunks = await db.DocumentChunks.Include(chunk => chunk.Document)
        .Where(chunk => chunk.Document.WorkspaceId == workspaceId)
        .ToListAsync(cancellationToken);
    var matches = chunks.Select(chunk => new { Chunk = chunk, Score = Similarity(query, chunk.Embedding ?? ParseEmbedding(chunk.EmbeddingData)) })
        .Where(match => match.Score > 0.05).OrderByDescending(match => match.Score).Take(5).ToArray();
    if (matches.Length == 0) return Results.Ok(new AnswerResponse("I could not find an answer in the workspace documents.", []));
    var session = new ChatSession { Id = Guid.NewGuid(), WorkspaceId = workspaceId, UserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!), Title = request.Question[..Math.Min(80, request.Question.Length)] };
    string generatedAnswer;
    try { generatedAnswer = await answerGeneration.GenerateAsync(request.Question, matches.Select(match => match.Chunk.Content).ToArray(), cancellationToken); }
    catch (InvalidOperationException exception) { return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway); }
    var message = new ChatMessage { Id = Guid.NewGuid(), ChatSessionId = session.Id, Role = "assistant", Content = generatedAnswer };
    foreach (var match in matches) message.Citations.Add(new RagWorkspace.Api.Models.Citation { Id = Guid.NewGuid(), ChatMessageId = message.Id, DocumentChunkId = match.Chunk.Id, Excerpt = match.Chunk.Content[..Math.Min(240, match.Chunk.Content.Length)] });
    session.Messages.Add(message);
    db.ChatSessions.Add(session);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new AnswerResponse(message.Content, message.Citations.Select(citation => new CitationResponse(citation.DocumentChunk.DocumentId, citation.DocumentChunk.Document.FileName, citation.Excerpt)).ToArray()));
}).RequireAuthorization();

app.MapGet("/api/workspaces/{workspaceId:guid}/chats", async (Guid workspaceId, ClaimsPrincipal principal, ApplicationDbContext db) =>
{
    if (!await IsMemberAsync(workspaceId, principal, db)) return Results.Unauthorized();
    return Results.Ok(await db.ChatSessions.Where(session => session.WorkspaceId == workspaceId).OrderByDescending(session => session.CreatedAt)
        .Select(session => new { session.Id, session.Title, session.CreatedAt }).ToListAsync());
}).RequireAuthorization();

app.MapGet("/api/chats/{chatId:guid}", async (Guid chatId, ClaimsPrincipal principal, ApplicationDbContext db) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var chat = await db.ChatSessions.Include(session => session.Messages).ThenInclude(message => message.Citations)
        .SingleOrDefaultAsync(session => session.Id == chatId && session.UserId.ToString() == userId);
    return chat is null ? Results.NotFound() : Results.Ok(chat.Messages.OrderBy(message => message.CreatedAt).Select(message => new { message.Role, message.Content, message.CreatedAt, Citations = message.Citations.Select(citation => new CitationResponse(citation.DocumentChunk.DocumentId, citation.DocumentChunk.Document.FileName, citation.Excerpt)) }));
}).RequireAuthorization();

app.MapDelete("/api/chats/{chatId:guid}", async (Guid chatId, ClaimsPrincipal principal, ApplicationDbContext db) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var chat = await db.ChatSessions.SingleOrDefaultAsync(session => session.Id == chatId && session.UserId.ToString() == userId);
    if (chat is null) return Results.NotFound();
    db.ChatSessions.Remove(chat);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/api/documents/{documentId:guid}", async (Guid documentId, ClaimsPrincipal principal, ApplicationDbContext db) =>
{
    var document = await db.Documents.FindAsync(documentId);
    if (document is null || !await IsMemberAsync(document.WorkspaceId, principal, db)) return Results.NotFound();
    db.Documents.Remove(document);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

static async Task<bool> IsMemberAsync(Guid workspaceId, ClaimsPrincipal principal, ApplicationDbContext db)
{
    var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(claim, out var userId) && await db.WorkspaceMembers.AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId);
}

static double Similarity(Pgvector.Vector query, Pgvector.Vector? candidate)
{
    if (candidate is null) return 0;
    var left = query.ToArray(); var right = candidate.ToArray();
    var dot = 0d; var leftNorm = 0d; var rightNorm = 0d;
    for (var index = 0; index < left.Length; index++) { dot += left[index] * right[index]; leftNorm += left[index] * left[index]; rightNorm += right[index] * right[index]; }
    return leftNorm == 0 || rightNorm == 0 ? 0 : dot / Math.Sqrt(leftNorm * rightNorm);
}

static Pgvector.Vector? ParseEmbedding(string data)
{
    if (string.IsNullOrWhiteSpace(data)) return null;
    var values = data.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray();
    return values.Length == 384 ? new Pgvector.Vector(values) : null;
}

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record CreateWorkspaceRequest(string Name);
public sealed record AskRequest(string Question);
public sealed record AuthResponse(string Token, Guid UserId, string? Email);
public sealed record DocumentResponse(Guid Id, string FileName, string ContentType, string ProcessingStatus, DateTimeOffset UploadedAt);
public sealed record CitationResponse(Guid DocumentId, string FileName, string Excerpt);
public sealed record AnswerResponse(string Answer, IReadOnlyCollection<CitationResponse> Citations);
public partial class Program { }
