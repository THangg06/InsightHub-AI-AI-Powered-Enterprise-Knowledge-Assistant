using Pgvector;

namespace RagWorkspace.Api.Services;

public interface IEmbeddingService
{
    Task<Vector> CreateAsync(string text, CancellationToken cancellationToken);
}

public sealed class DeterministicEmbeddingService : IEmbeddingService
{
    public Task<Vector> CreateAsync(string text, CancellationToken cancellationToken)
    {
        var values = new float[384];
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(token)) % values.Length;
            values[index] += 1;
        }
        var magnitude = MathF.Sqrt(values.Sum(value => value * value));
        if (magnitude > 0)
            for (var index = 0; index < values.Length; index++) values[index] /= magnitude;
        return Task.FromResult(new Vector(values));
    }
}
