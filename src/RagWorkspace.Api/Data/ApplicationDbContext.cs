using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using RagWorkspace.Api.Models;

namespace RagWorkspace.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Citation> Citations => Set<Citation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("vector");
        builder.Entity<Workspace>().HasIndex(workspace => workspace.Name).IsUnique();
        builder.Entity<Document>().HasIndex(document => new { document.WorkspaceId, document.ContentHash }).IsUnique();
        builder.Entity<WorkspaceMember>().HasKey(member => new { member.WorkspaceId, member.UserId });
        builder.Entity<WorkspaceMember>().HasOne(member => member.Workspace).WithMany(workspace => workspace.Members).HasForeignKey(member => member.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WorkspaceMember>().HasOne(member => member.User).WithMany(user => user.WorkspaceMemberships).HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Document>().HasOne(document => document.Workspace).WithMany(workspace => workspace.Documents).HasForeignKey(document => document.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<DocumentChunk>().HasOne(chunk => chunk.Document).WithMany(document => document.Chunks).HasForeignKey(chunk => chunk.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ChatSession>().HasOne(session => session.Workspace).WithMany(workspace => workspace.ChatSessions).HasForeignKey(session => session.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ChatMessage>().HasOne(message => message.ChatSession).WithMany(session => session.Messages).HasForeignKey(message => message.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Citation>().HasOne(citation => citation.ChatMessage).WithMany(message => message.Citations).HasForeignKey(citation => citation.ChatMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Citation>().HasOne(citation => citation.DocumentChunk).WithMany().HasForeignKey(citation => citation.DocumentChunkId).OnDelete(DeleteBehavior.Restrict);
        if (Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            builder.Entity<DocumentChunk>().Ignore(chunk => chunk.Embedding);
        else
            builder.Entity<DocumentChunk>().Property(chunk => chunk.Embedding).HasColumnType("vector(384)");
    }
}
