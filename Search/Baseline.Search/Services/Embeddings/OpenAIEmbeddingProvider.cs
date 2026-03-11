using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Baseline.Search;

/// <summary>
/// Embedding provider using direct OpenAI Embeddings API (not Azure).
/// Requires OpenAIApiKey in config.
/// </summary>
public sealed class OpenAIEmbeddingProvider(
    HttpClient httpClient,
    SemanticSearchOptions options,
    ILogger<OpenAIEmbeddingProvider> logger) : IEmbeddingProvider
{
    private const string Endpoint = "https://api.openai.com/v1/embeddings";

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var batch = await GenerateBatchEmbeddingsAsync([text]);
        return batch[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts)
    {
        if (string.IsNullOrWhiteSpace(options.OpenAIApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI embedding provider requires OpenAIApiKey in SemanticSearchOptions");
        }

        var payload = new
        {
            input = texts.ToList(),
            model = options.EmbeddingModel,
            dimensions = options.EmbeddingDimensions
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload, options: JsonOpts)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.OpenAIApiKey);

        logger.LogDebug("Requesting {Count} embeddings from OpenAI model '{Model}'",
            texts.Count, options.EmbeddingModel);

        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            logger.LogError("OpenAI embedding request failed ({Status}): {Body}",
                (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"OpenAI returned {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOpts);
        if (result?.Data is null || result.Data.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"OpenAI returned unexpected data count (expected {texts.Count})");
        }

        IReadOnlyList<float[]> embeddings = result.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();

        return embeddings;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class EmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = [];
    }

    private sealed class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = [];
    }
}
