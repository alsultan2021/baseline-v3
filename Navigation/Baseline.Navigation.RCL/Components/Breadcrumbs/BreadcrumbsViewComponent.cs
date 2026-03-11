using Baseline.Core;
using Microsoft.AspNetCore.Mvc;

namespace Baseline.Navigation.Components;

/// <summary>
/// Renders breadcrumb navigation for the current page.
/// Uses IBreadcrumbService to retrieve breadcrumb items.
/// </summary>
public class BreadcrumbsViewComponent(IBreadcrumbService breadcrumbService) : ViewComponent
{
    /// <summary>
    /// Renders breadcrumbs for the current page context.
    /// </summary>
    /// <param name="includeHome">Whether to include the home link.</param>
    /// <param name="includeCurrent">Whether to include the current page.</param>
    /// <param name="separator">The separator between breadcrumb items.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        bool includeHome = true,
        bool includeCurrent = true,
        string separator = "/")
    {
        var breadcrumbs = await breadcrumbService.GetBreadcrumbsAsync();

        var model = new BreadcrumbsViewModel
        {
            Items = breadcrumbs,
            IncludeHome = includeHome,
            IncludeCurrent = includeCurrent,
            Separator = separator
        };

        return View(model);
    }
}

/// <summary>
/// View model for breadcrumb navigation.
/// </summary>
public class BreadcrumbsViewModel
{
    /// <summary>
    /// The breadcrumb items to display.
    /// </summary>
    public IEnumerable<BreadcrumbItem> Items { get; set; } = [];

    /// <summary>
    /// Whether to show the home link.
    /// </summary>
    public bool IncludeHome { get; set; } = true;

    /// <summary>
    /// Whether to show the current page (last item).
    /// </summary>
    public bool IncludeCurrent { get; set; } = true;

    /// <summary>
    /// Separator character/string between items.
    /// </summary>
    public string Separator { get; set; } = "/";
}
