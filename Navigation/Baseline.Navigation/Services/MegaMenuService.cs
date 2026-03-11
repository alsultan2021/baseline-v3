using CMS.ContentEngine;
using CMS.Websites;
using Generic;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Baseline.Navigation;

/// <summary>
/// Service for mega menu generation with enhanced content support.
/// </summary>
public interface IMegaMenuService
{
    /// <summary>
    /// Gets a mega menu by code name with all content loaded.
    /// </summary>
    Task<MegaMenu?> GetMegaMenuAsync(string codeName);

    /// <summary>
    /// Gets all mega menu items for a navigation item.
    /// </summary>
    Task<MegaMenuContent> GetMegaMenuContentAsync(NavigationItem navigationItem);

    /// <summary>
    /// Invalidates cached mega menu data.
    /// </summary>
    void InvalidateCache(string? codeName = null);
}

/// <summary>
/// Implementation of mega menu service with caching and rich content support.
/// </summary>
public sealed class MegaMenuService(
    IContentRetriever contentRetriever,
    IMemoryCache memoryCache,
    IOptions<BaselineNavigationOptions> options,
    ILogger<MegaMenuService> logger) : IMegaMenuService
{
    private readonly MenuOptions _options = options.Value.Menus;
    private const string CacheKeyPrefix = "Baseline_MegaMenu_";
    private CancellationTokenSource _cacheTokenSource = new();

    /// <inheritdoc />
    public async Task<MegaMenu?> GetMegaMenuAsync(string codeName)
    {
        if (string.IsNullOrWhiteSpace(codeName))
        {
            return null;
        }

        var cacheKey = GetCacheKey(codeName);

        // Try to get from cache if caching is enabled
        if (_options.EnableCaching && memoryCache.TryGetValue(cacheKey, out MegaMenu? cached))
        {
            logger.LogDebug("MegaMenuService: Returning cached mega menu '{CodeName}'", codeName);
            return cached;
        }

        var megaMenu = await BuildMegaMenuAsync(codeName);

        if (megaMenu is not null && _options.EnableCaching)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheDurationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(_options.CacheDurationMinutes / 2)
            };
            cacheOptions.AddExpirationToken(new CancellationChangeToken(_cacheTokenSource.Token));

            memoryCache.Set(cacheKey, megaMenu, cacheOptions);
            logger.LogDebug("MegaMenuService: Cached mega menu '{CodeName}'", codeName);
        }

        return megaMenu;
    }

    /// <inheritdoc />
    public async Task<MegaMenuContent> GetMegaMenuContentAsync(NavigationItem navigationItem)
    {
        if (!navigationItem.IsMegaMenu)
        {
            return new MegaMenuContent();
        }

        var content = new MegaMenuContent
        {
            Title = navigationItem.Title,
            Description = navigationItem.Description
        };

        // Load child items as mega menu columns
        if (navigationItem.HasChildren)
        {
            foreach (var child in navigationItem.Children)
            {
                var column = new MegaMenuColumn
                {
                    Title = child.Title,
                    Url = child.Url,
                    Items = child.Children.Select(c => new MegaMenuItem
                    {
                        Title = c.Title,
                        Url = c.Url,
                        Description = c.Description,
                        IconClass = c.IconClass
                    }).ToList()
                };

                content.Columns.Add(column);
            }
        }

        return await Task.FromResult(content);
    }

    /// <inheritdoc />
    public void InvalidateCache(string? codeName = null)
    {
        if (!string.IsNullOrEmpty(codeName))
        {
            memoryCache.Remove(GetCacheKey(codeName));
            logger.LogInformation("MegaMenuService: Invalidated cache for mega menu '{CodeName}'", codeName);
        }
        else
        {
            // Cancel all linked cache entries and create a new token source
            _cacheTokenSource.Cancel();
            _cacheTokenSource.Dispose();
            _cacheTokenSource = new CancellationTokenSource();
            logger.LogInformation("MegaMenuService: Invalidated all mega menu caches");
        }
    }

    private async Task<MegaMenu?> BuildMegaMenuAsync(string codeName)
    {
        try
        {
            // Use IContentRetriever — auto-handles channel, preview, language
            var navigationItems = await contentRetriever.RetrievePages<Generic.Navigation>(
                new RetrievePagesParameters
                {
                    LinkedItemsMaxLevel = 2
                },
                additionalQueryConfiguration: query => query
                    .Where(where => where
                        .WhereEquals("NavigationIsMegaMenu", true)
                        .WhereContains("DynamicCodeName", codeName))
                    .OrderBy("WebPageItemOrder"),
                cacheSettings: new RetrievalCacheSettings(
                    cacheItemNameSuffix: $"megamenu|{codeName}"));

            var items = navigationItems.ToList();

            if (items.Count == 0)
            {
                logger.LogDebug("MegaMenuService: No mega menu items found for '{CodeName}'", codeName);
                return null;
            }

            var megaMenu = new MegaMenu
            {
                CodeName = codeName,
                Title = items.First().NavigationLinkText ?? codeName
            };

            // Build columns from navigation items
            foreach (var item in items)
            {
                var column = new MegaMenuColumn
                {
                    Title = item.NavigationLinkText ?? string.Empty,
                    Url = item.NavigationLinkUrl
                };

                // TODO: Load child items for each column
                megaMenu.Columns.Add(column);
            }

            return megaMenu;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MegaMenuService: Error building mega menu '{CodeName}'", codeName);
            return null;
        }
    }

    private static string GetCacheKey(string codeName) => $"{CacheKeyPrefix}{codeName}";
}

/// <summary>
/// Represents a mega menu structure.
/// </summary>
public class MegaMenu
{
    /// <summary>
    /// Code name of the mega menu.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Display title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Columns in the mega menu.
    /// </summary>
    public IList<MegaMenuColumn> Columns { get; set; } = [];

    /// <summary>
    /// Optional featured content for the mega menu.
    /// </summary>
    public MegaMenuFeaturedContent? FeaturedContent { get; set; }

    /// <summary>
    /// Optional call-to-action for the mega menu.
    /// </summary>
    public MegaMenuCta? CallToAction { get; set; }
}

/// <summary>
/// Represents a column in a mega menu.
/// </summary>
public class MegaMenuColumn
{
    /// <summary>
    /// Column header title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL for the column header.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Items in this column.
    /// </summary>
    public IList<MegaMenuItem> Items { get; set; } = [];
}

/// <summary>
/// Represents a single item in a mega menu column.
/// </summary>
public class MegaMenuItem
{
    /// <summary>
    /// Display title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL for the item.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional icon class.
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Optional badge text (e.g., "New", "Popular").
    /// </summary>
    public string? Badge { get; set; }
}

/// <summary>
/// Featured content for mega menu (e.g., promotional banner).
/// </summary>
public class MegaMenuFeaturedContent
{
    /// <summary>
    /// Featured content title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Featured content description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Featured image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Link URL.
    /// </summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Link text.
    /// </summary>
    public string? LinkText { get; set; }
}

/// <summary>
/// Call-to-action for mega menu.
/// </summary>
public class MegaMenuCta
{
    /// <summary>
    /// CTA text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// CTA URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// CSS class for styling.
    /// </summary>
    public string? CssClass { get; set; }
}

/// <summary>
/// Content loaded for a mega menu navigation item.
/// </summary>
public class MegaMenuContent
{
    /// <summary>
    /// Menu title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Columns in the mega menu.
    /// </summary>
    public IList<MegaMenuColumn> Columns { get; set; } = [];

    /// <summary>
    /// Featured content.
    /// </summary>
    public MegaMenuFeaturedContent? FeaturedContent { get; set; }
}
