using Microsoft.AspNetCore.Identity;
using Pgvector;

namespace RagWorkspace.Api.Models;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = [];
}

public sealed class Workspace
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<WorkspaceMember> Members { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
}

public sealed class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = "Member";
}

public sealed class Document
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public string ProcessingStatus { get; set; } = "Ready";
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ContentHash { get; set; } = string.Empty;
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}

public sealed class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public required string Content { get; set; }
    public int? PageNumber { get; set; }
    public Vector? Embedding { get; set; }
    public string EmbeddingData { get; set; } = string.Empty;
}

public sealed class ChatSession
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public required string Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = [];
}

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Citation> Citations { get; set; } = [];
}

public sealed class Citation
{
    public Guid Id { get; set; }
    public Guid ChatMessageId { get; set; }
    public ChatMessage ChatMessage { get; set; } = null!;
    public Guid DocumentChunkId { get; set; }
    public DocumentChunk DocumentChunk { get; set; } = null!;
    public required string Excerpt { get; set; }
}
