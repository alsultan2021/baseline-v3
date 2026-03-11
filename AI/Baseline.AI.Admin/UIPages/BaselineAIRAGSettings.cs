using CMS.DataEngine;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.UIPages;

using Microsoft.Extensions.Logging;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.AI.Admin.UIPages.BaselineAIApplication),
    slug: "rag",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.BaselineAIRAGSettings),
    name: "RAG Settings",
    templateName: TemplateNames.EDIT,
    order: 400)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Edit page for Baseline AI RAG (Retrieval-Augmented Generation) settings.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class BaselineAIRAGSettings : ModelEditPage<BaselineAIRAGSettingsModel>
{
    private readonly ILogger<BaselineAIRAGSettings> _logger;
    private readonly IInfoProvider<BaselineAISettingsInfo> _settingsProvider;
    private BaselineAIRAGSettingsModel _model = new();

    public BaselineAIRAGSettings(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILogger<BaselineAIRAGSettings> logger,
        IInfoProvider<BaselineAISettingsInfo> settingsProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    protected override BaselineAIRAGSettingsModel Model => _model;

    public override async Task ConfigurePage()
    {
        LoadSettings();
        await base.ConfigurePage();
    }

    private void LoadSettings()
    {
        try
        {
            var settings = _settingsProvider.Get()
                .WhereEquals(nameof(BaselineAISettingsInfo.BaselineAISettingsName), "Default")
                .FirstOrDefault();

            if (settings is not null)
            {
                _model = new BaselineAIRAGSettingsModel
                {
                    TopK = settings.RAGTopK,
                    SimilarityThreshold = settings.RAGSimilarityThreshold,
                    SystemPrompt = settings.RAGSystemPrompt,
                    MaxContextTokens = settings.RAGMaxContextTokens
                };
            }

            _logger.LogDebug("Loaded Baseline AI RAG settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Baseline AI RAG settings");
            _model = new BaselineAIRAGSettingsModel();
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        BaselineAIRAGSettingsModel model,
        ICollection<IFormItem> formItems)
    {
        try
        {
            var settings = _settingsProvider.Get()
                .WhereEquals(nameof(BaselineAISettingsInfo.BaselineAISettingsName), "Default")
                .FirstOrDefault();

            if (settings is null)
            {
                return GetErrorResponse("Settings not found. Please contact administrator.");
            }

            settings.RAGTopK = model.TopK;
            settings.RAGSimilarityThreshold = model.SimilarityThreshold;
            settings.RAGSystemPrompt = model.SystemPrompt ?? string.Empty;
            settings.RAGMaxContextTokens = model.MaxContextTokens;

            _settingsProvider.Set(settings);

            _logger.LogInformation("Baseline AI RAG settings saved successfully");
            return GetSuccessResponse("Settings saved successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Baseline AI RAG settings");
            return GetErrorResponse($"Error saving settings: {ex.Message}");
        }
    }

    private ICommandResponse GetSuccessResponse(string message) =>
        ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess))
            .AddSuccessMessage(message);

    private ICommandResponse GetErrorResponse(string message) =>
        ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
            .AddErrorMessage(message);
}

/// <summary>
/// Model for RAG (Retrieval-Augmented Generation) settings.
/// </summary>
public class BaselineAIRAGSettingsModel
{
    [NumberInputComponent(
        Label = "Top K Results",
        ExplanationText = "Number of top semantic search results to include in RAG context")]
    public int TopK { get; set; } = 5;

    [DecimalNumberInputComponent(
        Label = "Similarity Threshold",
        ExplanationText = "Minimum similarity score (0.0 to 1.0) for results to be included")]
    public decimal SimilarityThreshold { get; set; } = 0.7m;

    [TextAreaComponent(
        Label = "System Prompt",
        ExplanationText = "System prompt that defines how the AI should use retrieved context to answer questions")]
    public string? SystemPrompt { get; set; }

    [NumberInputComponent(
        Label = "Max Context Tokens",
        ExplanationText = "Maximum number of tokens to include from retrieved content")]
    public int MaxContextTokens { get; set; } = 4000;
}
