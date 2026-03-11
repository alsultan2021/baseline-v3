using Microsoft.AspNetCore.Mvc;

namespace Baseline.TabbedPages.Components;

/// <summary>
/// Renders a tabbed page navigation interface.
/// </summary>
public class TabbedPageViewComponent(ITabbedPageService tabbedPageService) : ViewComponent
{
    /// <summary>
    /// Renders tabs for the current page's tab group.
    /// </summary>
    /// <param name="parentPageId">Parent page ID containing the tabs.</param>
    /// <param name="currentPageId">Current active page ID.</param>
    /// <param name="variant">Tab style variant.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        int? parentPageId = null,
        int? currentPageId = null,
        TabVariant variant = TabVariant.Default)
    {
        // Return empty if no parent page specified
        if (!parentPageId.HasValue)
        {
            return View(new TabbedPageComponentViewModel
            {
                Tabs = [],
                CurrentPageId = currentPageId,
                Variant = variant
            });
        }

        var tabs = (await tabbedPageService.GetTabsAsync(parentPageId.Value)).ToList();

        var model = new TabbedPageComponentViewModel
        {
            Tabs = tabs,
            CurrentPageId = currentPageId,
            Variant = variant
        };

        return View(model);
    }
}

/// <summary>
/// View model for TabbedPage ViewComponent rendering.
/// Uses the module's <see cref="TabItem"/> directly — no duplicate type.
/// </summary>
public class TabbedPageComponentViewModel
{
    public IReadOnlyList<TabItem> Tabs { get; set; } = [];
    public int? CurrentPageId { get; set; }
    public TabVariant Variant { get; set; } = TabVariant.Default;
}

/// <summary>
/// Tab display variants.
/// </summary>
public enum TabVariant
{
    Default,
    Pills,
    Underlined,
    Vertical
}
