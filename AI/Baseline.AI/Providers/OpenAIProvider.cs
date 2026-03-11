using System.ClientModel;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Baseline.AI.Providers;

/// <summary>
/// OpenAI provider implementing <see cref="IAIProvider"/> using the official OpenAI .NET SDK.
/// Supports both OpenAI (api.openai.com) and Azure OpenAI endpoints.
/// </summary>
public sealed class OpenAIProvider(
    IOptions<BaselineAIOptions> options,
    ILogger<OpenAIProvider> logger) : IAIProvider
{
    private readonly BaselineAIOptions _options = options.Value;
    private readonly ILogger<OpenAIProvider> _logger = logger;

    private OpenAIClient? _client;
    private ChatClient? _chatClient;
    private EmbeddingClient? _embeddingClient;

    /// <inheritdoc />
    public string ProviderName => _options.Provider switch
    {
        AIProviderType.Azure => "AzureOpenAI",
        _ => "OpenAI"
    };

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return false;
        }

        try
        {
            var client = GetEmbeddingClient();
            // Quick health check with minimal tokens
            var result = await client.GenerateEmbeddingAsync("test", cancellationToken: cancellationToken);
            return result?.Value is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI provider availability check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var client = GetEmbeddingClient();

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.EmbeddingDimensions
        };

        var result = await client.GenerateEmbeddingAsync(text, embeddingOptions, cancellationToken);
        return result.Value.ToFloats().ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        if (textList.Count == 0)
        {
            return [];
        }

        var client = GetEmbeddingClient();

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.EmbeddingDimensions
        };

        var result = await client.GenerateEmbeddingsAsync(textList, embeddingOptions, cancellationToken);

        return result.Value
            .OrderBy(e => e.Index)
            .Select(e => e.ToFloats().ToArray())
            .ToList();
    }

    /// <inheritdoc />
    public async Task<AIResponse> GenerateChatCompletionAsync(
        IEnumerable<AIChatMessage> messages,
        AICompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var client = GetChatClient();
        var chatMessages = MapMessages(messages);
        var chatOptions = MapOptions(options);

        ClientResult<ChatCompletion> result = await client.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);
        var completion = result.Value;

        return new AIResponse
        {
            Content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty,
            FinishReason = MapFinishReason(completion.FinishReason),
            Model = completion.Model,
            Usage = completion.Usage is not null
                ? new AITokenUsage
                {
                    PromptTokens = completion.Usage.InputTokenCount,
                    CompletionTokens = completion.Usage.OutputTokenCount
                }
                : null
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AIStreamChunk> GenerateChatCompletionStreamingAsync(
        IEnumerable<AIChatMessage> messages,
        AICompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = GetChatClient();
        var chatMessages = MapMessages(messages);
        var chatOptions = MapOptions(options);

        AsyncCollectionResult<StreamingChatCompletionUpdate> stream =
            client.CompleteChatStreamingAsync(chatMessages, chatOptions, cancellationToken);

        await foreach (var update in stream)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var content = update.ContentUpdate?.FirstOrDefault()?.Text;
            bool isComplete = update.FinishReason.HasValue;

            yield return new AIStreamChunk
            {
                Content = content,
                IsComplete = isComplete,
                FinishReason = isComplete ? MapFinishReason(update.FinishReason!.Value) : null
            };
        }
    }

    private OpenAIClient GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "BaselineAI:ApiKey is not configured. Set it in appsettings.json or environment variables.");
        }

        var credential = new ApiKeyCredential(_options.ApiKey);

        if (_options.Provider == AIProviderType.Azure && !string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            _client = new OpenAIClient(credential, new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.Endpoint)
            });
        }
        else if (!string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            _client = new OpenAIClient(credential, new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.Endpoint)
            });
        }
        else
        {
            _client = new OpenAIClient(credential);
        }

        return _client;
    }

    private ChatClient GetChatClient()
        => _chatClient ??= GetClient().GetChatClient(_options.ChatModel);

    private EmbeddingClient GetEmbeddingClient()
        => _embeddingClient ??= GetClient().GetEmbeddingClient(_options.EmbeddingModel);

    private static List<OpenAI.Chat.ChatMessage> MapMessages(IEnumerable<AIChatMessage> messages)
        => messages.Select<AIChatMessage, OpenAI.Chat.ChatMessage>(m => m.Role switch
        {
            AIChatRole.System => new SystemChatMessage(m.Content),
            AIChatRole.User => new UserChatMessage(m.Content),
            AIChatRole.Assistant => new AssistantChatMessage(m.Content),
            _ => new UserChatMessage(m.Content)
        }).ToList();

    private static ChatCompletionOptions? MapOptions(AICompletionOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        var chatOptions = new ChatCompletionOptions();

        if (options.MaxTokens.HasValue)
        {
            chatOptions.MaxOutputTokenCount = options.MaxTokens.Value;
        }

        if (options.Temperature.HasValue)
        {
            chatOptions.Temperature = (float)options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            chatOptions.TopP = (float)options.TopP.Value;
        }

        if (options.StopSequences is { Count: > 0 })
        {
            foreach (string stop in options.StopSequences)
            {
                chatOptions.StopSequences.Add(stop);
            }
        }

        return chatOptions;
    }

    private static AIFinishReason MapFinishReason(ChatFinishReason reason) => reason switch
    {
        ChatFinishReason.Stop => AIFinishReason.Stop,
        ChatFinishReason.Length => AIFinishReason.Length,
        ChatFinishReason.ContentFilter => AIFinishReason.ContentFilter,
        ChatFinishReason.ToolCalls => AIFinishReason.ToolCalls,
        _ => AIFinishReason.Stop
    };
}
