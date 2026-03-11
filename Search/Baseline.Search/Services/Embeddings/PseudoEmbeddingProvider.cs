namespace Baseline.Search;

/// <summary>
/// Hash-based pseudo-embedding provider for development/demo.
/// Produces deterministic but non-semantic vectors.
/// </summary>
public sealed class PseudoEmbeddingProvider(SemanticSearchOptions options) : IEmbeddingProvider
{
    public Task<float[]> GenerateEmbeddingAsync(string text)
        => Task.FromResult(Generate(text, options.EmbeddingDimensions));

    public Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts)
    {
        IReadOnlyList<float[]> result = texts
            .Select(t => Generate(t, options.EmbeddingDimensions))
            .ToList();
        return Task.FromResult(result);
    }

    private static float[] Generate(string text, int dimensions)
    {
        var embedding = new float[dimensions];
        var random = new Random(text.GetHashCode());

        for (int i = 0; i < dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        // L2-normalise
        var norm = MathF.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < dimensions; i++)
            {
                embedding[i] /= norm;
            }
        }

        return embedding;
    }
}
