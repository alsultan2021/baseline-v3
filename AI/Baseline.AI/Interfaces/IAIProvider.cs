namespace Baseline.AI;

/// <summary>
/// Interface for AI provider - similar to how Lucene uses different analyzers.
/// Allows plugging in different AI backends (OpenAI, Azure, Anthropic, Ollama).
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Provider name for identification.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Generates embeddings for the given texts.
    /// </summary>
    /// <param name="texts">Texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Embeddings as float arrays.</returns>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a single embedding.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat completion.
    /// </summary>
    /// <param name="messages">Chat messages.</param>
    /// <param name="options">Completion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI response.</returns>
    Task<AIResponse> GenerateChatCompletionAsync(
        IEnumerable<AIChatMessage> messages,
        AICompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion.
    /// </summary>
    IAsyncEnumerable<AIStreamChunk> GenerateChatCompletionStreamingAsync(
        IEnumerable<AIChatMessage> messages,
        AICompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider is configured and available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Chat message for AI completion.
/// </summary>
public sealed class AIChatMessage
{
    /// <summary>
    /// Role of the message sender.
    /// </summary>
    public required AIChatRole Role { get; init; }

    /// <summary>
    /// Content of the message.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Optional name for the message sender.
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
/// Chat message roles.
/// </summary>
public enum AIChatRole
{
    System,
    User,
    Assistant,
    Tool
}

/// <summary>
/// AI completion response.
/// </summary>
public sealed class AIResponse
{
    /// <summary>
    /// The generated text content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Finish reason.
    /// </summary>
    public AIFinishReason FinishReason { get; init; }

    /// <summary>
    /// Token usage information.
    /// </summary>
    public AITokenUsage? Usage { get; init; }

    /// <summary>
    /// Model used for the response.
    /// </summary>
    public string? Model { get; init; }
}

/// <summary>
/// Streaming chunk for AI completion.
/// </summary>
public sealed class AIStreamChunk
{
    /// <summary>
    /// Delta content.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Whether this is the final chunk.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Finish reason (only on final chunk).
    /// </summary>
    public AIFinishReason? FinishReason { get; init; }
}

/// <summary>
/// AI completion finish reasons.
/// </summary>
public enum AIFinishReason
{
    Stop,
    Length,
    ContentFilter,
    ToolCalls,
    Error
}

/// <summary>
/// Token usage information.
/// </summary>
public sealed class AITokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Options for AI completion.
/// </summary>
public sealed class AICompletionOptions
{
    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature for randomness (0.0 to 2.0).
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Top-p nucleus sampling.
    /// </summary>
    public double? TopP { get; init; }

    /// <summary>
    /// Stop sequences.
    /// </summary>
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }
}
