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
    slug: "general",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.BaselineAIGeneralSettings),
    name: "General Settings",
    templateName: TemplateNames.EDIT,
    order: 100)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Edit page for Baseline AI general settings (feature toggles).
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class BaselineAIGeneralSettings : ModelEditPage<BaselineAIGeneralSettingsModel>
{
    private readonly ILogger<BaselineAIGeneralSettings> _logger;
    private readonly IInfoProvider<BaselineAISettingsInfo> _settingsProvider;
    private BaselineAIGeneralSettingsModel _model = new();

    public BaselineAIGeneralSettings(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILogger<BaselineAIGeneralSettings> logger,
        IInfoProvider<BaselineAISettingsInfo> settingsProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    protected override BaselineAIGeneralSettingsModel Model => _model;

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
                _model = new BaselineAIGeneralSettingsModel
                {
                    EnableVectorSearch = settings.EnableVectorSearch,
                    EnableChatbot = settings.EnableChatbot,
                    EnableAutoTagging = settings.EnableAutoTagging,
                    EnableSearchSuggestions = settings.EnableSearchSuggestions
                };
            }

            _logger.LogDebug("Loaded Baseline AI general settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Baseline AI general settings");
            _model = new BaselineAIGeneralSettingsModel();
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        BaselineAIGeneralSettingsModel model,
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

            settings.EnableVectorSearch = model.EnableVectorSearch;
            settings.EnableChatbot = model.EnableChatbot;
            settings.EnableAutoTagging = model.EnableAutoTagging;
            settings.EnableSearchSuggestions = model.EnableSearchSuggestions;

            _settingsProvider.Set(settings);

            _logger.LogInformation("Baseline AI general settings saved successfully");
            return GetSuccessResponse("Settings saved successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Baseline AI general settings");
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
/// Model for general AI settings (feature toggles).
/// </summary>
public class BaselineAIGeneralSettingsModel
{
    [CheckBoxComponent(
        Label = "Enable Vector Search",
        ExplanationText = "Enables semantic vector search capabilities using AI embeddings")]
    public bool EnableVectorSearch { get; set; } = true;

    [CheckBoxComponent(
        Label = "Enable Chatbot",
        ExplanationText = "Enables the AI chatbot widget on the frontend")]
    public bool EnableChatbot { get; set; } = true;

    [CheckBoxComponent(
        Label = "Enable Auto-Tagging",
        ExplanationText = "Enables automatic taxonomy tagging of content items")]
    public bool EnableAutoTagging { get; set; } = true;

    [CheckBoxComponent(
        Label = "Enable Search Suggestions",
        ExplanationText = "Enables AI-powered search suggestions")]
    public bool EnableSearchSuggestions { get; set; } = true;
}
