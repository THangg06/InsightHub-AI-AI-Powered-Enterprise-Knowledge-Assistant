using System.Collections.Concurrent;

namespace RagWorkspace.Api.Services;

public sealed record DocumentRecord(Guid Id, string FileName, string Content, DateTimeOffset UploadedAt);

public sealed class WorkspaceStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, DocumentRecord>> workspaces = new();

    public bool IsAuthorized(string workspaceId, string? token) =>
        !string.IsNullOrWhiteSpace(workspaceId) && token == workspaceId;

    public IReadOnlyCollection<DocumentRecord> GetDocuments(string workspaceId) =>
        workspaces.TryGetValue(workspaceId, out var documents)
            ? documents.Values.ToArray()
            : Array.Empty<DocumentRecord>();

    public DocumentRecord AddDocument(string workspaceId, string fileName, string content)
    {
        var document = new DocumentRecord(Guid.NewGuid(), fileName, content, DateTimeOffset.UtcNow);
        var documents = workspaces.GetOrAdd(workspaceId, _ => new ConcurrentDictionary<Guid, DocumentRecord>());
        documents[document.Id] = document;
        return document;
    }
}

