using CMS.ContentEngine;
using CMS.DataEngine;

namespace Baseline.Localization;

/// <summary>
/// Service for retrieving localized strings.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string by key with format arguments.
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string for a specific culture.
    /// </summary>
    string GetString(string key, string cultureCode);

    /// <summary>
    /// Gets a localized string with format arguments for a specific culture.
    /// </summary>
    string GetString(string key, string cultureCode, params object[] args);

    /// <summary>
    /// Tries to get a localized string, returning null if not found.
    /// </summary>
    string? TryGetString(string key);

    /// <summary>
    /// Gets all strings for a culture.
    /// </summary>
    Task<IDictionary<string, string>> GetAllStringsAsync(string? cultureCode = null);

    /// <summary>
    /// Gets strings matching a prefix.
    /// </summary>
    Task<IDictionary<string, string>> GetStringsByPrefixAsync(string prefix, string? cultureCode = null);
}

/// <summary>
/// Service for managing cultures.
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Gets the current culture.
    /// </summary>
    BaselineCultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    IEnumerable<BaselineCultureInfo> SupportedCultures { get; }

    /// <summary>
    /// Gets the default culture.
    /// </summary>
    BaselineCultureInfo DefaultCulture { get; }

    /// <summary>
    /// Sets the current culture.
    /// </summary>
    void SetCulture(string cultureCode);

    /// <summary>
    /// Gets a culture by code.
    /// </summary>
    BaselineCultureInfo? GetCulture(string cultureCode);

    /// <summary>
    /// Checks if a culture is supported.
    /// </summary>
    bool IsCultureSupported(string cultureCode);

    /// <summary>
    /// Gets the URL for the current page in a different culture.
    /// </summary>
    Task<string?> GetCultureUrlAsync(string cultureCode);
}

/// <summary>
/// Service for managing resource strings (admin operations).
/// </summary>
public interface IResourceStringService
{
    /// <summary>
    /// Gets a resource string.
    /// </summary>
    Task<ResourceString?> GetAsync(string key);

    /// <summary>
    /// Creates or updates a resource string.
    /// </summary>
    Task<ResourceStringResult> SaveAsync(ResourceString resourceString);

    /// <summary>
    /// Deletes a resource string.
    /// </summary>
    Task<ResourceStringResult> DeleteAsync(string key);

    /// <summary>
    /// Gets all resource strings with pagination.
    /// </summary>
    Task<PagedResult<ResourceString>> GetAllAsync(ResourceStringQuery query);

    /// <summary>
    /// Imports resource strings from a file.
    /// </summary>
    Task<ImportResult> ImportAsync(Stream fileStream, string format);

    /// <summary>
    /// Exports resource strings to a file.
    /// </summary>
    Task<Stream> ExportAsync(string format, string? cultureCode = null);

    /// <summary>
    /// Gets missing translations for a culture.
    /// </summary>
    Task<IEnumerable<string>> GetMissingTranslationsAsync(string cultureCode);
}

/// <summary>
/// Service for URL localization.
/// </summary>
public interface ILocalizedUrlService
{
    /// <summary>
    /// Gets the localized URL for a page.
    /// </summary>
    Task<string?> GetLocalizedUrlAsync(int contentItemId, string cultureCode);

    /// <summary>
    /// Gets alternate language URLs for SEO (hreflang).
    /// </summary>
    Task<IEnumerable<AlternateLanguageUrl>> GetAlternateUrlsAsync(int contentItemId);

    /// <summary>
    /// Gets the canonical URL for the current page.
    /// </summary>
    Task<string?> GetCanonicalUrlAsync();

    /// <summary>
    /// Resolves the culture from a URL.
    /// </summary>
    string? ResolveCultureFromUrl(string url);

    /// <summary>
    /// Builds a localized URL.
    /// </summary>
    string BuildLocalizedUrl(string path, string cultureCode);
}

