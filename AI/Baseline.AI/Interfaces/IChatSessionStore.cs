namespace Baseline.AI;

/// <summary>
/// Abstraction for chat session persistence.
/// Allows swapping between in-memory (dev) and distributed (production) stores.
/// </summary>
public interface IChatSessionStore
{
    /// <summary>
    /// Gets a session by ID, or null if not found.
    /// </summary>
    Task<ChatSessionData?> GetAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a session.
    /// </summary>
    Task SetAsync(string sessionId, ChatSessionData session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a session.
    /// </summary>
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Serializable chat session data.
/// </summary>
public sealed class ChatSessionData
{
    /// <summary>Session identifier.</summary>
    public required string Id { get; init; }

    /// <summary>When the session was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Conversation messages.</summary>
    public List<ChatMessage> Messages { get; init; } = [];
}
