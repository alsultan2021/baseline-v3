using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI.Services;

/// <summary>
/// Default implementation of content auto-tagging service.
/// Uses AI embeddings and optional LLM for intelligent tag suggestions.
/// </summary>
public class DefaultContentAutoTaggingService(
    IAIEmbeddingService embeddingService,
    ITaxonomyEmbeddingService taxonomyEmbeddingService,
    IAIProvider aiProvider,
    IContentProvider contentProvider,
    ITaxonomyProvider taxonomyProvider,
    IOptions<BaselineAIOptions> options,
    ILogger<DefaultContentAutoTaggingService> logger) : IContentAutoTaggingService
{
    private readonly IAIEmbeddingService _embeddingService = embeddingService;
    private readonly ITaxonomyEmbeddingService _taxonomyEmbeddingService = taxonomyEmbeddingService;
    private readonly IAIProvider _aiProvider = aiProvider;
    private readonly IContentProvider _contentProvider = contentProvider;
    private readonly ITaxonomyProvider _taxonomyProvider = taxonomyProvider;
    private readonly BaselineAIOptions _options = options.Value;
    private readonly ILogger<DefaultContentAutoTaggingService> _logger = logger;

    /// <inheritdoc />
    public async Task<TagSuggestionResult> SuggestTagsAsync(
        ContentToTag content,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        options ??= new TaggingOptions();

        try
        {
            _logger.LogDebug("Suggesting tags for content: {Title}", content.Title);

            // Build content text for analysis
            var contentText = BuildContentText(content);

            // Get available taxonomies
            var taxonomies = await GetTargetTaxonomiesAsync(options.TaxonomyNames, cancellationToken);
            if (!taxonomies.Any())
            {
                return new TagSuggestionResult
                {
                    Success = false,
                    ErrorMessage = "No taxonomies available for tagging"
                };
            }

            var allSuggestions = new List<SuggestedTag>();
            var suggestionsByTaxonomy = new Dictionary<string, IReadOnlyList<SuggestedTag>>();

            if (options.UseLLM && _options.AutoTagging.UseLLM)
            {
                // Use LLM for intelligent tag suggestion
                var llmSuggestions = await SuggestTagsWithLLMAsync(
                    contentText,
                    taxonomies,
                    options,
                    cancellationToken);

                allSuggestions.AddRange(llmSuggestions);
            }
            else
            {
                // Use embedding similarity
                var embeddingSuggestions = await SuggestTagsWithEmbeddingsAsync(
                    contentText,
                    taxonomies,
                    options,
                    cancellationToken);

                allSuggestions.AddRange(embeddingSuggestions);
            }

            // Filter by minimum confidence
            var filteredSuggestions = allSuggestions
                .Where(s => s.Confidence >= options.MinConfidence)
                .OrderByDescending(s => s.Confidence)
                .ToList();

            // Group by taxonomy
            foreach (var group in filteredSuggestions.GroupBy(s => s.TaxonomyName))
            {
                var taxonomySuggestions = group
                    .Take(options.MaxTagsPerTaxonomy)
                    .ToList();
                suggestionsByTaxonomy[group.Key] = taxonomySuggestions;
            }

            stopwatch.Stop();

            return new TagSuggestionResult
            {
                Success = true,
                AllTags = filteredSuggestions,
                TagsByTaxonomy = suggestionsByTaxonomy,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting tags for content: {Title}", content.Title);
            return new TagSuggestionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc />
    public async Task<TagSuggestionResult> SuggestTagsForContentItemAsync(
        Guid contentItemGuid,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var content = await _contentProvider.GetContentForTaggingAsync(contentItemGuid, cancellationToken);
        if (content == null)
        {
            return new TagSuggestionResult
            {
                Success = false,
                ErrorMessage = $"Content item not found: {contentItemGuid}"
            };
        }

        return await SuggestTagsAsync(content, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TagApplicationResult> ApplyTagsAsync(
        Guid contentItemGuid,
        IEnumerable<Guid> tagGuids,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tagList = tagGuids.ToList();
            _logger.LogInformation("Applying {Count} tags to content {ContentGuid}",
                tagList.Count, contentItemGuid);

            var existingTags = await _contentProvider.GetExistingTagsAsync(contentItemGuid, cancellationToken);
            var existingGuids = existingTags.ToHashSet();

            var newTags = tagList.Where(t => !existingGuids.Contains(t)).ToList();
            var alreadyPresent = tagList.Count - newTags.Count;

            if (newTags.Count > 0)
            {
                await _contentProvider.ApplyTagsAsync(contentItemGuid, newTags, cancellationToken);
            }

            return new TagApplicationResult
            {
                Success = true,
                AppliedCount = newTags.Count,
                AlreadyPresentCount = alreadyPresent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying tags to content: {ContentGuid}", contentItemGuid);
            return new TagApplicationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<AutoTagResult> AutoTagContentAsync(
        Guid contentItemGuid,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new TaggingOptions
        {
            AutoApply = _options.AutoTagging.AutoApply
        };

        var suggestionResult = await SuggestTagsForContentItemAsync(contentItemGuid, options, cancellationToken);

        if (!suggestionResult.Success)
        {
            return new AutoTagResult
            {
                ContentItemGuid = contentItemGuid,
                Success = false,
                ErrorMessage = suggestionResult.ErrorMessage
            };
        }

        var appliedTags = new List<SuggestedTag>();

        if (options.AutoApply)
        {
            var tagsToApply = suggestionResult.AllTags
                .Where(t => t.Confidence >= _options.AutoTagging.AutoApplyThreshold)
                .ToList();

            if (tagsToApply.Count > 0)
            {
                var applyResult = await ApplyTagsAsync(
                    contentItemGuid,
                    tagsToApply.Select(t => t.TagGuid),
                    cancellationToken);

                if (applyResult.Success)
                {
                    appliedTags = tagsToApply;
                }
            }
        }

        return new AutoTagResult
        {
            ContentItemGuid = contentItemGuid,
            Success = true,
            SuggestedTags = suggestionResult.AllTags,
            AppliedTags = appliedTags
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutoTagResult>> BulkAutoTagAsync(
        IEnumerable<Guid> contentItemGuids,
        TaggingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AutoTagResult>();

        foreach (var guid in contentItemGuids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await AutoTagContentAsync(guid, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaxonomyInfo>> GetAvailableTaxonomiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _taxonomyProvider.GetTaxonomiesAsync(cancellationToken);
    }

    private static string BuildContentText(ContentToTag content)
    {
        var parts = new List<string>
        {
            content.Title,
            content.Body
        };

        if (content.Metadata != null)
        {
            foreach (var value in content.Metadata.Values.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                parts.Add(value);
            }
        }

        return string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private async Task<IReadOnlyList<TaxonomyInfo>> GetTargetTaxonomiesAsync(
        IEnumerable<string>? taxonomyNames,
        CancellationToken cancellationToken)
    {
        var allTaxonomies = await _taxonomyProvider.GetTaxonomiesAsync(cancellationToken);

        if (taxonomyNames?.Any() == true)
        {
            var nameSet = taxonomyNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return allTaxonomies.Where(t => nameSet.Contains(t.Name)).ToList();
        }

        if (_options.AutoTagging.EnabledTaxonomies.Count > 0)
        {
            var enabledSet = _options.AutoTagging.EnabledTaxonomies.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return allTaxonomies.Where(t => enabledSet.Contains(t.Name)).ToList();
        }

        return allTaxonomies;
    }

    private async Task<List<SuggestedTag>> SuggestTagsWithEmbeddingsAsync(
        string contentText,
        IReadOnlyList<TaxonomyInfo> taxonomies,
        TaggingOptions options,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<SuggestedTag>();

        foreach (var taxonomy in taxonomies)
        {
            var matches = await _taxonomyEmbeddingService.FindSimilarTagsAsync(
                contentText,
                taxonomy.Name,
                options.MaxTagsPerTaxonomy,
                cancellationToken);

            foreach (var match in matches)
            {
                suggestions.Add(new SuggestedTag
                {
                    TagGuid = match.TagGuid,
                    Name = match.Name,
                    TaxonomyName = match.TaxonomyName,
                    Confidence = match.Score
                });
            }
        }

        return suggestions;
    }

    private async Task<List<SuggestedTag>> SuggestTagsWithLLMAsync(
        string contentText,
        IReadOnlyList<TaxonomyInfo> taxonomies,
        TaggingOptions options,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<SuggestedTag>();

        // Get all tags from target taxonomies
        var allTags = new List<(string TaxonomyName, Guid TagGuid, string TagName)>();
        foreach (var taxonomy in taxonomies)
        {
            var tags = await _taxonomyProvider.GetTagsAsync(taxonomy.Name, cancellationToken);
            allTags.AddRange(tags.Select(t => (taxonomy.Name, t.TagGuid, t.Name)));
        }

        if (!allTags.Any())
        {
            return suggestions;
        }

        // Build prompt
        var tagListText = string.Join("\n", allTags.Select(t => $"- {t.TagName} ({t.TaxonomyName})"));
        var prompt = _options.AutoTagging.LLMPrompt
            .Replace("{content}", contentText.Length > 4000 ? contentText[..4000] : contentText)
            .Replace("{tags}", tagListText);

        try
        {
            var messages = new List<AIChatMessage>
            {
                new() { Role = AIChatRole.System, Content = "You are a content tagging assistant. Analyze the provided content and suggest the most relevant tags from the given list. Respond with a JSON object containing an array of suggestions." },
                new() { Role = AIChatRole.User, Content = prompt }
            };

            var aiResponse = await _aiProvider.GenerateChatCompletionAsync(messages, null, cancellationToken);
            var response = aiResponse.Content;

            // Parse JSON response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<LLMTagSuggestionResponse>(jsonText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed?.Suggestions != null)
                {
                    foreach (var suggestion in parsed.Suggestions)
                    {
                        var matchingTag = allTags.FirstOrDefault(t =>
                            t.TagName.Equals(suggestion.TagName, StringComparison.OrdinalIgnoreCase));

                        if (matchingTag != default)
                        {
                            suggestions.Add(new SuggestedTag
                            {
                                TagGuid = matchingTag.TagGuid,
                                Name = matchingTag.TagName,
                                TaxonomyName = matchingTag.TaxonomyName,
                                Confidence = Math.Clamp(suggestion.Confidence, 0, 1),
                                Reason = suggestion.Reason
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM tag suggestion failed, falling back to embeddings");
            return await SuggestTagsWithEmbeddingsAsync(contentText, taxonomies, options, cancellationToken);
        }

        return suggestions;
    }

    private sealed class LLMTagSuggestionResponse
    {
        public List<LLMTagSuggestion>? Suggestions { get; set; }
    }

    private sealed class LLMTagSuggestion
    {
        public string TagName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? Reason { get; set; }
    }
}

/// <summary>
/// Interface for content provider used by auto-tagging.
/// </summary>
public interface IContentProvider
{
    /// <summary>
    /// Gets content item for tagging analysis.
    /// </summary>
    Task<ContentToTag?> GetContentForTaggingAsync(Guid contentItemGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets existing tags on a content item.
    /// </summary>
    Task<IEnumerable<Guid>> GetExistingTagsAsync(Guid contentItemGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies tags to a content item.
    /// </summary>
    Task ApplyTagsAsync(Guid contentItemGuid, IEnumerable<Guid> tagGuids, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for taxonomy provider used by auto-tagging.
/// </summary>
public interface ITaxonomyProvider
{
    /// <summary>
    /// Gets available taxonomies.
    /// </summary>
    Task<IReadOnlyList<TaxonomyInfo>> GetTaxonomiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tags for a taxonomy.
    /// </summary>
    Task<IReadOnlyList<TagInfo>> GetTagsAsync(string taxonomyName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tag information.
/// </summary>
public sealed class TagInfo
{
    /// <summary>
    /// Tag GUID.
    /// </summary>
    public Guid TagGuid { get; init; }

    /// <summary>
    /// Tag name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Tag description.
    /// </summary>
    public string? Description { get; init; }
}
