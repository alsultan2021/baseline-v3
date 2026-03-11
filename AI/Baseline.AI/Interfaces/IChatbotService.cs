namespace Baseline.AI;

/// <summary>
/// Interface for chatbot service - handles conversational AI interactions.
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Processes a chat message and returns a response.
    /// </summary>
    /// <param name="message">User message.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Chatbot response.</returns>
    Task<ChatbotResponse> ProcessMessageAsync(
        string message,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a chat message scoped to a knowledge base.
    /// </summary>
    Task<ChatbotResponse> ProcessMessageAsync(
        string message,
        string sessionId,
        int? knowledgeBaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a chat message with streaming response.
    /// </summary>
    IAsyncEnumerable<ChatbotStreamChunk> ProcessMessageStreamingAsync(
        string message,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a chat message with streaming response scoped to a knowledge base.
    /// </summary>
    IAsyncEnumerable<ChatbotStreamChunk> ProcessMessageStreamingAsync(
        string message,
        string sessionId,
        int? knowledgeBaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conversation history for a session.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears conversation history for a session.
    /// </summary>
    Task ClearHistoryAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested questions based on current context.
    /// </summary>
    Task<IReadOnlyList<string>> GetSuggestedQuestionsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    Task<string> CreateSessionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Chatbot response.
/// </summary>
public sealed class ChatbotResponse
{
    /// <summary>
    /// Response message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Source documents used.
    /// </summary>
    public IReadOnlyList<AISource> Sources { get; init; } = [];

    /// <summary>
    /// Suggested follow-up questions.
    /// </summary>
    public IReadOnlyList<string> SuggestedQuestions { get; init; } = [];

    /// <summary>
    /// Confidence score.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Session ID.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Message ID.
    /// </summary>
    public required string MessageId { get; init; }
}

/// <summary>
/// Streaming chunk for chatbot response.
/// </summary>
public sealed class ChatbotStreamChunk
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
    /// Sources (only on final chunk).
    /// </summary>
    public IReadOnlyList<AISource>? Sources { get; init; }

    /// <summary>
    /// Suggested questions (only on final chunk).
    /// </summary>
    public IReadOnlyList<string>? SuggestedQuestions { get; init; }
}

/// <summary>
/// Chat message in conversation history.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// Message ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Role (user or assistant).
    /// </summary>
    public required ChatMessageRole Role { get; init; }

    /// <summary>
    /// Message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Sources used (for assistant messages).
    /// </summary>
    public IReadOnlyList<AISource>? Sources { get; init; }
}

/// <summary>
/// Chat message role.
/// </summary>
public enum ChatMessageRole
{
    User,
    Assistant
}
