using System.Data;

using CMS;
using CMS.DataEngine;

[assembly: RegisterObjectType(typeof(Baseline.AI.Admin.BaselineAISettingsInfo), Baseline.AI.Admin.BaselineAISettingsInfo.OBJECT_TYPE)]

namespace Baseline.AI.Admin;

/// <summary>
/// Data class for storing Baseline AI configuration settings.
/// </summary>
public class BaselineAISettingsInfo : AbstractInfo<BaselineAISettingsInfo, IInfoProvider<BaselineAISettingsInfo>>
{
    /// <summary>
    /// Object type name for Baseline AI Settings.
    /// </summary>
    public const string OBJECT_TYPE = "baselineai.settings";

    /// <summary>
    /// Type information for Baseline AI Settings.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<BaselineAISettingsInfo>),
        OBJECT_TYPE,
        "BaselineAI.Settings",
        nameof(BaselineAISettingsID),
        nameof(BaselineAISettingsLastModified),
        nameof(BaselineAISettingsGuid),
        nameof(BaselineAISettingsName),
        nameof(BaselineAISettingsDisplayName),
        null, // Binary column
        null, // Site ID column
        null) // Parent ID column
    {
        TouchCacheDependencies = true,
        DependsOn = [],
        ContinuousIntegrationSettings =
        {
            Enabled = true,
        }
    };

    /// <summary>
    /// Creates an empty <see cref="BaselineAISettingsInfo"/> instance.
    /// </summary>
    public BaselineAISettingsInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BaselineAISettingsInfo"/> instance from the provided <see cref="DataRow"/>.
    /// </summary>
    public BaselineAISettingsInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    #region System Properties

    /// <summary>
    /// Primary key ID.
    /// </summary>
    [DatabaseField]
    public virtual int BaselineAISettingsID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(BaselineAISettingsID)), 0);
        set => SetValue(nameof(BaselineAISettingsID), value);
    }

    /// <summary>
    /// Unique code name for the settings record.
    /// </summary>
    [DatabaseField]
    public virtual string BaselineAISettingsName
    {
        get => ValidationHelper.GetString(GetValue(nameof(BaselineAISettingsName)), string.Empty);
        set => SetValue(nameof(BaselineAISettingsName), value);
    }

    /// <summary>
    /// Display name for the settings record.
    /// </summary>
    [DatabaseField]
    public virtual string BaselineAISettingsDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(BaselineAISettingsDisplayName)), string.Empty);
        set => SetValue(nameof(BaselineAISettingsDisplayName), value);
    }

    /// <summary>
    /// Unique GUID identifier.
    /// </summary>
    [DatabaseField]
    public virtual Guid BaselineAISettingsGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(BaselineAISettingsGuid)), Guid.Empty);
        set => SetValue(nameof(BaselineAISettingsGuid), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime BaselineAISettingsLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(BaselineAISettingsLastModified)), DateTime.MinValue);
        set => SetValue(nameof(BaselineAISettingsLastModified), value);
    }

    #endregion

    #region Feature Toggles

    /// <summary>
    /// Enables the vector search feature.
    /// </summary>
    [DatabaseField]
    public virtual bool EnableVectorSearch
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(EnableVectorSearch)), true);
        set => SetValue(nameof(EnableVectorSearch), value);
    }

    /// <summary>
    /// Enables the chatbot feature.
    /// </summary>
    [DatabaseField]
    public virtual bool EnableChatbot
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(EnableChatbot)), true);
        set => SetValue(nameof(EnableChatbot), value);
    }

    /// <summary>
    /// Enables the auto-tagging feature.
    /// </summary>
    [DatabaseField]
    public virtual bool EnableAutoTagging
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(EnableAutoTagging)), true);
        set => SetValue(nameof(EnableAutoTagging), value);
    }

    /// <summary>
    /// Enables the search suggestions feature.
    /// </summary>
    [DatabaseField]
    public virtual bool EnableSearchSuggestions
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(EnableSearchSuggestions)), true);
        set => SetValue(nameof(EnableSearchSuggestions), value);
    }

    #endregion

    #region Chatbot Branding

    /// <summary>
    /// Title displayed in the chatbot header.
    /// </summary>
    [DatabaseField]
    public virtual string ChatbotTitle
    {
        get => ValidationHelper.GetString(GetValue(nameof(ChatbotTitle)), "AI Assistant");
        set => SetValue(nameof(ChatbotTitle), value);
    }

    /// <summary>
    /// Placeholder text for the chatbot input field.
    /// </summary>
    [DatabaseField]
    public virtual string ChatbotPlaceholder
    {
        get => ValidationHelper.GetString(GetValue(nameof(ChatbotPlaceholder)), "Ask me anything...");
        set => SetValue(nameof(ChatbotPlaceholder), value);
    }

    /// <summary>
    /// Welcome message displayed when the chatbot opens.
    /// </summary>
    [DatabaseField]
    public virtual string ChatbotWelcomeMessage
    {
        get => ValidationHelper.GetString(GetValue(nameof(ChatbotWelcomeMessage)), "Hello! How can I help you today?");
        set => SetValue(nameof(ChatbotWelcomeMessage), value);
    }

    /// <summary>
    /// Theme color for the chatbot UI (hex code).
    /// </summary>
    [DatabaseField]
    public virtual string ChatbotThemeColor
    {
        get => ValidationHelper.GetString(GetValue(nameof(ChatbotThemeColor)), "#0d6efd");
        set => SetValue(nameof(ChatbotThemeColor), value);
    }

    /// <summary>
    /// Position of the chatbot widget on the page.
    /// </summary>
    [DatabaseField]
    public virtual string ChatbotPosition
    {
        get => ValidationHelper.GetString(GetValue(nameof(ChatbotPosition)), "bottom-right");
        set => SetValue(nameof(ChatbotPosition), value);
    }

    /// <summary>
    /// System prompt for the chatbot.
    /// </summary>
    [DatabaseField]
    public virtual string ChatbotSystemPrompt
    {
        get => ValidationHelper.GetString(GetValue(nameof(ChatbotSystemPrompt)), string.Empty);
        set => SetValue(nameof(ChatbotSystemPrompt), value);
    }

    #endregion

    #region Auto-Tagging Configuration

    /// <summary>
    /// Minimum confidence threshold for auto-tagging (0.0 to 1.0).
    /// </summary>
    [DatabaseField]
    public virtual decimal AutoTaggingMinConfidence
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(AutoTaggingMinConfidence)), 0.7m);
        set => SetValue(nameof(AutoTaggingMinConfidence), value);
    }

    /// <summary>
    /// Maximum number of tags to apply per taxonomy.
    /// </summary>
    [DatabaseField]
    public virtual int AutoTaggingMaxTagsPerTaxonomy
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutoTaggingMaxTagsPerTaxonomy)), 5);
        set => SetValue(nameof(AutoTaggingMaxTagsPerTaxonomy), value);
    }

    /// <summary>
    /// Whether to use LLM for enhanced tag matching.
    /// </summary>
    [DatabaseField]
    public virtual bool AutoTaggingUseLLM
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(AutoTaggingUseLLM)), false);
        set => SetValue(nameof(AutoTaggingUseLLM), value);
    }

    /// <summary>
    /// Whether to automatically apply tags without review.
    /// </summary>
    [DatabaseField]
    public virtual bool AutoTaggingAutoApply
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(AutoTaggingAutoApply)), false);
        set => SetValue(nameof(AutoTaggingAutoApply), value);
    }

    /// <summary>
    /// Confidence threshold for automatic tag application (0.0 to 1.0).
    /// </summary>
    [DatabaseField]
    public virtual decimal AutoTaggingAutoApplyThreshold
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(AutoTaggingAutoApplyThreshold)), 0.9m);
        set => SetValue(nameof(AutoTaggingAutoApplyThreshold), value);
    }

    /// <summary>
    /// JSON array of enabled taxonomy names for auto-tagging.
    /// </summary>
    [DatabaseField]
    public virtual string AutoTaggingEnabledTaxonomies
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutoTaggingEnabledTaxonomies)), "[]");
        set => SetValue(nameof(AutoTaggingEnabledTaxonomies), value);
    }

    /// <summary>
    /// JSON array of content type code names eligible for auto-tagging.
    /// </summary>
    [DatabaseField]
    public virtual string AutoTaggingEligibleContentTypes
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutoTaggingEligibleContentTypes)), "[]");
        set => SetValue(nameof(AutoTaggingEligibleContentTypes), value);
    }

    /// <summary>
    /// JSON array of field names to analyze for auto-tagging.
    /// </summary>
    [DatabaseField]
    public virtual string AutoTaggingAnalyzedFields
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutoTaggingAnalyzedFields)), "[\"Title\", \"Description\", \"Content\"]");
        set => SetValue(nameof(AutoTaggingAnalyzedFields), value);
    }

    /// <summary>
    /// Custom LLM prompt for auto-tagging analysis.
    /// </summary>
    [DatabaseField]
    public virtual string AutoTaggingLLMPrompt
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutoTaggingLLMPrompt)), string.Empty);
        set => SetValue(nameof(AutoTaggingLLMPrompt), value);
    }

    #endregion

    #region RAG Configuration

    /// <summary>
    /// Number of top results to retrieve for RAG context.
    /// </summary>
    [DatabaseField]
    public virtual int RAGTopK
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(RAGTopK)), 5);
        set => SetValue(nameof(RAGTopK), value);
    }

    /// <summary>
    /// Minimum similarity threshold for RAG results (0.0 to 1.0).
    /// </summary>
    [DatabaseField]
    public virtual decimal RAGSimilarityThreshold
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(RAGSimilarityThreshold)), 0.7m);
        set => SetValue(nameof(RAGSimilarityThreshold), value);
    }

    /// <summary>
    /// System prompt for RAG-based responses.
    /// </summary>
    [DatabaseField]
    public virtual string RAGSystemPrompt
    {
        get => ValidationHelper.GetString(GetValue(nameof(RAGSystemPrompt)), string.Empty);
        set => SetValue(nameof(RAGSystemPrompt), value);
    }

    /// <summary>
    /// Maximum number of tokens for RAG context.
    /// </summary>
    [DatabaseField]
    public virtual int RAGMaxContextTokens
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(RAGMaxContextTokens)), 4000);
        set => SetValue(nameof(RAGMaxContextTokens), value);
    }

    #endregion

    

    #region Methods

    /// <inheritdoc/>
    protected override void DeleteObject() =>
        Provider.Delete(this);

    /// <inheritdoc/>
    protected override void SetObject() =>
        Provider.Set(this);

    #endregion
}
