namespace RagWorkspace.Api.Services;

public sealed record Citation(Guid DocumentId, string FileName, string Excerpt);
public sealed record AnswerResponse(string Answer, IReadOnlyCollection<Citation> Citations);

public sealed class RetrievalService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "are", "for", "how", "what", "when", "where", "does", "can", "with"
    };

    public AnswerResponse Answer(
        string workspaceId,
        string question,
        IEnumerable<DocumentRecord> documents)
    {
        var terms = question.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(term => term.Trim('.', ',', '?', '!', ':', ';').ToLowerInvariant())
            .Where(term => term.Length >= 3 && !StopWords.Contains(term))
            .Distinct()
            .ToArray();

        var matches = documents
            .Select(document => new
            {
                Document = document,
                Score = terms.Count(term => document.Content.Contains(term, StringComparison.OrdinalIgnoreCase))
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Take(3)
            .ToArray();

        if (matches.Length == 0)
            return new AnswerResponse(
                "I could not find an answer in the documents available to this workspace.",
                Array.Empty<Citation>());

        var citations = matches
            .Select(item => new Citation(
                item.Document.Id,
                item.Document.FileName,
                BuildExcerpt(item.Document.Content, terms)))
            .ToArray();
        return new AnswerResponse(
            $"Based on the uploaded documents, the most relevant information is in {string.Join(", ", citations.Select(citation => citation.FileName))}.",
            citations);
    }

    private static string BuildExcerpt(string content, IReadOnlyCollection<string> terms)
    {
        var sentence = content.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(candidate => terms.Any(term => candidate.Contains(term, StringComparison.OrdinalIgnoreCase)));
        return (sentence ?? content).Trim()[..Math.Min(240, (sentence ?? content).Trim().Length)];
    }
}
