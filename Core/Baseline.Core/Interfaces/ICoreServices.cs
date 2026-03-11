using CMS.Websites;
using CSharpFunctionalExtensions;

namespace Baseline.Core;

/// <summary>
/// V3 native: Service for managing language/culture information.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the current language/culture code.
    /// </summary>
    LanguageInfo GetCurrentLanguage();

    /// <summary>
    /// Gets all available languages for the current site.
    /// </summary>
    Task<IEnumerable<LanguageInfo>> GetAvailableLanguagesAsync();

    /// <summary>
    /// Gets a language by its culture code.
    /// </summary>
    Task<LanguageInfo?> GetLanguageAsync(string cultureCode);

    /// <summary>
    /// Gets the default language for the current site.
    /// </summary>
    Task<LanguageInfo?> GetDefaultLanguageAsync();
}

/// <summary>
/// V3 native: Language information record.
/// </summary>
public record LanguageInfo
{
    public string CultureCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string? UrlPrefix { get; init; }
    public string? NativeName { get; init; }

    // V2 compatibility aliases
    /// <summary>V2 compatibility: Alias for CultureCode</summary>
    public string CodeName => CultureCode;
    /// <summary>V2 compatibility: Alias for CultureCode</summary>
    public string Culture => CultureCode;
}

/// <summary>
/// V3 native: Interface for custom page metadata conversion.
/// Allows sites to customize how page content is converted to SEO metadata.
/// </summary>
public interface IContentItemToPageMetadataConverter
{
    /// <summary>
    /// Attempts to convert a web page content to page metadata.
    /// Return Result.Failure to use default conversion logic.
    /// </summary>
    Task<CSharpFunctionalExtensions.Result<PageMetaData>> MapAndGetPageMetadata(IWebPageFieldsSource webPageFieldsSource, PageMetaData baseMetaData);

    /// <summary>
    /// Legacy method for IWebPageContentQueryDataContainer support.
    /// </summary>
    Task<CSharpFunctionalExtensions.Result<PageMetaData>> MapAndGetPageMetadata(IWebPageContentQueryDataContainer webPageContentQueryDataContainer, PageMetaData baseMetaData);
}

/// <summary>
/// V3 native: Interface for URL resolution.
/// </summary>
public interface IUrlResolver
{
    /// <summary>
    /// Converts a relative URL to an absolute URL.
    /// </summary>
    string GetAbsoluteUrl(string relativeUrl);

    /// <summary>
    /// Resolves a URL (handles virtual paths, etc.).
    /// </summary>
    string ResolveUrl(string url);
}

/// <summary>
/// V3 native: Interface for page context/identity.
/// </summary>
public interface IPageContextService
{
    /// <summary>
    /// Gets the current page identity.
    /// </summary>
    Task<PageIdentity?> GetCurrentPageAsync();

    /// <summary>
    /// Gets page identity by tree identity.
    /// </summary>
    Task<PageIdentity?> GetPageAsync(TreeIdentity identity);

    /// <summary>
    /// Gets page identity by tree culture identity.
    /// </summary>
    Task<PageIdentity?> GetPageAsync(TreeCultureIdentity identity);
}

/// <summary>
/// V3 native: Interface for page context repository (alias for IPageContextService).
/// Returns Maybe for compatibility with existing code patterns.
/// </summary>
public interface IPageContextRepository
{
    /// <summary>
    /// Gets the current page identity.
    /// </summary>
    Task<CSharpFunctionalExtensions.Maybe<PageIdentity>> GetCurrentPageAsync();

    /// <summary>
    /// Gets page identity by tree identity.
    /// </summary>
    Task<CSharpFunctionalExtensions.Maybe<PageIdentity>> GetPageAsync(TreeIdentity identity);

    /// <summary>
    /// Gets page identity by tree culture identity.
    /// </summary>
    Task<CSharpFunctionalExtensions.Maybe<PageIdentity>> GetPageAsync(TreeCultureIdentity identity);
}

// Note: Use IMetaDataService (in IMetaDataService.cs) for metadata operations.
// IMetaDataService provides GetPageMetaDataAsync(), GetMetaDataForContentAsync(), etc.

/// <summary>
/// V3 native: Factory for creating page identities in views.
/// </summary>
public interface IPageIdentityFactory
{
    /// <summary>
    /// Creates a page identity from a content item.
    /// </summary>
    PageIdentity Create(int contentItemId, string name, string path);
}

/// <summary>
/// V3 native: Marker class for shared localization resources.
/// </summary>
public class SharedResources { }
