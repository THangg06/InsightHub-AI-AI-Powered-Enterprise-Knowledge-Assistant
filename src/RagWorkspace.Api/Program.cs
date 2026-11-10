using RagWorkspace.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WorkspaceStore>();
builder.Services.AddSingleton<RetrievalService>();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(section => section.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Cast<string>()
    .ToArray();
if (allowedOrigins.Length == 0)
    allowedOrigins = ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/workspaces/{workspaceId}/documents", (
    string workspaceId,
    WorkspaceStore store,
    HttpContext context) =>
{
    if (!store.IsAuthorized(workspaceId, context.Request.Headers["X-Workspace-Token"]))
        return Results.Unauthorized();
    return Results.Ok(store.GetDocuments(workspaceId).Select(DocumentResponse.From));
});

app.MapPost("/api/workspaces/{workspaceId}/documents", async (
    string workspaceId,
    HttpRequest request,
    WorkspaceStore store,
    HttpContext context) =>
{
    if (!store.IsAuthorized(workspaceId, context.Request.Headers["X-Workspace-Token"]))
        return Results.Unauthorized();
    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "multipart/form-data is required." });

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "A non-empty text file is required." });
    if (file.Length > 2 * 1024 * 1024)
        return Results.BadRequest(new { error = "The maximum document size is 2 MB." });
    var extension = Path.GetExtension(file.FileName);
    if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only .txt, .md, and .csv files are supported." });

    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(content))
        return Results.BadRequest(new { error = "The document cannot be empty." });

    var document = store.AddDocument(workspaceId, Path.GetFileName(file.FileName), content);
    return Results.Created($"/api/workspaces/{workspaceId}/documents", DocumentResponse.From(document));
});

app.MapPost("/api/workspaces/{workspaceId}/ask", (
    string workspaceId,
    AskRequest? request,
    WorkspaceStore store,
    RetrievalService retrieval,
    HttpContext context) =>
{
    if (!store.IsAuthorized(workspaceId, context.Request.Headers["X-Workspace-Token"]))
        return Results.Unauthorized();
    if (request is null || string.IsNullOrWhiteSpace(request.Question))
        return Results.BadRequest(new { error = "Question is required." });

    return Results.Ok(retrieval.Answer(workspaceId, request.Question, store.GetDocuments(workspaceId)));
});

app.Run();

public sealed record AskRequest(string Question);
public sealed record DocumentResponse(Guid Id, string FileName, DateTimeOffset UploadedAt)
{
    public static DocumentResponse From(DocumentRecord document) =>
        new(document.Id, document.FileName, document.UploadedAt);
}

public partial class Program
{
}
