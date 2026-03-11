using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Baseline.AI.API;

/// <summary>
/// API controller for AI chatbot interactions.
/// Rate-limited to prevent abuse of AI endpoints.
/// </summary>
[ApiController]
[Route("api/ai/chat")]
[EnableRateLimiting(AIChatRateLimiterPolicy.POLICY_NAME)]
public class AIChatController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ILogger<AIChatController> _logger;

    public AIChatController(
        IChatbotService chatbotService,
        ILogger<AIChatController> logger)
    {
        _chatbotService = chatbotService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a chat message and returns a response.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ChatResponse>> PostMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var response = await _chatbotService.ProcessMessageAsync(
                request.Message,
                request.SessionId ?? Guid.NewGuid().ToString(),
                request.KnowledgeBaseId,
                HttpContext.RequestAborted);

            return Ok(new ChatResponse
            {
                Answer = response.Message,
                SessionId = request.SessionId ?? "",
                Sources = response.Sources?.Select(s => new SourceInfo
                {
                    Title = s.Title,
                    Url = s.Url,
                    Snippet = s.Snippet
                }).ToList() ?? []
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "An error occurred processing your message" });
        }
    }

    /// <summary>
    /// Streams a chat response using Server-Sent Events.
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatRequest request)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await WriteEventAsync("error", "Message is required");
                return;
            }

            await foreach (var chunk in _chatbotService.ProcessMessageStreamingAsync(
                request.Message,
                request.SessionId ?? Guid.NewGuid().ToString(),
                request.KnowledgeBaseId,
                HttpContext.RequestAborted))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    await WriteEventAsync("content", chunk.Content);
                }

                if (chunk.IsComplete)
                {
                    if (chunk.Sources != null && chunk.Sources.Count > 0)
                    {
                        var sourcesJson = System.Text.Json.JsonSerializer.Serialize(
                            chunk.Sources.Select(s => new
                            {
                                title = s.Title,
                                url = s.Url,
                                snippet = s.Snippet
                            })
                        );
                        await WriteEventAsync("sources", sourcesJson);
                    }

                    if (chunk.SuggestedQuestions != null && chunk.SuggestedQuestions.Count > 0)
                    {
                        var suggestionsJson = System.Text.Json.JsonSerializer.Serialize(
                            chunk.SuggestedQuestions);
                        await WriteEventAsync("suggestions", suggestionsJson);
                    }

                    await WriteEventAsync("done", "");
                    break;
                }

                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming chat message");
            await WriteEventAsync("error", ex.Message);
        }
    }

    /// <summary>
    /// Gets suggested questions for the chat.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<SuggestionsResponse>> GetSuggestions([FromQuery] string? sessionId)
    {
        try
        {
            var suggestions = await _chatbotService.GetSuggestedQuestionsAsync(
                sessionId ?? Guid.NewGuid().ToString(),
                HttpContext.RequestAborted);

            return Ok(new SuggestionsResponse
            {
                Suggestions = suggestions.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new { error = "An error occurred getting suggestions" });
        }
    }

    /// <summary>
    /// Clears chat history for a session.
    /// </summary>
    [HttpDelete("history/{sessionId}")]
    public async Task<ActionResult> ClearHistory(string sessionId)
    {
        try
        {
            await _chatbotService.ClearHistoryAsync(sessionId, HttpContext.RequestAborted);
            return Ok(new { message = "History cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing history for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "An error occurred clearing history" });
        }
    }

    /// <summary>
    /// Records user feedback (thumbs up/down) for a message.
    /// </summary>
    [HttpPost("feedback")]
    [IgnoreAntiforgeryToken]
    public ActionResult SubmitFeedback([FromBody] FeedbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.MessageId))
        {
            return BadRequest(new { error = "SessionId and MessageId are required" });
        }

        _logger.LogInformation(
            "Chat feedback: session={SessionId} message={MessageId} helpful={IsHelpful}",
            request.SessionId,
            request.MessageId,
            request.IsHelpful);

        // In production, persist to a feedback store / analytics pipeline
        return Ok(new { message = "Feedback recorded" });
    }

    private async Task WriteEventAsync(string eventName, string data)
    {
        await Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"event: {eventName}\n"));
        await Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: {data}\n\n"));
    }
}

/// <summary>
/// Request model for chat messages.
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = "";
    public string? SessionId { get; set; }
    public int? KnowledgeBaseId { get; set; }
}

/// <summary>
/// Response model for chat messages.
/// </summary>
public class ChatResponse
{
    public string Answer { get; set; } = "";
    public string SessionId { get; set; } = "";
    public List<SourceInfo> Sources { get; set; } = [];
}

/// <summary>
/// Source information for citations.
/// </summary>
public class SourceInfo
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Snippet { get; set; }
}

/// <summary>
/// Response model for suggestions.
/// </summary>
public class SuggestionsResponse
{
    public List<string> Suggestions { get; set; } = [];
}

/// <summary>
/// Request model for message feedback.
/// </summary>
public class FeedbackRequest
{
    public string SessionId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public bool IsHelpful { get; set; }
}
