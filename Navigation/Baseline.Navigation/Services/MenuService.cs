using CMS.Websites;
using Generic;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Navigation;

/// <summary>
/// Implementation of menu service with INavigationRepository and IMenuService support.
/// </summary>
public class MenuService(
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IOptions<BaselineNavigationOptions> options,
    ILogger<MenuService> logger) : INavigationRepository, IMenuService
{
    private readonly MenuOptions _options = options.Value.Menus;

    public async Task<Menu?> GetMenuAsync(string menuCodeName)
    {
        // Query navigation items under the menu path
        var rootPath = $"/MasterPage/Navigation/{menuCodeName}";
        var items = await GetNavItemsAsync(rootPath);
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            logger.LogDebug("MenuService: No items found for menu '{CodeName}'", menuCodeName);
            return null;
        }

        return new Menu
        {
            CodeName = menuCodeName,
            DisplayName = menuCodeName,
            Items = itemList
        };
    }

    public async Task<Menu?> GetMainMenuAsync()
    {
        return await GetMenuAsync("main-menu");
    }

    public async Task<Menu?> GetFooterMenuAsync()
    {
        return await GetMenuAsync("footer-menu");
    }

    public async Task<IEnumerable<NavigationItem>> GetNavigationTreeAsync(
        string? rootPath = null,
        int maxDepth = 3)
    {
        // Limit depth to configuration
        maxDepth = Math.Min(maxDepth, _options.MaxDepth);

        // Use provided path or fall back to default navigation path
        var path = rootPath ?? "/MasterPage/Navigation";

        // Query navigation items from content tree
        return await GetNavItemsAsync(path);
    }

    public async Task<IEnumerable<NavigationItem>> GetChildNavigationAsync(string parentPath)
    {
        return await GetNavItemsAsync(parentPath);
    }

    /// <summary>
    /// Queries Navigation content type items under the specified parent path.
    /// </summary>
    public async Task<IEnumerable<NavigationItem>> GetNavItemsAsync(string parentPath)
    {
        var navItems = new List<NavigationItem>();

        try
        {
            logger.LogDebug("MenuService: Querying navigation under path '{Path}'", parentPath);

            // Use IContentRetriever — auto-handles channel, preview, language
            var navigationPages = await contentRetriever.RetrievePages<Generic.Navigation>(
                new RetrievePagesParameters
                {
                    PathMatch = PathMatch.Children(parentPath, nestingLevel: 1),
                    LinkedItemsMaxLevel = 1
                },
                additionalQueryConfiguration: query => query.OrderBy("WebPageItemOrder"),
                cacheSettings: new RetrievalCacheSettings(
                    cacheItemNameSuffix: $"nav|{parentPath}"));

            var allPages = navigationPages.ToList();

            logger.LogDebug("MenuService: Found {Count} navigation items under path '{Path}'", allPages.Count, parentPath);

            foreach (var nav in allPages)
            {
                var navItem = await MapToNavigationItem(nav);
                navItems.Add(navItem);
            }

            logger.LogDebug("MenuService: Returning {Count} items for path '{Path}'", navItems.Count, parentPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MenuService: Error querying navigation for path '{Path}'", parentPath);
        }

        return navItems;
    }

    private async Task<NavigationItem> MapToNavigationItem(Generic.Navigation nav)
    {
        var url = nav.NavigationLinkUrl;

        // For "automatic" navigation type, get URL from linked page
        var isAutomatic = nav.NavigationType?.Equals("automatic", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isAutomatic && nav.NavigationWebPageItemGuid?.Any() == true)
        {
            var linkedPage = nav.NavigationWebPageItemGuid.FirstOrDefault();
            if (linkedPage != null)
            {
                try
                {
                    // Get the current language and use IWebPageUrlRetriever to get the URL for the linked page
                    var languageName = preferredLanguageRetriever.Get();
                    var pageUrl = await webPageUrlRetriever.Retrieve(linkedPage.WebPageGuid, languageName);
                    url = pageUrl?.RelativePath ?? "#";
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "MenuService: Failed to retrieve URL for linked page {PageGuid}", linkedPage.WebPageGuid);
                    url = "#";
                }
            }
            else
            {
                url = "#";
            }
        }
        else if (isAutomatic && (nav.NavigationWebPageItemGuid == null || !nav.NavigationWebPageItemGuid.Any()))
        {
            url = "#";
        }
        else if (string.IsNullOrWhiteSpace(url))
        {
            url = "#";
        }

        // Determine title - use NavigationLinkAlt (Alternative Text) if provided, 
        // otherwise NavigationLinkText, otherwise WebPageItemName
        var title = !string.IsNullOrWhiteSpace(nav.NavigationLinkAlt)
            ? nav.NavigationLinkAlt
            : !string.IsNullOrWhiteSpace(nav.NavigationLinkText)
                ? nav.NavigationLinkText
                : nav.SystemFields?.WebPageItemName ?? string.Empty;

        // Clean up the title - replace underscores with spaces for display
        title = title.Replace("_", " ");

        return new NavigationItem
        {
            Title = title,
            Url = url ?? "#",
            Target = nav.NavigationLinkTarget,
            Description = nav.NavigationLinkAlt,
            CssClass = nav.NavigationLinkCSS,
            OnClick = nav.NavigationLinkOnClick,
            IsMegaMenu = nav.NavigationIsMegaMenu,
            Path = nav.SystemFields?.WebPageItemTreePath ?? string.Empty,
            IsDynamic = nav.IsDynamic,
            DynamicCodeName = nav.DynamicCodeName
        };
    }
}
