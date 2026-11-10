using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RagWorkspace.Api.Services;

public sealed class AnswerGenerationService(IConfiguration configuration, IHttpClientFactory clients)
{
    public async Task<string> GenerateAsync(string question, IReadOnlyCollection<string> context, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return $"Relevant evidence was found in the uploaded documents: {context.FirstOrDefault() ?? "No evidence was available."}";
        var model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        var prompt = $"Answer only from the evidence below. If evidence is insufficient, say so.\\nEvidence:\\n{string.Join("\\n---\\n", context)}\\nQuestion: {question}";
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}")
        {
            Content = JsonContent.Create(new { contents = new[] { new { parts = new[] { new { text = prompt } } } } })
        };
        using var response = await clients.CreateClient("gemini").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini request failed with status {(int)response.StatusCode}.");
        var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
        return payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Gemini returned an empty answer.");
    }

    private sealed record GeminiResponse([property: JsonPropertyName("candidates")] Candidate[]? Candidates);
    private sealed record Candidate([property: JsonPropertyName("content")] GeminiContent? Content);
    private sealed record GeminiContent([property: JsonPropertyName("parts")] GeminiPart[]? Parts);
    private sealed record GeminiPart([property: JsonPropertyName("text")] string? Text);
}
