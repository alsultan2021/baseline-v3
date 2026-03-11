using Microsoft.Extensions.Logging;

namespace Baseline.Navigation;

/// <summary>
/// Implementation of dynamic navigation service with provider support.
/// </summary>
public sealed class DynamicNavigationService(
    ILogger<DynamicNavigationService> logger) : IDynamicNavigationService
{
    private readonly List<IDynamicNavigationProvider> _providers = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public async Task<IEnumerable<NavigationItem>> GetDynamicItemsAsync(string menuCodeName)
    {
        var items = new List<NavigationItem>();

        List<IDynamicNavigationProvider> matchingProviders;
        lock (_lock)
        {
            matchingProviders = _providers
                .Where(p => p.SupportedMenus.Contains(menuCodeName, StringComparer.OrdinalIgnoreCase) ||
                           p.SupportedMenus.Contains("*"))
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        foreach (var provider in matchingProviders)
        {
            try
            {
                var providerItems = await provider.GetItemsAsync(menuCodeName);
                items.AddRange(providerItems);
                logger.LogDebug("DynamicNavigationService: Provider {Provider} returned {Count} items for menu '{Menu}'",
                    provider.GetType().Name, providerItems.Count(), menuCodeName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DynamicNavigationService: Error getting items from provider {Provider} for menu '{Menu}'",
                    provider.GetType().Name, menuCodeName);
            }
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NavigationItem>> GetDynamicChildItemsAsync(NavigationItem parent)
    {
        if (!parent.IsDynamic || string.IsNullOrEmpty(parent.DynamicCodeName))
        {
            return [];
        }

        var items = new List<NavigationItem>();

        List<IDynamicNavigationProvider> matchingProviders;
        lock (_lock)
        {
            matchingProviders = _providers
                .Where(p => p.SupportedMenus.Contains(parent.DynamicCodeName, StringComparer.OrdinalIgnoreCase) ||
                           p.SupportedMenus.Contains("*"))
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        foreach (var provider in matchingProviders)
        {
            try
            {
                var providerItems = await provider.GetItemsAsync(parent.DynamicCodeName);
                items.AddRange(providerItems);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DynamicNavigationService: Error getting child items from provider {Provider}",
                    provider.GetType().Name);
            }
        }

        return items;
    }

    /// <inheritdoc />
    public void RegisterProvider(IDynamicNavigationProvider provider)
    {
        lock (_lock)
        {
            // Remove existing provider of same type if present
            _providers.RemoveAll(p => p.GetType() == provider.GetType());
            _providers.Add(provider);

            logger.LogInformation("DynamicNavigationService: Registered provider {Provider} for menus: {Menus}",
                provider.GetType().Name, string.Join(", ", provider.SupportedMenus));
        }
    }
}

/// <summary>
/// Base class for dynamic navigation providers.
/// </summary>
public abstract class DynamicNavigationProviderBase : IDynamicNavigationProvider
{
    /// <inheritdoc />
    public abstract IEnumerable<string> SupportedMenus { get; }

    /// <inheritdoc />
    public virtual int Priority => 0;

    /// <inheritdoc />
    public abstract Task<IEnumerable<NavigationItem>> GetItemsAsync(string menuCodeName);

    /// <summary>
    /// Creates a navigation item with common defaults.
    /// </summary>
    protected static NavigationItem CreateItem(
        string title,
        string url,
        string? description = null,
        string? iconClass = null,
        bool openInNewWindow = false)
    {
        return new NavigationItem
        {
            Title = title,
            Url = url,
            Description = description,
            IconClass = iconClass,
            Target = openInNewWindow ? "_blank" : null,
            IsDynamic = false
        };
    }
}

/// <summary>
/// Sample provider for recent blog posts in navigation.
/// </summary>
/// <remarks>
/// This is an example implementation. Override or create your own providers
/// for specific dynamic navigation needs.
/// </remarks>
public class RecentBlogPostsNavigationProvider : DynamicNavigationProviderBase
{
    private readonly INavigationCacheService? _cacheService;
    private readonly Func<Task<IEnumerable<(string Title, string Url)>>>? _blogPostRetriever;

    /// <summary>
    /// Creates a new instance with optional dependencies.
    /// </summary>
    public RecentBlogPostsNavigationProvider(
        INavigationCacheService? cacheService = null,
        Func<Task<IEnumerable<(string Title, string Url)>>>? blogPostRetriever = null)
    {
        _cacheService = cacheService;
        _blogPostRetriever = blogPostRetriever;
    }

    /// <inheritdoc />
    public override IEnumerable<string> SupportedMenus => ["recent-posts", "blog-nav"];

    /// <inheritdoc />
    public override int Priority => 10;

    /// <inheritdoc />
    public override async Task<IEnumerable<NavigationItem>> GetItemsAsync(string menuCodeName)
    {
        if (_blogPostRetriever is null)
        {
            return [];
        }

        // Use cache if available
        if (_cacheService is not null)
        {
            var cached = await _cacheService.GetOrCreateAsync(
                $"dynamic_blog_nav_{menuCodeName}",
                async () => await FetchBlogPostsAsync(),
                TimeSpan.FromMinutes(10));

            return cached ?? [];
        }

        return await FetchBlogPostsAsync();
    }

    private async Task<List<NavigationItem>> FetchBlogPostsAsync()
    {
        var posts = await _blogPostRetriever!();
        return posts.Select(p => CreateItem(p.Title, p.Url)).ToList();
    }
}

/// <summary>
/// Provider for taxonomy-based navigation (e.g., categories, tags).
/// </summary>
public class TaxonomyNavigationProvider : DynamicNavigationProviderBase
{
    private readonly Func<string, Task<IEnumerable<(string Name, string Url, int Count)>>>? _taxonomyRetriever;

    /// <summary>
    /// Creates a new instance with a taxonomy retriever function.
    /// </summary>
    public TaxonomyNavigationProvider(
        Func<string, Task<IEnumerable<(string Name, string Url, int Count)>>>? taxonomyRetriever = null)
    {
        _taxonomyRetriever = taxonomyRetriever;
    }

    /// <inheritdoc />
    public override IEnumerable<string> SupportedMenus => ["categories", "tags", "taxonomy-nav"];

    /// <inheritdoc />
    public override int Priority => 5;

    /// <inheritdoc />
    public override async Task<IEnumerable<NavigationItem>> GetItemsAsync(string menuCodeName)
    {
        if (_taxonomyRetriever is null)
        {
            return [];
        }

        var taxonomies = await _taxonomyRetriever(menuCodeName);

        return taxonomies.Select(t => new NavigationItem
        {
            Title = t.Name,
            Url = t.Url,
            Description = $"{t.Count} items"
        });
    }
}
