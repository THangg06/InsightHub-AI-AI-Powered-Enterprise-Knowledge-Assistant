using RagWorkspace.Api.Services;

namespace RagWorkspace.Api.Tests;

public sealed class RetrievalServiceTests
{
    [Fact]
    public void Returns_citations_for_matching_documents()
    {
        var service = new RetrievalService();
        var document = new DocumentRecord(Guid.NewGuid(), "guide.txt", "The deployment uses GitHub Actions.", DateTimeOffset.UtcNow);

        var result = service.Answer("workspace", "How is deployment automated?", [document]);

        Assert.Single(result.Citations);
        Assert.Equal("guide.txt", result.Citations.First().FileName);
    }

    [Fact]
    public void Returns_not_found_without_citations_for_irrelevant_question()
    {
        var service = new RetrievalService();
        var document = new DocumentRecord(Guid.NewGuid(), "guide.txt", "The deployment uses GitHub Actions.", DateTimeOffset.UtcNow);

        var result = service.Answer("workspace", "What is the database password?", [document]);

        Assert.Empty(result.Citations);
        Assert.Contains("could not find", result.Answer, StringComparison.OrdinalIgnoreCase);
    }
}

