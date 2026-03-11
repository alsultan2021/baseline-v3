using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI.Services;

/// <summary>
/// Distributed cache-backed session store for multi-instance production deployments.
/// Uses IDistributedCache (Redis, SQL Server, NCache, etc.).
/// </summary>
internal sealed class DistributedChatSessionStore(
    IDistributedCache cache,
    IOptions<BaselineAIOptions> options,
    ILogger<DistributedChatSessionStore> logger) : IChatSessionStore
{
    private const string KEY_PREFIX = "baseline:ai:session:";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<ChatSessionData?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await cache.GetStringAsync(KEY_PREFIX + sessionId, cancellationToken);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ChatSessionData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get session {SessionId} from distributed cache", sessionId);
            return null;
        }
    }

    public async Task SetAsync(string sessionId, ChatSessionData session, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(session, _jsonOptions);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(options.Value.Chatbot.SessionTimeoutMinutes)
            };

            await cache.SetStringAsync(KEY_PREFIX + sessionId, json, cacheOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set session {SessionId} in distributed cache", sessionId);
        }
    }

    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(KEY_PREFIX + sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove session {SessionId} from distributed cache", sessionId);
        }
    }
}
