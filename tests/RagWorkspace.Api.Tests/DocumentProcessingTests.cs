using RagWorkspace.Api.Services;

namespace RagWorkspace.Api.Tests;

public sealed class DocumentProcessingTests
{
    [Fact]
    public void Chunks_text_with_overlap_without_empty_chunks()
    {
        var chunks = DocumentProcessingService.Chunk(new string('a', 2500), 1000, 100);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Length, 1, 1000));
        Assert.DoesNotContain(chunks, string.IsNullOrWhiteSpace);
    }

    [Fact]
    public void Produces_stable_content_hashes()
    {
        var first = DocumentProcessingService.Hash("same content");
        var second = DocumentProcessingService.Hash("same content");

        Assert.Equal(first, second);
    }
}
