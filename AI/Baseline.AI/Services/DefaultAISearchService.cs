// Suppress obsolete warnings - this service uses legacy methods during transition
#pragma warning disable CS0618

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI;

/// <summary>
/// Default implementation of IAISearchService.
/// </summary>
internal sealed class DefaultAISearchService : IAISearchService
{
    private readonly IAIProvider? _aiProvider;
    private readonly IVectorStore? _vectorStore;
    private readonly BaselineAIOptions _options;
    private readonly ILogger<DefaultAISearchService> _logger;

    public DefaultAISearchService(
        IOptions<BaselineAIOptions> options,
        ILogger<DefaultAISearchService> logger,
        IAIProvider? aiProvider = null,
        IVectorStore? vectorStore = null)
    {
        _options = options.Value;
        _logger = logger;
        _aiProvider = aiProvider;
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<AISearchResult> SearchAsync(
        string query,
        AISearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new AISearchOptions();

        if (_aiProvider == null || _vectorStore == null)
        {
            _logger.LogWarning("AI provider or vector store not configured");
            return new AISearchResult
            {
                Hits = [],
                TotalCount = 0,
                Query = query,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        try
        {
            // Generate embedding for query
            var queryEmbedding = await _aiProvider.GenerateEmbeddingAsync(query, cancellationToken);

            // Search vector store
            var filter = new VectorSearchFilter
            {
                KnowledgeBaseId = options.KnowledgeBaseId,
                ContentTypes = options.ContentTypes,
                LanguageCode = options.LanguageCode,
                ChannelName = options.ChannelName,
                MinScore = options.MinScore
            };

            var results = await _vectorStore.SearchAsync(
                queryEmbedding,
                options.TopK,
                filter,
                cancellationToken);

            var hits = results.Select(r => new AISearchHit
            {
                DocumentId = r.Document.Id,
                ContentItemId = r.Document.ContentItemId,
                ContentItemGuid = r.Document.ContentItemGuid,
                Title = r.Document.Title,
                Url = r.Document.Url,
                Snippet = options.IncludeSnippets
                    ? TruncateContent(r.Document.Content, options.MaxSnippetLength)
                    : null,
                ContentTypeName = r.Document.ContentTypeName,
                Score = r.Score,
                Metadata = r.Document.Metadata
            }).ToList();

            sw.Stop();
            return new AISearchResult
            {
                Hits = hits,
                TotalCount = hits.Count,
                Query = query,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI search failed for query: {Query}", query);
            return new AISearchResult
            {
                Hits = [],
                TotalCount = 0,
                Query = query,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task<AIAnswer> GetAnswerAsync(
        string question,
        AIAnswerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new AIAnswerOptions();

        if (_aiProvider == null || _vectorStore == null)
        {
            _logger.LogWarning("AI provider or vector store not configured");
            return new AIAnswer
            {
                Answer = "AI services are not configured.",
                Sources = [],
                Question = question,
                Confidence = 0,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        try
        {
            // Search for relevant context
            var searchResult = await SearchAsync(
                question,
                new AISearchOptions
                {
                    TopK = options.TopK,
                    ContentTypes = options.ContentTypes,
                    LanguageCode = options.LanguageCode,
                    KnowledgeBaseId = options.KnowledgeBaseId
                },
                cancellationToken);

            // Build context from search results
            var context = BuildContext(searchResult.Hits);

            // Build messages
            var messages = BuildRAGMessages(question, context, options);

            // Generate response
            var response = await _aiProvider.GenerateChatCompletionAsync(
                messages,
                new AICompletionOptions
                {
                    MaxTokens = options.MaxTokens,
                    Temperature = options.Temperature
                },
                cancellationToken);

            // Build sources
            var sources = searchResult.Hits
                .Select(h => new AISource
                {
                    DocumentId = h.DocumentId,
                    Title = h.Title,
                    Url = h.Url,
                    Snippet = h.Snippet,
                    Score = h.Score
                })
                .ToList();

            sw.Stop();
            return new AIAnswer
            {
                Answer = response.Content,
                Sources = options.IncludeSources ? sources : [],
                Question = question,
                Confidence = searchResult.Hits.Count > 0
                    ? searchResult.Hits.Average(h => h.Score)
                    : 0,
                TokenUsage = response.Usage,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI answer generation failed for question: {Question}", question);
            return new AIAnswer
            {
                Answer = "I'm sorry, I encountered an error while processing your question.",
                Sources = [],
                Question = question,
                Confidence = 0,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AIAnswerChunk> GetAnswerStreamingAsync(
        string question,
        AIAnswerOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        options ??= new AIAnswerOptions();

        if (_aiProvider == null || _vectorStore == null)
        {
            yield return new AIAnswerChunk
            {
                Content = "AI services are not configured.",
                IsComplete = true
            };
            yield break;
        }

        // Search for relevant context first
        var searchResult = await SearchAsync(
            question,
            new AISearchOptions
            {
                TopK = options.TopK,
                ContentTypes = options.ContentTypes,
                LanguageCode = options.LanguageCode,
                KnowledgeBaseId = options.KnowledgeBaseId
            },
            cancellationToken);

        var context = BuildContext(searchResult.Hits);
        var messages = BuildRAGMessages(question, context, options);

        var sources = searchResult.Hits
            .Select(h => new AISource
            {
                DocumentId = h.DocumentId,
                Title = h.Title,
                Url = h.Url,
                Snippet = h.Snippet,
                Score = h.Score
            })
            .ToList();

        await foreach (var chunk in _aiProvider.GenerateChatCompletionStreamingAsync(
            messages,
            new AICompletionOptions
            {
                MaxTokens = options.MaxTokens,
                Temperature = options.Temperature,
                Stream = true
            },
            cancellationToken))
        {
            yield return new AIAnswerChunk
            {
                Content = chunk.Content,
                IsComplete = chunk.IsComplete,
                Sources = chunk.IsComplete && options.IncludeSources ? sources : null
            };
        }
    }

    /// <inheritdoc />
    public async Task<AISearchResult> HybridSearchAsync(
        string query,
        AISearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new AISearchOptions();

        if (_aiProvider == null || _vectorStore == null)
        {
            _logger.LogWarning("AI provider or vector store not configured");
            return new AISearchResult
            {
                Hits = [],
                TotalCount = 0,
                Query = query,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        try
        {
            // Arm 1: Semantic search (vector similarity)
            var semanticTask = SearchAsync(query, options, cancellationToken);

            // Arm 2: Keyword search (scan stored documents for term overlap)
            var keywordTask = KeywordSearchAsync(query, options, cancellationToken);

            await Task.WhenAll(semanticTask, keywordTask);

            var semanticHits = semanticTask.Result.Hits;
            var keywordHits = keywordTask.Result.Hits;

            // Reciprocal Rank Fusion (k=60 is standard)
            const int k = 60;
            var fusedScores = new Dictionary<string, (AISearchHit Hit, double Score)>();

            for (int i = 0; i < semanticHits.Count; i++)
            {
                var hit = semanticHits[i];
                var rrf = 1.0 / (k + i + 1);
                fusedScores[hit.DocumentId] = (hit, rrf);
            }

            for (int i = 0; i < keywordHits.Count; i++)
            {
                var hit = keywordHits[i];
                var rrf = 1.0 / (k + i + 1);

                if (fusedScores.TryGetValue(hit.DocumentId, out var existing))
                {
                    fusedScores[hit.DocumentId] = (existing.Hit, existing.Score + rrf);
                }
                else
                {
                    fusedScores[hit.DocumentId] = (hit, rrf);
                }
            }

            var merged = fusedScores.Values
                .OrderByDescending(x => x.Score)
                .Take(options.TopK)
                .Select(x => new AISearchHit
                {
                    DocumentId = x.Hit.DocumentId,
                    ContentItemId = x.Hit.ContentItemId,
                    ContentItemGuid = x.Hit.ContentItemGuid,
                    Title = x.Hit.Title,
                    Url = x.Hit.Url,
                    Snippet = x.Hit.Snippet,
                    ContentTypeName = x.Hit.ContentTypeName,
                    Score = x.Score,
                    Metadata = x.Hit.Metadata
                })
                .ToList();

            sw.Stop();
            return new AISearchResult
            {
                Hits = merged,
                TotalCount = merged.Count,
                Query = query,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hybrid search failed for query: {Query}", query);
            // Fallback to pure semantic
            return await SearchAsync(query, options, cancellationToken);
        }
    }

    /// <summary>
    /// Simple keyword search: tokenizes query, scans stored documents for term overlap.
    /// Returns hits ranked by BM25-inspired term frequency scoring.
    /// </summary>
    private async Task<AISearchResult> KeywordSearchAsync(
        string query,
        AISearchOptions options,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var filter = new VectorSearchFilter
            {
                KnowledgeBaseId = options.KnowledgeBaseId,
                ContentTypes = options.ContentTypes,
                LanguageCode = options.LanguageCode,
                ChannelName = options.ChannelName
            };

            // Get all documents from vector store (for keyword scanning)
            var allDocs = await _vectorStore!.ListDocumentsAsync(filter, cancellationToken);

            // Tokenize query into lowercase terms
            var queryTerms = TokenizeForKeyword(query);
            if (queryTerms.Count == 0)
            {
                return new AISearchResult { Hits = [], TotalCount = 0, Query = query, DurationMs = sw.ElapsedMilliseconds };
            }

            // Score each document by term overlap
            var scored = new List<(AIDocument Doc, double Score)>();
            foreach (var doc in allDocs)
            {
                var docTerms = TokenizeForKeyword(doc.Content ?? "");
                if (docTerms.Count == 0) continue;

                // Simple TF scoring: count of query terms found in document / total query terms
                var matchCount = queryTerms.Count(qt => docTerms.Contains(qt));
                if (matchCount == 0) continue;

                var score = (double)matchCount / queryTerms.Count;

                // Boost exact phrase matches
                var lowerContent = (doc.Content ?? "").ToLowerInvariant();
                if (lowerContent.Contains(query.ToLowerInvariant()))
                {
                    score += 0.5;
                }

                scored.Add((doc, score));
            }

            var hits = scored
                .OrderByDescending(x => x.Score)
                .Take(options.TopK)
                .Select(x => new AISearchHit
                {
                    DocumentId = x.Doc.Id,
                    ContentItemId = x.Doc.ContentItemId,
                    ContentItemGuid = x.Doc.ContentItemGuid,
                    Title = x.Doc.Title,
                    Url = x.Doc.Url,
                    Snippet = options.IncludeSnippets
                        ? TruncateContent(x.Doc.Content ?? "", options.MaxSnippetLength)
                        : null,
                    ContentTypeName = x.Doc.ContentTypeName,
                    Score = x.Score,
                    Metadata = x.Doc.Metadata
                })
                .ToList();

            sw.Stop();
            return new AISearchResult
            {
                Hits = hits,
                TotalCount = hits.Count,
                Query = query,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword search failed, returning empty results");
            return new AISearchResult { Hits = [], TotalCount = 0, Query = query, DurationMs = sw.ElapsedMilliseconds };
        }
    }

    /// <summary>
    /// Splits text into lowercase word tokens, filtering stop words.
    /// </summary>
    private static HashSet<string> TokenizeForKeyword(string text)
    {
        var words = System.Text.RegularExpressions.Regex
            .Split(text.ToLowerInvariant(), @"\W+")
            .Where(w => w.Length > 2 && !StopWords.Contains(w));

        return [.. words];
    }

    private static readonly HashSet<string> StopWords =
    [
        "the", "and", "for", "are", "but", "not", "you", "all",
        "can", "had", "her", "was", "one", "our", "out", "has",
        "have", "been", "some", "them", "than", "its", "over",
        "such", "that", "this", "with", "will", "each", "from",
        "they", "into", "more", "other", "about", "which", "when",
        "what", "there", "also", "just", "how", "where"
    ];

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content[..maxLength] + "...";
    }

    private static string BuildContext(IReadOnlyList<AISearchHit> hits)
    {
        if (hits.Count == 0)
            return string.Empty;

        var parts = hits.Select((h, i) =>
            $"[{i + 1}] {h.Title ?? "Untitled"}\n{h.Snippet ?? "No content available."}");

        return string.Join("\n\n", parts);
    }

    private List<AIChatMessage> BuildRAGMessages(
        string question,
        string context,
        AIAnswerOptions options)
    {
        var systemPrompt = ResolvePromptTemplate(
            options.SystemPrompt ?? _options.RAG.SystemPrompt,
            options);

        var messages = new List<AIChatMessage>
        {
            new() { Role = AIChatRole.System, Content = systemPrompt }
        };

        // Add conversation history if provided
        if (options.ConversationHistory != null)
        {
            messages.AddRange(options.ConversationHistory);
        }

        // Add context and question
        var userMessage = string.IsNullOrEmpty(context)
            ? question
            : $"Context:\n{context}\n\nQuestion: {question}";

        messages.Add(new AIChatMessage
        {
            Role = AIChatRole.User,
            Content = userMessage
        });

        return messages;
    }

    /// <summary>
    /// Resolves template placeholders in system prompt:
    /// {SiteName}, {ContentTypes}, {KnowledgeBaseId}, {Language}, {MaxTokens}
    /// </summary>
    private static string ResolvePromptTemplate(string template, AIAnswerOptions options)
    {
        if (!template.Contains('{'))
        {
            return template;
        }

        return template
            .Replace("{KnowledgeBaseId}", options.KnowledgeBaseId?.ToString() ?? "default", StringComparison.OrdinalIgnoreCase)
            .Replace("{Language}", options.LanguageCode ?? "en", StringComparison.OrdinalIgnoreCase)
            .Replace("{ContentTypes}", options.ContentTypes is { Count: > 0 }
                ? string.Join(", ", options.ContentTypes)
                : "all", StringComparison.OrdinalIgnoreCase)
            .Replace("{MaxTokens}", options.MaxTokens?.ToString() ?? "1024", StringComparison.OrdinalIgnoreCase);
    }
}
