using Microsoft.AspNetCore.Mvc;

namespace Baseline.Navigation.Components;

/// <summary>
/// Renders a navigation menu by code name.
/// </summary>
public class MenuViewComponent(IMenuService menuService) : ViewComponent
{
    /// <summary>
    /// Renders a menu by its code name.
    /// </summary>
    /// <param name="menuCodeName">The code name of the menu to render.</param>
    /// <param name="viewName">Optional view name override.</param>
    /// <param name="maxDepth">Maximum depth for nested menus.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        string menuCodeName,
        string? viewName = null,
        int maxDepth = 3)
    {
        var menu = await menuService.GetMenuAsync(menuCodeName);

        var model = new MenuViewModel
        {
            Menu = menu,
            MaxDepth = maxDepth,
            CurrentDepth = 0
        };

        return View(viewName ?? "Default", model);
    }
}

/// <summary>
/// View model for menu rendering.
/// </summary>
public class MenuViewModel
{
    /// <summary>
    /// The menu data.
    /// </summary>
    public Menu? Menu { get; set; }

    /// <summary>
    /// Maximum depth for nested items.
    /// </summary>
    public int MaxDepth { get; set; } = 3;

    /// <summary>
    /// Current rendering depth.
    /// </summary>
    public int CurrentDepth { get; set; }

    /// <summary>
    /// Whether this menu has items.
    /// </summary>
    public bool HasItems => Menu?.Items.Any() ?? false;
}

/// <summary>
/// Model for rendering a single menu item partial with depth tracking.
/// </summary>
public record MenuItemRenderModel(NavigationItem Item, int MaxDepth, int CurrentDepth);

/// <summary>
/// Renders the main navigation menu.
/// </summary>
public class MainMenuViewComponent(IMenuService menuService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        string? viewName = null,
        int maxDepth = 3)
    {
        var menu = await menuService.GetMainMenuAsync();

        var model = new MenuViewModel
        {
            Menu = menu,
            MaxDepth = maxDepth
        };

        return View(viewName ?? "MainMenu", model);
    }
}

/// <summary>
/// Renders the footer navigation menu.
/// </summary>
public class FooterMenuViewComponent(IMenuService menuService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string? viewName = null)
    {
        var menu = await menuService.GetFooterMenuAsync();

        var model = new MenuViewModel { Menu = menu };

        return View(viewName ?? "FooterMenu", model);
    }
}
