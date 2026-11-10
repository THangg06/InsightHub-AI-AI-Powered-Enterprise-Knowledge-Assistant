using RagWorkspace.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WorkspaceStore>();
builder.Services.AddSingleton<RetrievalService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

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
    return Results.Ok(store.GetDocuments(workspaceId));
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

    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(content))
        return Results.BadRequest(new { error = "The document cannot be empty." });

    var document = store.AddDocument(workspaceId, file.FileName, content);
    return Results.Created($"/api/workspaces/{workspaceId}/documents", document);
});

app.MapPost("/api/workspaces/{workspaceId}/ask", (
    string workspaceId,
    AskRequest request,
    WorkspaceStore store,
    RetrievalService retrieval,
    HttpContext context) =>
{
    if (!store.IsAuthorized(workspaceId, context.Request.Headers["X-Workspace-Token"]))
        return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Question))
        return Results.BadRequest(new { error = "Question is required." });

    return Results.Ok(retrieval.Answer(workspaceId, request.Question, store.GetDocuments(workspaceId)));
});

app.Run();

public sealed record AskRequest(string Question);

