using System.Text.Json;

using Baseline.AI;

using CMS.Core;
using CMS.DataEngine;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.AI.Admin.Services;

/// <summary>
/// Implementation of <see cref="IBaselineAISettingsProvider"/> that merges
/// database settings with appsettings.json defaults.
/// </summary>
public class BaselineAISettingsProvider : IBaselineAISettingsProvider
{
    private const string CacheKey = "BaselineAI:Settings";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    private readonly IInfoProvider<BaselineAISettingsInfo> _settingsInfoProvider;
    private readonly IOptions<BaselineAIOptions> _optionsFromConfig;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BaselineAISettingsProvider> _logger;

    public BaselineAISettingsProvider(
        IInfoProvider<BaselineAISettingsInfo> settingsInfoProvider,
        IOptions<BaselineAIOptions> optionsFromConfig,
        IMemoryCache cache,
        ILogger<BaselineAISettingsProvider> logger)
    {
        _settingsInfoProvider = settingsInfoProvider;
        _optionsFromConfig = optionsFromConfig;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public BaselineAIOptions GetSettings()
    {
        if (_cache.TryGetValue<BaselineAIOptions>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var settings = BuildMergedSettings();

        _cache.Set(CacheKey, settings, CacheExpiration);

        return settings;
    }

    /// <inheritdoc/>
    public Task<BaselineAIOptions> GetSettingsAsync()
    {
        return Task.FromResult(GetSettings());
    }

    /// <inheritdoc/>
    public void RefreshCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("Baseline AI settings cache cleared");
    }

    private BaselineAIOptions BuildMergedSettings()
    {
        // Start with appsettings.json values
        var baseOptions = _optionsFromConfig.Value;

        // Try to load database settings
        BaselineAISettingsInfo? dbSettings = null;
        try
        {
            dbSettings = _settingsInfoProvider.Get()
                .WhereEquals(nameof(BaselineAISettingsInfo.BaselineAISettingsName), "Default")
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load Baseline AI settings from database, using appsettings.json defaults");
        }

        if (dbSettings is null)
        {
            return baseOptions;
        }

        // Merge: database values override appsettings.json for UI-managed settings
        // Keep sensitive/infrastructure settings from appsettings.json
        return new BaselineAIOptions
        {
            // From appsettings.json only (sensitive/infrastructure)
            Provider = baseOptions.Provider,
            ApiKey = baseOptions.ApiKey,
            Endpoint = baseOptions.Endpoint,
            EmbeddingModel = baseOptions.EmbeddingModel,
            ChatModel = baseOptions.ChatModel,
            EmbeddingDimensions = baseOptions.EmbeddingDimensions,
            EnableAutoEmbeddings = baseOptions.EnableAutoEmbeddings,
            IndexedContentTypes = baseOptions.IndexedContentTypes,
            ExcludedContentTypes = baseOptions.ExcludedContentTypes,
            MaxChunkTokens = baseOptions.MaxChunkTokens,
            ChunkOverlapTokens = baseOptions.ChunkOverlapTokens,

            // From database (UI-managed feature toggles)
            EnableVectorSearch = dbSettings.EnableVectorSearch,
            EnableChatbot = dbSettings.EnableChatbot,
            EnableAutoTagging = dbSettings.EnableAutoTagging,
            EnableSearchSuggestions = dbSettings.EnableSearchSuggestions,

            // Chatbot settings from database
            Chatbot = new ChatbotOptions
            {
                Title = dbSettings.ChatbotTitle,
                Placeholder = dbSettings.ChatbotPlaceholder,
                WelcomeMessage = dbSettings.ChatbotWelcomeMessage,
                ThemeColor = dbSettings.ChatbotThemeColor,
                Position = ParseChatbotPosition(dbSettings.ChatbotPosition),
                // Keep remaining from appsettings
                MaxHistoryMessages = baseOptions.Chatbot.MaxHistoryMessages,
                SessionTimeoutMinutes = baseOptions.Chatbot.SessionTimeoutMinutes,
                ShowTypingIndicator = baseOptions.Chatbot.ShowTypingIndicator,
                EnableSuggestedQuestions = baseOptions.Chatbot.EnableSuggestedQuestions,
                StarterQuestions = baseOptions.Chatbot.StarterQuestions
            },

            // Auto-tagging settings from database
            AutoTagging = new AutoTaggingOptions
            {
                MinConfidence = (double)dbSettings.AutoTaggingMinConfidence,
                MaxTagsPerTaxonomy = dbSettings.AutoTaggingMaxTagsPerTaxonomy,
                UseLLM = dbSettings.AutoTaggingUseLLM,
                AutoApply = dbSettings.AutoTaggingAutoApply,
                AutoApplyThreshold = (double)dbSettings.AutoTaggingAutoApplyThreshold,
                EnabledTaxonomies = ParseJsonStringArray(dbSettings.AutoTaggingEnabledTaxonomies),
                EligibleContentTypes = ParseJsonStringArray(dbSettings.AutoTaggingEligibleContentTypes),
                AnalyzedFields = ParseJsonStringArray(dbSettings.AutoTaggingAnalyzedFields),
                LLMPrompt = string.IsNullOrWhiteSpace(dbSettings.AutoTaggingLLMPrompt)
                    ? baseOptions.AutoTagging.LLMPrompt
                    : dbSettings.AutoTaggingLLMPrompt,
                // Keep remaining from appsettings
                IncludeTagDescriptions = baseOptions.AutoTagging.IncludeTagDescriptions
            },

            // RAG settings from database
            RAG = new RAGOptions
            {
                TopK = dbSettings.RAGTopK,
                SimilarityThreshold = (double)dbSettings.RAGSimilarityThreshold,
                MaxContextTokens = dbSettings.RAGMaxContextTokens,
                SystemPrompt = string.IsNullOrWhiteSpace(dbSettings.RAGSystemPrompt)
                    ? baseOptions.RAG.SystemPrompt
                    : dbSettings.RAGSystemPrompt,
                // Keep remaining from appsettings
                IncludeSourceCitations = baseOptions.RAG.IncludeSourceCitations,
                EnableHybridSearch = baseOptions.RAG.EnableHybridSearch,
                SemanticWeight = baseOptions.RAG.SemanticWeight
            }
        };
    }

    private static ChatbotPosition ParseChatbotPosition(string position) =>
        position?.ToLowerInvariant() switch
        {
            "bottom-left" => ChatbotPosition.BottomLeft,
            "top-right" => ChatbotPosition.TopRight,
            "top-left" => ChatbotPosition.TopLeft,
            _ => ChatbotPosition.BottomRight
        };

    private static List<string> ParseJsonStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
