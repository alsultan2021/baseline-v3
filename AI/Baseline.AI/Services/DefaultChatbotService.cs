using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI;

/// <summary>
/// Default implementation of IChatbotService.
/// </summary>
internal sealed class DefaultChatbotService : IChatbotService
{
    private readonly IAISearchService _searchService;
    private readonly IAIProvider? _aiProvider;
    private readonly IChatSessionStore _sessionStore;
    private readonly BaselineAIOptions _options;
    private readonly ILogger<DefaultChatbotService> _logger;

    public DefaultChatbotService(
        IAISearchService searchService,
        IChatSessionStore sessionStore,
        IOptions<BaselineAIOptions> options,
        ILogger<DefaultChatbotService> logger,
        IAIProvider? aiProvider = null)
    {
        _searchService = searchService;
        _sessionStore = sessionStore;
        _options = options.Value;
        _logger = logger;
        _aiProvider = aiProvider;
    }

    /// <inheritdoc />
    public Task<ChatbotResponse> ProcessMessageAsync(
        string message,
        string sessionId,
        CancellationToken cancellationToken = default)
        => ProcessMessageAsync(message, sessionId, knowledgeBaseId: null, cancellationToken);

    /// <inheritdoc />
    public async Task<ChatbotResponse> ProcessMessageAsync(
        string message,
        string sessionId,
        int? knowledgeBaseId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOrCreateSessionAsync(sessionId, cancellationToken);
        var messageId = Guid.NewGuid().ToString();

        // Add user message to history
        session.Messages.Add(new ChatMessage
        {
            Id = messageId,
            Role = ChatMessageRole.User,
            Content = message,
            Timestamp = DateTime.UtcNow
        });

        try
        {
            // Get AI answer with conversation context
            var conversationHistory = BuildConversationHistory(session, _options.Chatbot.MaxHistoryMessages);

            var answerOptions = new AIAnswerOptions
            {
                TopK = _options.RAG.TopK,
                IncludeSources = _options.RAG.IncludeSourceCitations,
                ConversationHistory = conversationHistory,
                MaxTokens = 1000,
                Temperature = 0.7,
                KnowledgeBaseId = knowledgeBaseId
            };

            var answer = await _searchService.GetAnswerAsync(
                message,
                answerOptions,
                cancellationToken);

            // Add assistant response to history
            var assistantMessageId = Guid.NewGuid().ToString();
            session.Messages.Add(new ChatMessage
            {
                Id = assistantMessageId,
                Role = ChatMessageRole.Assistant,
                Content = answer.Answer,
                Timestamp = DateTime.UtcNow,
                Sources = answer.Sources
            });

            // Trim history if needed
            TrimHistory(session);
            await _sessionStore.SetAsync(sessionId, session, cancellationToken);

            // Get suggested questions
            var suggestedQuestions = await GetSuggestedQuestionsAsync(sessionId, cancellationToken);

            return new ChatbotResponse
            {
                Message = answer.Answer,
                Sources = answer.Sources,
                SuggestedQuestions = suggestedQuestions,
                Confidence = answer.Confidence,
                SessionId = sessionId,
                MessageId = assistantMessageId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot failed to process message for session {SessionId}", sessionId);

            var errorMessageId = Guid.NewGuid().ToString();
            var errorMessage = "I'm sorry, I encountered an error. Please try again.";

            session.Messages.Add(new ChatMessage
            {
                Id = errorMessageId,
                Role = ChatMessageRole.Assistant,
                Content = errorMessage,
                Timestamp = DateTime.UtcNow
            });

            await _sessionStore.SetAsync(sessionId, session, cancellationToken);

            return new ChatbotResponse
            {
                Message = errorMessage,
                Sources = [],
                SuggestedQuestions = _options.Chatbot.StarterQuestions,
                Confidence = 0,
                SessionId = sessionId,
                MessageId = errorMessageId
            };
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatbotStreamChunk> ProcessMessageStreamingAsync(
        string message,
        string sessionId,
        CancellationToken cancellationToken = default)
        => ProcessMessageStreamingAsync(message, sessionId, knowledgeBaseId: null, cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatbotStreamChunk> ProcessMessageStreamingAsync(
        string message,
        string sessionId,
        int? knowledgeBaseId,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var session = await GetOrCreateSessionAsync(sessionId, cancellationToken);
        var messageId = Guid.NewGuid().ToString();

        // Add user message
        session.Messages.Add(new ChatMessage
        {
            Id = messageId,
            Role = ChatMessageRole.User,
            Content = message,
            Timestamp = DateTime.UtcNow
        });

        var conversationHistory = BuildConversationHistory(session, _options.Chatbot.MaxHistoryMessages);
        var fullContent = string.Empty;
        IReadOnlyList<AISource>? sources = null;

        await foreach (var chunk in _searchService.GetAnswerStreamingAsync(
            message,
            new AIAnswerOptions
            {
                TopK = _options.RAG.TopK,
                IncludeSources = _options.RAG.IncludeSourceCitations,
                ConversationHistory = conversationHistory,
                KnowledgeBaseId = knowledgeBaseId
            },
            cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                fullContent += chunk.Content;
            }

            if (chunk.Sources != null)
            {
                sources = chunk.Sources;
            }

            yield return new ChatbotStreamChunk
            {
                Content = chunk.Content,
                IsComplete = chunk.IsComplete,
                Sources = chunk.Sources,
                SuggestedQuestions = chunk.IsComplete
                    ? await GetSuggestedQuestionsAsync(sessionId, cancellationToken)
                    : null
            };
        }

        // Add complete response to history
        session.Messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = ChatMessageRole.Assistant,
            Content = fullContent,
            Timestamp = DateTime.UtcNow,
            Sources = sources
        });

        TrimHistory(session);
        await _sessionStore.SetAsync(sessionId, session, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionStore.GetAsync(sessionId, cancellationToken);
        return session?.Messages.ToList() ?? [];
    }

    /// <inheritdoc />
    public async Task ClearHistoryAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        await _sessionStore.RemoveAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSuggestedQuestionsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        // If no AI provider or no conversation history, return static starters
        var session = await _sessionStore.GetAsync(sessionId, cancellationToken);
        if (_aiProvider == null || session == null || session.Messages.Count < 2)
        {
            return _options.Chatbot.StarterQuestions;
        }

        try
        {
            // Build a concise summary of the last exchange for follow-up generation
            var recentMessages = session.Messages.TakeLast(4).ToList();
            var conversationSnippet = string.Join("\n",
                recentMessages.Select(m => $"{m.Role}: {Truncate(m.Content, 300)}"));

            var messages = new List<AIChatMessage>
            {
                new()
                {
                    Role = AIChatRole.System,
                    Content = """
                        You generate follow-up questions for a website chatbot.
                        Given a conversation snippet, produce exactly 3 short follow-up questions the user might ask next.
                        Return ONLY a JSON array of 3 strings. No markdown, no explanation.
                        Example: ["What are the pricing options?","How do I get started?","Can I see a demo?"]
                        """
                },
                new()
                {
                    Role = AIChatRole.User,
                    Content = conversationSnippet
                }
            };

            var response = await _aiProvider.GenerateChatCompletionAsync(
                messages,
                new AICompletionOptions { MaxTokens = 200, Temperature = 0.8 },
                cancellationToken);

            var parsed = ParseJsonStringArray(response.Content);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Follow-up question generation failed, falling back to starters");
        }

        return _options.Chatbot.StarterQuestions;
    }

    /// <inheritdoc />
    public async Task<string> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        await _sessionStore.SetAsync(sessionId, new ChatSessionData
        {
            Id = sessionId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return sessionId;
    }

    private async Task<ChatSessionData> GetOrCreateSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetAsync(sessionId, cancellationToken);
        if (session is not null)
        {
            return session;
        }

        session = new ChatSessionData
        {
            Id = sessionId,
            CreatedAt = DateTime.UtcNow
        };
        await _sessionStore.SetAsync(sessionId, session, cancellationToken);
        return session;
    }

    private static List<AIChatMessage> BuildConversationHistory(ChatSessionData session, int maxMessages)
    {
        return session.Messages
            .TakeLast(maxMessages)
            .Select(m => new AIChatMessage
            {
                Role = m.Role == ChatMessageRole.User ? AIChatRole.User : AIChatRole.Assistant,
                Content = m.Content
            })
            .ToList();
    }

    private void TrimHistory(ChatSessionData session)
    {
        var maxMessages = _options.Chatbot.MaxHistoryMessages * 2; // User + Assistant pairs
        while (session.Messages.Count > maxMessages)
        {
            session.Messages.RemoveAt(0);
        }
    }

    /// <summary>
    /// Parses a JSON string array from LLM output (e.g. ["q1","q2","q3"]).
    /// </summary>
    private static IReadOnlyList<string> ParseJsonStringArray(string content)
    {
        try
        {
            // Find the JSON array in the response (LLM may wrap in markdown)
            var start = content.IndexOf('[');
            var end = content.LastIndexOf(']');
            if (start < 0 || end < 0 || end <= start)
            {
                return [];
            }

            var json = content[start..(end + 1)];
            var result = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            return result?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}
