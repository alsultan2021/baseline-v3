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
    slug: "chatbot",
    uiPageType: typeof(Baseline.AI.Admin.UIPages.BaselineAIChatbotSettings),
    name: "Chatbot Settings",
    templateName: TemplateNames.EDIT,
    order: 200)]

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Edit page for Baseline AI chatbot settings (branding and behavior).
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class BaselineAIChatbotSettings : ModelEditPage<BaselineAIChatbotSettingsModel>
{
    private readonly ILogger<BaselineAIChatbotSettings> _logger;
    private readonly IInfoProvider<BaselineAISettingsInfo> _settingsProvider;
    private BaselineAIChatbotSettingsModel _model = new();

    public BaselineAIChatbotSettings(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILogger<BaselineAIChatbotSettings> logger,
        IInfoProvider<BaselineAISettingsInfo> settingsProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    protected override BaselineAIChatbotSettingsModel Model => _model;

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
                _model = new BaselineAIChatbotSettingsModel
                {
                    Title = settings.ChatbotTitle,
                    Placeholder = settings.ChatbotPlaceholder,
                    WelcomeMessage = settings.ChatbotWelcomeMessage,
                    ThemeColor = settings.ChatbotThemeColor,
                    Position = settings.ChatbotPosition,
                    SystemPrompt = settings.ChatbotSystemPrompt
                };
            }

            _logger.LogDebug("Loaded Baseline AI chatbot settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Baseline AI chatbot settings");
            _model = new BaselineAIChatbotSettingsModel();
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        BaselineAIChatbotSettingsModel model,
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

            settings.ChatbotTitle = model.Title;
            settings.ChatbotPlaceholder = model.Placeholder;
            settings.ChatbotWelcomeMessage = model.WelcomeMessage;
            settings.ChatbotThemeColor = model.ThemeColor;
            settings.ChatbotPosition = model.Position;
            settings.ChatbotSystemPrompt = model.SystemPrompt ?? string.Empty;

            _settingsProvider.Set(settings);

            _logger.LogInformation("Baseline AI chatbot settings saved successfully");
            return GetSuccessResponse("Settings saved successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Baseline AI chatbot settings");
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
/// Model for chatbot branding and behavior settings.
/// </summary>
public class BaselineAIChatbotSettingsModel
{
    [TextInputComponent(
        Label = "Chatbot Title",
        ExplanationText = "Title displayed in the chatbot header")]
    [RequiredValidationRule]
    public string Title { get; set; } = "AI Assistant";

    [TextInputComponent(
        Label = "Input Placeholder",
        ExplanationText = "Placeholder text shown in the chat input field")]
    public string Placeholder { get; set; } = "Ask me anything...";

    [TextAreaComponent(
        Label = "Welcome Message",
        ExplanationText = "Message displayed when the chatbot first opens")]
    public string WelcomeMessage { get; set; } = "Hello! How can I help you today?";

    [TextInputComponent(
        Label = "Theme Color",
        ExplanationText = "Primary color for the chatbot widget (hex code, e.g., #0d6efd)")]
    public string ThemeColor { get; set; } = "#0d6efd";

    [DropDownComponent(
        Label = "Widget Position",
        ExplanationText = "Position of the chatbot widget on the page",
        DataProviderType = typeof(ChatbotPositionOptionsProvider))]
    public string Position { get; set; } = "bottom-right";

    [TextAreaComponent(
        Label = "System Prompt",
        ExplanationText = "System prompt that defines the chatbot's behavior and personality (optional)")]
    public string? SystemPrompt { get; set; }
}

/// <summary>
/// Provides options for chatbot widget position dropdown.
/// </summary>
internal class ChatbotPositionOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new DropDownOptionItem { Value = "bottom-right", Text = "Bottom Right" },
            new DropDownOptionItem { Value = "bottom-left", Text = "Bottom Left" },
            new DropDownOptionItem { Value = "top-right", Text = "Top Right" },
            new DropDownOptionItem { Value = "top-left", Text = "Top Left" }
        ]);
}
