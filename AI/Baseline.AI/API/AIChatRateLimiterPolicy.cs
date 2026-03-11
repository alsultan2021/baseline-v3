using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Baseline.AI.API;

/// <summary>
/// Configures rate limiting for AI chat endpoints.
/// Uses a sliding window per IP to prevent abuse.
/// </summary>
public sealed class AIChatRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string POLICY_NAME = "BaselineAIChatPolicy";

    private readonly BaselineAIOptions _options;

    public AIChatRateLimiterPolicy(IOptions<BaselineAIOptions> options)
    {
        _options = options.Value;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)_options.Chatbot.RateLimitWindowSeconds).ToString();
            return ValueTask.CompletedTask;
        };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(remoteIp, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = _options.Chatbot.RateLimitPermitCount,
                Window = TimeSpan.FromSeconds(_options.Chatbot.RateLimitWindowSeconds),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }
}
