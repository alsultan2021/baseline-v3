using Baseline.AI.Data;
using Baseline.AI.ViewComponents;

using CMS.DataEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterWidget(
    identifier: AIChatWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(AIChatWidgetViewComponent),
    name: "AI Chat",
    propertiesType: typeof(AIChatWidgetProperties),
    Description = "Displays an AI-powered chatbot that answers questions using a knowledge base.",
    IconClass = "icon-bubble",
    AllowCache = false)]

namespace Baseline.AI.ViewComponents;

/// <summary>
/// Page Builder widget for AI chatbot.
/// </summary>
public class AIChatWidgetViewComponent(
    IInfoProvider<AIKnowledgeBaseInfo> kbProvider,
    IOptions<BaselineAIOptions> aiOptions,
    ILogger<AIChatWidgetViewComponent> logger) : ViewComponent
{
    /// <summary>
    /// Widget identifier.
    /// </summary>
    public const string IDENTIFIER = "XperienceCommunity.Baseline.AIChatWidget";

    public IViewComponentResult Invoke(ComponentViewModel<AIChatWidgetProperties> cvm)
    {
        var properties = cvm.Properties;
        var chatOptions = aiOptions.Value.Chatbot;

        if (properties.KnowledgeBaseId <= 0)
        {
            return Content("");
        }

        var kb = kbProvider.Get()
            .WhereEquals(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId), properties.KnowledgeBaseId)
            .FirstOrDefault();

        if (kb is null)
        {
            logger.LogWarning("Knowledge base {KbId} not found for chat widget", properties.KnowledgeBaseId);
            return Content("");
        }

        if (kb.KnowledgeBaseStatus != (int)KnowledgeBaseStatus.Idle)
        {
            logger.LogWarning("Knowledge base {KbId} '{KbName}' is not ready",
                kb.KnowledgeBaseId, kb.KnowledgeBaseDisplayName);
            return Content("");
        }

        var model = new AIChatWidgetViewModel
        {
            KnowledgeBaseId = properties.KnowledgeBaseId,
            KnowledgeBaseName = kb.KnowledgeBaseDisplayName,
            Title = string.IsNullOrEmpty(properties.Title) ? chatOptions.Title : properties.Title,
            Placeholder = string.IsNullOrEmpty(properties.Placeholder) ? chatOptions.Placeholder : properties.Placeholder,
            WelcomeMessage = string.IsNullOrEmpty(properties.WelcomeMessage) ? chatOptions.WelcomeMessage : properties.WelcomeMessage,
            Theme = string.IsNullOrEmpty(properties.Theme) ? "light" : properties.Theme,
            Position = string.IsNullOrEmpty(properties.Position) ? chatOptions.Position.ToString().ToLowerInvariant() : properties.Position,
            InitialExpanded = properties.InitialExpanded,
            StarterQuestions = chatOptions.StarterQuestions
        };

        return View("~/Views/Shared/Components/AIChatWidget/Default.cshtml", model);
    }
}

/// <summary>
/// Properties for AI chat widget.
/// </summary>
public class AIChatWidgetProperties : IWidgetProperties
{
    /// <summary>
    /// Knowledge base ID to use for answering questions.
    /// </summary>
    [NumberInputComponent(Label = "Knowledge base ID", Order = 1)]
    public int KnowledgeBaseId { get; set; }

    /// <summary>
    /// Title displayed in chat header.
    /// </summary>
    [TextInputComponent(Label = "Title", Order = 2)]
    public string Title { get; set; } = "Chat with us";

    /// <summary>
    /// Placeholder text for input field.
    /// </summary>
    [TextInputComponent(Label = "Placeholder text", Order = 3)]
    public string Placeholder { get; set; } = "Type your question...";

    /// <summary>
    /// Theme: "light" or "dark".
    /// </summary>
    [DropDownComponent(Label = "Theme", Options = "light;Light\ndark;Dark", Order = 4)]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Position: "bottom-right", "bottom-left", "top-right", "top-left".
    /// </summary>
    [DropDownComponent(Label = "Position", Options = "bottom-right;Bottom Right\nbottom-left;Bottom Left\ntop-right;Top Right\ntop-left;Top Left", Order = 5)]
    public string Position { get; set; } = "bottom-right";

    /// <summary>
    /// Whether chat should be expanded by default.
    /// </summary>
    [CheckBoxComponent(Label = "Initially expanded", Order = 6)]
    public bool InitialExpanded { get; set; }

    /// <summary>
    /// Custom welcome message. Falls back to config if empty.
    /// </summary>
    [TextInputComponent(Label = "Welcome message", Order = 7)]
    public string WelcomeMessage { get; set; } = "";
}

/// <summary>
/// ViewModel for AI chat widget.
/// </summary>
public class AIChatWidgetViewModel
{
    public int KnowledgeBaseId { get; set; }
    public string KnowledgeBaseName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public string WelcomeMessage { get; set; } = "Hi! How can I help you today?";
    public string Theme { get; set; } = "light";
    public string Position { get; set; } = "bottom-right";
    public bool InitialExpanded { get; set; }
    public IReadOnlyList<string> StarterQuestions { get; set; } = [];
}
