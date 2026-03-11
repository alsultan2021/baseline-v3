using System.Text.Json;

using CMS.DataEngine;
using CMS.Membership;

using Baseline.AI.Admin.DataProviders;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.UIPages;

using Microsoft.Extensions.Logging;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.AI.Admin.UIPages.BaselineAIApplication),
    slug: "auto-tagging",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.BaselineAIAutoTaggingSettings),
    name: "Auto-Tagging Settings",
    templateName: TemplateNames.EDIT,
    order: 300)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Edit page for Baseline AI auto-tagging settings.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class BaselineAIAutoTaggingSettings : ModelEditPage<BaselineAIAutoTaggingSettingsModel>
{
    private readonly ILogger<BaselineAIAutoTaggingSettings> _logger;
    private readonly IInfoProvider<BaselineAISettingsInfo> _settingsProvider;
    private BaselineAIAutoTaggingSettingsModel _model = new();

    public BaselineAIAutoTaggingSettings(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILogger<BaselineAIAutoTaggingSettings> logger,
        IInfoProvider<BaselineAISettingsInfo> settingsProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    protected override BaselineAIAutoTaggingSettingsModel Model => _model;

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
                _model = new BaselineAIAutoTaggingSettingsModel
                {
                    MinConfidence = settings.AutoTaggingMinConfidence,
                    MaxTagsPerTaxonomy = settings.AutoTaggingMaxTagsPerTaxonomy,
                    UseLLM = settings.AutoTaggingUseLLM,
                    AutoApply = settings.AutoTaggingAutoApply,
                    AutoApplyThreshold = settings.AutoTaggingAutoApplyThreshold,
                    EnabledTaxonomies = DeserializeList(settings.AutoTaggingEnabledTaxonomies),
                    EligibleContentTypes = DeserializeList(settings.AutoTaggingEligibleContentTypes),
                    AnalyzedFields = DeserializeList(settings.AutoTaggingAnalyzedFields),
                    LLMPrompt = settings.AutoTaggingLLMPrompt
                };
            }

            _logger.LogDebug("Loaded Baseline AI auto-tagging settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Baseline AI auto-tagging settings");
            _model = new BaselineAIAutoTaggingSettingsModel();
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        BaselineAIAutoTaggingSettingsModel model,
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

            settings.AutoTaggingMinConfidence = model.MinConfidence;
            settings.AutoTaggingMaxTagsPerTaxonomy = model.MaxTagsPerTaxonomy;
            settings.AutoTaggingUseLLM = model.UseLLM;
            settings.AutoTaggingAutoApply = model.AutoApply;
            settings.AutoTaggingAutoApplyThreshold = model.AutoApplyThreshold;
            settings.AutoTaggingEnabledTaxonomies = SerializeList(model.EnabledTaxonomies);
            settings.AutoTaggingEligibleContentTypes = SerializeList(model.EligibleContentTypes);
            settings.AutoTaggingAnalyzedFields = SerializeList(model.AnalyzedFields);
            settings.AutoTaggingLLMPrompt = model.LLMPrompt ?? string.Empty;

            _settingsProvider.Set(settings);

            _logger.LogInformation("Baseline AI auto-tagging settings saved successfully");
            return GetSuccessResponse("Settings saved successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Baseline AI auto-tagging settings");
            return GetErrorResponse($"Error saving settings: {ex.Message}");
        }
    }

    private ICommandResponse GetSuccessResponse(string message) =>
        ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess))
            .AddSuccessMessage(message);

    private ICommandResponse GetErrorResponse(string message) =>
        ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
            .AddErrorMessage(message);

    private static IEnumerable<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IEnumerable<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeList(IEnumerable<string>? items) =>
        items is not null && items.Any()
            ? JsonSerializer.Serialize(items)
            : "[]";
}

/// <summary>
/// Model for auto-tagging configuration settings.
/// </summary>
public class BaselineAIAutoTaggingSettingsModel
{
    [DecimalNumberInputComponent(
        Label = "Minimum Confidence",
        ExplanationText = "Minimum confidence score (0.0 to 1.0) required to suggest a tag")]
    public decimal MinConfidence { get; set; } = 0.7m;

    [NumberInputComponent(
        Label = "Max Tags per Taxonomy",
        ExplanationText = "Maximum number of tags to suggest per taxonomy")]
    public int MaxTagsPerTaxonomy { get; set; } = 5;

    [CheckBoxComponent(
        Label = "Use LLM Enhancement",
        ExplanationText = "Use Large Language Model for more intelligent tag matching (requires additional API calls)")]
    public bool UseLLM { get; set; } = false;

    [CheckBoxComponent(
        Label = "Auto-Apply Tags",
        ExplanationText = "Automatically apply high-confidence tags without manual review")]
    public bool AutoApply { get; set; } = false;

    [DecimalNumberInputComponent(
        Label = "Auto-Apply Threshold",
        ExplanationText = "Minimum confidence score (0.0 to 1.0) for automatic tag application")]
    public decimal AutoApplyThreshold { get; set; } = 0.9m;

    [GeneralSelectorComponent(
        dataProviderType: typeof(AutoTaggingTaxonomySelectorDataProvider),
        Label = "Enabled Taxonomies",
        ExplanationText = "Select taxonomies to use for auto-tagging",
        Placeholder = "Select taxonomies...",
        Order = 60)]
    public IEnumerable<string> EnabledTaxonomies { get; set; } = [];

    [GeneralSelectorComponent(
        dataProviderType: typeof(AutoTaggingContentTypeSelectorDataProvider),
        Label = "Eligible Content Types",
        ExplanationText = "Select content types eligible for auto-tagging (leave empty for all types)",
        Placeholder = "Select content types...",
        Order = 70)]
    public IEnumerable<string> EligibleContentTypes { get; set; } = [];

    [GeneralSelectorComponent(
        dataProviderType: typeof(AnalyzedFieldsSelectorDataProvider),
        Label = "Analyzed Fields",
        ExplanationText = "Select content fields to analyze for tag suggestions",
        Placeholder = "Select fields to analyze...",
        Order = 80)]
    public IEnumerable<string> AnalyzedFields { get; set; } = ["Title", "Description", "Content"];

    [TextAreaComponent(
        Label = "Custom LLM Prompt",
        ExplanationText = "Custom prompt for LLM-based tag analysis (leave empty for default)")]
    public string? LLMPrompt { get; set; }
}