/// <summary>
/// Repository for localized category operations.
/// </summary>
public interface ILocalizedCategoryRepository
{
    /// <summary>
    /// Gets categories with localized names.
    /// </summary>
    Task<IEnumerable<LocalizedCategory>> GetCategoriesAsync(string? cultureCode = null);

    /// <summary>
    /// Gets a localized category by ID.
    /// </summary>
    Task<LocalizedCategory?> GetCategoryAsync(int categoryId, string? cultureCode = null);

    /// <summary>
    /// Gets localized categories by parent.
    /// </summary>
    Task<IEnumerable<LocalizedCategory>> GetChildCategoriesAsync(int parentCategoryId, string? cultureCode = null);
}

/// <summary>
/// Represents a localized category.
/// </summary>
public sealed record LocalizedCategory(
    int CategoryId,
    string CodeName,
    string DisplayName,
    string? Description,
    int? ParentCategoryId);

/// <summary>
/// Default implementation of ILocalizedCategoryRepository using XbK taxonomy API.
/// </summary>
public sealed class LocalizedCategoryRepository(
    ITaxonomyRetriever taxonomyRetriever,
    IInfoProvider<TagInfo> tagInfoProvider) : ILocalizedCategoryRepository
{
    // Cache taxonomies to avoid repeated lookups
    private readonly Dictionary<string, TaxonomyData> _taxonomyCache = [];

    public async Task<IEnumerable<LocalizedCategory>> GetCategoriesAsync(string? cultureCode = null)
    {
        var culture = cultureCode ?? System.Globalization.CultureInfo.CurrentCulture.Name;
        var categories = new List<LocalizedCategory>();

        // Get all tags from the system
        var tags = tagInfoProvider.Get().GetEnumerableTypedResult();

        foreach (var tagInfo in tags)
        {
            var category = await MapTagToLocalizedCategoryAsync(tagInfo, culture);
            categories.Add(category);
        }

        return categories;
    }

    public async Task<LocalizedCategory?> GetCategoryAsync(int categoryId, string? cultureCode = null)
    {
        var culture = cultureCode ?? System.Globalization.CultureInfo.CurrentCulture.Name;

        var tagInfo = tagInfoProvider.Get()
            .WhereEquals(nameof(TagInfo.TagID), categoryId)
            .FirstOrDefault();

        if (tagInfo is null)
        {
            return null;
        }

        return await MapTagToLocalizedCategoryAsync(tagInfo, culture);
    }

    public async Task<IEnumerable<LocalizedCategory>> GetChildCategoriesAsync(int parentCategoryId, string? cultureCode = null)
    {
        var culture = cultureCode ?? System.Globalization.CultureInfo.CurrentCulture.Name;
        var categories = new List<LocalizedCategory>();

        var childTags = tagInfoProvider.Get()
            .WhereEquals(nameof(TagInfo.TagParentID), parentCategoryId)
            .GetEnumerableTypedResult();

        foreach (var tagInfo in childTags)
        {
            var category = await MapTagToLocalizedCategoryAsync(tagInfo, culture);
            categories.Add(category);
        }

        return categories;
    }

    private async Task<LocalizedCategory> MapTagToLocalizedCategoryAsync(TagInfo tagInfo, string cultureCode)
    {
        // Try to get localized title/description from taxonomy API
        var displayName = tagInfo.TagTitle;
        string? description = null;

        try
        {
            // Use ITaxonomyRetriever to get localized tag data
            var tags = await taxonomyRetriever.RetrieveTags([tagInfo.TagGUID], cultureCode);
            var localizedTag = tags.FirstOrDefault();
            if (localizedTag is not null)
            {
                displayName = localizedTag.Title ?? displayName;
                description = localizedTag.Description;
            }
        }
        catch
        {
            // Fall back to default values if retrieval fails
        }

        return new LocalizedCategory(
            CategoryId: tagInfo.TagID,
            CodeName: tagInfo.TagName,
            DisplayName: displayName,
            Description: description,
            ParentCategoryId: tagInfo.TagParentID > 0 ? tagInfo.TagParentID : null
        );
    }
}
