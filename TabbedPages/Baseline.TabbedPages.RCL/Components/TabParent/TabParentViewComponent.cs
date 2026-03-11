using Microsoft.AspNetCore.Mvc;

namespace Baseline.TabbedPages.Components;

/// <summary>
/// Renders a tab parent container with tab navigation and content.
/// </summary>
public class TabParentViewComponent(ITabbedPageService tabbedPageService) : ViewComponent
{
    /// <summary>
    /// Renders the tab parent container.
    /// </summary>
    /// <param name="parentPageId">Parent page ID.</param>
    /// <param name="title">Title for the tab container.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="variant">Tab style variant.</param>
    /// <param name="showContent">Whether to render tab content.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        int parentPageId,
        string? title = null,
        string? description = null,
        TabVariant variant = TabVariant.Default,
        bool showContent = true)
    {
        var tabs = (await tabbedPageService.GetTabsAsync(parentPageId)).ToList();

        var model = new TabParentViewModel
        {
            ParentPageId = parentPageId,
            Title = title ?? string.Empty,
            Description = description,
            Tabs = tabs,
            Variant = variant,
            ShowContent = showContent
        };

        return View(model);
    }
}

/// <summary>
/// View model for tab parent container.
/// Uses the module's <see cref="TabItem"/> directly — no duplicate type.
/// </summary>
public class TabParentViewModel
{
    public int ParentPageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<TabItem> Tabs { get; set; } = [];
    public TabVariant Variant { get; set; } = TabVariant.Default;
    public bool ShowContent { get; set; } = true;
    public int? ActiveTabId { get; set; }
}
