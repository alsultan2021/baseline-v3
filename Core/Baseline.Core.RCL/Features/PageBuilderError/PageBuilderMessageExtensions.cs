using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Baseline.Core.RCL.Features.PageBuilderError;

/// <summary>
/// View model for page builder error/message display.
/// </summary>
public sealed record PageBuilderMessageViewModel(
    string Message,
    bool Inline = true,
    bool IsError = true);

/// <summary>
/// Extension methods for displaying messages in Page Builder widgets.
/// </summary>
public static class PageBuilderMessageExtensions
{
    /// <summary>
    /// Renders a message in a Page Builder widget.
    /// </summary>
    /// <param name="component">The view component.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="inline">Whether to display inline.</param>
    /// <param name="isError">Whether this is an error message.</param>
    public static ViewViewComponentResult PageBuilderMessage(
        this ViewComponent component,
        string message,
        bool inline = true,
        bool isError = true)
    {
        var model = new PageBuilderMessageViewModel(message, inline, isError);
        return component.View("~/Features/PageBuilderError/Message.cshtml", model);
    }

    /// <summary>
    /// Renders an error message in a Page Builder widget.
    /// </summary>
    public static ViewViewComponentResult PageBuilderError(
        this ViewComponent component,
        string message,
        bool inline = true) =>
        component.PageBuilderMessage(message, inline, isError: true);

    /// <summary>
    /// Renders a warning message in a Page Builder widget.
    /// </summary>
    public static ViewViewComponentResult PageBuilderWarning(
        this ViewComponent component,
        string message,
        bool inline = true) =>
        component.PageBuilderMessage(message, inline, isError: false);

    /// <summary>
    /// Renders an info message in a Page Builder widget.
    /// </summary>
    public static ViewViewComponentResult PageBuilderInfo(
        this ViewComponent component,
        string message,
        bool inline = true) =>
        component.PageBuilderMessage(message, inline, isError: false);
}
