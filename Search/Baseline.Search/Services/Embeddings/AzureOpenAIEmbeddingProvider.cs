using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Baseline.Search;

/// <summary>
/// Embedding provider using Azure OpenAI Embeddings API.
/// Requires AzureOpenAIEndpoint, AzureOpenAIApiKey, and AzureOpenAIDeployment in config.
/// </summary>
public sealed class AzureOpenAIEmbeddingProvider(
    HttpClient httpClient,
    SemanticSearchOptions options,
    ILogger<AzureOpenAIEmbeddingProvider> logger) : IEmbeddingProvider
{
    private const string ApiVersion = "2024-02-01";

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var batch = await GenerateBatchEmbeddingsAsync([text]);
        return batch[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts)
    {
        if (string.IsNullOrWhiteSpace(options.AzureOpenAIEndpoint) ||
            string.IsNullOrWhiteSpace(options.AzureOpenAIApiKey) ||
            string.IsNullOrWhiteSpace(options.AzureOpenAIDeployment))
        {
            throw new InvalidOperationException(
                "AzureOpenAI embedding provider requires Endpoint, ApiKey, and Deployment in SemanticSearchOptions");
        }

        var endpoint = options.AzureOpenAIEndpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{options.AzureOpenAIDeployment}" +
                  $"/embeddings?api-version={ApiVersion}";

        var payload = new EmbeddingRequest
        {
            Input = texts.ToList(),
            Model = options.EmbeddingModel,
            Dimensions = options.EmbeddingDimensions
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: JsonOpts)
        };
        request.Headers.Add("api-key", options.AzureOpenAIApiKey);

        logger.LogDebug("Requesting {Count} embeddings from Azure OpenAI deployment '{Deployment}'",
            texts.Count, options.AzureOpenAIDeployment);

        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            logger.LogError("Azure OpenAI embedding request failed ({Status}): {Body}",
                (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"Azure OpenAI returned {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOpts);
        if (result?.Data is null || result.Data.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI returned unexpected data count (expected {texts.Count})");
        }

        IReadOnlyList<float[]> embeddings = result.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();

        logger.LogDebug("Received {Count} embeddings ({Tokens} tokens used)",
            embeddings.Count, result.Usage?.TotalTokens ?? 0);

        return embeddings;
    }

    // --- JSON models for Azure OpenAI Embeddings API ---

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class EmbeddingRequest
    {
        public List<string> Input { get; set; } = [];
        public string Model { get; set; } = "";
        public int? Dimensions { get; set; }
    }

    private sealed class EmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = [];
        public UsageData? Usage { get; set; }
    }

    private sealed class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = [];
    }

    private sealed class UsageData
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
