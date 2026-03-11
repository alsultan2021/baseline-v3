using System.Collections.Concurrent;

namespace Baseline.AI.Services;

/// <summary>
/// In-memory chat session store for development/single-instance deployments.
/// </summary>
internal sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private static readonly ConcurrentDictionary<string, ChatSessionData> _sessions = new();

    public Task<ChatSessionData?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task SetAsync(string sessionId, ChatSessionData session, CancellationToken cancellationToken = default)
    {
        _sessions[sessionId] = session;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
