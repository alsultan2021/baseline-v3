using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace Baseline.Navigation;

/// <summary>
/// Service for generating accessible navigation markup with ARIA attributes.
/// </summary>
public interface INavigationAccessibilityService
{
    /// <summary>
    /// Generates skip navigation link for accessibility.
    /// </summary>
    IHtmlContent GenerateSkipLink(string targetId = "main-content", string text = "Skip to main content");

    /// <summary>
    /// Gets ARIA attributes for a navigation region.
    /// </summary>
    NavigationAriaAttributes GetNavigationAriaAttributes(string label, bool isExpanded = false);

    /// <summary>
    /// Gets ARIA attributes for a menu item.
    /// </summary>
    MenuItemAriaAttributes GetMenuItemAriaAttributes(
        NavigationItem item,
        bool hasPopup = false,
        bool isExpanded = false,
        int? setSize = null,
        int? positionInSet = null);

    /// <summary>
    /// Gets ARIA attributes for breadcrumb navigation.
    /// </summary>
    BreadcrumbAriaAttributes GetBreadcrumbAriaAttributes();
}

/// <summary>
/// Implementation of navigation accessibility service.
/// </summary>
public sealed class NavigationAccessibilityService(
    IHttpContextAccessor httpContextAccessor) : INavigationAccessibilityService
{
    /// <inheritdoc />
    public IHtmlContent GenerateSkipLink(string targetId = "main-content", string text = "Skip to main content")
    {
        var safeId = HtmlEncoder.Default.Encode(targetId);
        var safeText = HtmlEncoder.Default.Encode(text);
        return new HtmlString(
            $"<a href=\"#{safeId}\" class=\"skip-link sr-only sr-only-focusable\">{safeText}</a>");
    }

    /// <inheritdoc />
    public NavigationAriaAttributes GetNavigationAriaAttributes(string label, bool isExpanded = false)
    {
        return new NavigationAriaAttributes
        {
            Role = "navigation",
            AriaLabel = label,
            AriaExpanded = isExpanded
        };
    }

    /// <inheritdoc />
    public MenuItemAriaAttributes GetMenuItemAriaAttributes(
        NavigationItem item,
        bool hasPopup = false,
        bool isExpanded = false,
        int? setSize = null,
        int? positionInSet = null)
    {
        var currentPath = httpContextAccessor.HttpContext?.Request.Path.Value ?? "/";
        var isCurrentPage = item.Url.Equals(currentPath, StringComparison.OrdinalIgnoreCase);

        return new MenuItemAriaAttributes
        {
            Role = hasPopup ? "menuitem" : null,
            AriaHasPopup = hasPopup ? "menu" : null,
            AriaExpanded = hasPopup ? isExpanded : null,
            AriaCurrent = isCurrentPage ? "page" : item.IsInActivePath ? "true" : null,
            AriaSetSize = setSize,
            AriaPosInSet = positionInSet,
            TabIndex = 0
        };
    }

    /// <inheritdoc />
    public BreadcrumbAriaAttributes GetBreadcrumbAriaAttributes()
    {
        return new BreadcrumbAriaAttributes
        {
            Role = "navigation",
            AriaLabel = "Breadcrumb"
        };
    }
}

/// <summary>
/// ARIA attributes for navigation elements.
/// </summary>
public record NavigationAriaAttributes
{
    /// <summary>
    /// Role attribute value.
    /// </summary>
    public string Role { get; init; } = "navigation";

    /// <summary>
    /// aria-label attribute value.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    /// aria-expanded attribute value.
    /// </summary>
    public bool? AriaExpanded { get; init; }

    /// <summary>
    /// Renders attributes as HTML string.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string> { $"role=\"{HtmlEncoder.Default.Encode(Role)}\"" };

        if (!string.IsNullOrEmpty(AriaLabel))
        {
            parts.Add($"aria-label=\"{HtmlEncoder.Default.Encode(AriaLabel)}\"");
        }

        if (AriaExpanded.HasValue)
        {
            parts.Add($"aria-expanded=\"{AriaExpanded.Value.ToString().ToLowerInvariant()}\"");
        }

        return string.Join(" ", parts);
    }
}

/// <summary>
/// ARIA attributes for menu items.
/// </summary>
public record MenuItemAriaAttributes
{
    /// <summary>
    /// Role attribute value.
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// aria-haspopup attribute value.
    /// </summary>
    public string? AriaHasPopup { get; init; }

    /// <summary>
    /// aria-expanded attribute value.
    /// </summary>
    public bool? AriaExpanded { get; init; }

    /// <summary>
    /// aria-current attribute value.
    /// </summary>
    public string? AriaCurrent { get; init; }

    /// <summary>
    /// aria-setsize attribute value.
    /// </summary>
    public int? AriaSetSize { get; init; }

    /// <summary>
    /// aria-posinset attribute value.
    /// </summary>
    public int? AriaPosInSet { get; init; }

    /// <summary>
    /// tabindex attribute value.
    /// </summary>
    public int? TabIndex { get; init; }

    /// <summary>
    /// Renders attributes as HTML string.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Role))
        {
            parts.Add($"role=\"{HtmlEncoder.Default.Encode(Role)}\"");
        }

        if (!string.IsNullOrEmpty(AriaHasPopup))
        {
            parts.Add($"aria-haspopup=\"{HtmlEncoder.Default.Encode(AriaHasPopup)}\"");
        }

        if (AriaExpanded.HasValue)
        {
            parts.Add($"aria-expanded=\"{AriaExpanded.Value.ToString().ToLowerInvariant()}\"");
        }

        if (!string.IsNullOrEmpty(AriaCurrent))
        {
            parts.Add($"aria-current=\"{HtmlEncoder.Default.Encode(AriaCurrent)}\"");
        }

        if (AriaSetSize.HasValue)
        {
            parts.Add($"aria-setsize=\"{AriaSetSize.Value}\"");
        }

        if (AriaPosInSet.HasValue)
        {
            parts.Add($"aria-posinset=\"{AriaPosInSet.Value}\"");
        }

        if (TabIndex.HasValue)
        {
            parts.Add($"tabindex=\"{TabIndex.Value}\"");
        }

        return string.Join(" ", parts);
    }
}

/// <summary>
/// ARIA attributes for breadcrumb navigation.
/// </summary>
public record BreadcrumbAriaAttributes
{
    /// <summary>
    /// Role attribute value.
    /// </summary>
    public string Role { get; init; } = "navigation";

    /// <summary>
    /// aria-label attribute value.
    /// </summary>
    public string AriaLabel { get; init; } = "Breadcrumb";

    /// <summary>
    /// Renders attributes as HTML string.
    /// </summary>
    public override string ToString()
    {
        return $"role=\"{HtmlEncoder.Default.Encode(Role)}\" aria-label=\"{HtmlEncoder.Default.Encode(AriaLabel)}\"";
    }
}
