using CMS.Websites;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Localization.Services;

/// <summary>
/// Service for generating SEO-friendly multilingual link elements.
/// Implements hreflang tags for search engine optimization.
/// 
/// Reference: https://developers.google.com/search/docs/specialty/international/localized-versions
/// </summary>
public interface IHreflangService
{
    /// <summary>
    /// Gets hreflang link elements for the current page.
    /// </summary>
    Task<IEnumerable<HreflangLink>> GetHreflangLinksAsync();

    /// <summary>
    /// Gets hreflang links for a specific content item.
    /// </summary>
    Task<IEnumerable<HreflangLink>> GetHreflangLinksAsync(Guid contentItemGuid);

    /// <summary>
    /// Gets hreflang links for a specific content item by web page ID.
    /// </summary>
    Task<IEnumerable<HreflangLink>> GetHreflangLinksAsync(int webPageItemId);

    /// <summary>
    /// Generates the HTML for hreflang link elements.
    /// </summary>
    Task<string> GenerateHreflangHtmlAsync();

    /// <summary>
    /// Generates the HTML for hreflang link elements for a specific content item.
    /// </summary>
    Task<string> GenerateHreflangHtmlAsync(Guid contentItemGuid);

    /// <summary>
    /// Gets the x-default URL (typically primary language).
    /// </summary>
    Task<string?> GetDefaultUrlAsync();
}

/// <summary>
/// Represents a single hreflang link.
/// </summary>
public record HreflangLink
{
    /// <summary>
    /// The hreflang attribute value (e.g., "en-US", "fr", "x-default").
    /// </summary>
    public required string Hreflang { get; init; }

    /// <summary>
    /// The absolute URL for this language variant.
    /// </summary>
    public required string Href { get; init; }

    /// <summary>
    /// Language code (without region).
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Region code (if specified).
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Whether this is the x-default link.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Whether this is the current page's language.
    /// </summary>
    public bool IsCurrent { get; init; }

    /// <summary>
    /// Generates the HTML link element.
    /// </summary>
    public string ToHtml() =>
        $"<link rel=\"alternate\" hreflang=\"{System.Net.WebUtility.HtmlEncode(Hreflang)}\" href=\"{System.Net.WebUtility.HtmlEncode(Href)}\" />";
}

/// <summary>
/// Configuration for hreflang generation.
/// </summary>
public class HreflangOptions
{
    /// <summary>
    /// Include x-default link pointing to primary language.
    /// Default: true
    /// </summary>
    public bool IncludeXDefault { get; set; } = true;

    /// <summary>
    /// Include self-referencing link for current language.
    /// Default: true
    /// </summary>
    public bool IncludeSelfReference { get; set; } = true;

    /// <summary>
    /// Only include published language variants.
    /// Default: true
    /// </summary>
    public bool OnlyPublishedVariants { get; set; } = true;

    /// <summary>
    /// Use lowercase hreflang values.
    /// Default: true (per Google recommendations)
    /// </summary>
    public bool UseLowercaseHreflang { get; set; } = true;

    /// <summary>
    /// Map of language codes to hreflang values.
    /// Useful for mapping "en" to "en-US" or regional variants.
    /// </summary>
    public Dictionary<string, string> LanguageToHreflangMap { get; set; } = [];

    /// <summary>
    /// Languages to exclude from hreflang output.
    /// </summary>
    public List<string> ExcludedLanguages { get; set; } = [];
}

/// <summary>
/// Default implementation of hreflang service.
/// </summary>
public sealed class HreflangService(
    ILanguageService languageService,
    IWebPageUrlRetriever webPageUrlRetriever,
    IHttpContextAccessor httpContextAccessor,
    HreflangOptions options) : IHreflangService
{
    public async Task<IEnumerable<HreflangLink>> GetHreflangLinksAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return [];
        }

        // Try to get the current web page from route data
        var webPageItemId = httpContext.Items["WebPageItemId"] as int?;
        if (webPageItemId.HasValue)
        {
            return await GetHreflangLinksAsync(webPageItemId.Value);
        }

        // Fallback: return empty if we can't determine the current page
        return [];
    }

    public async Task<IEnumerable<HreflangLink>> GetHreflangLinksAsync(Guid contentItemGuid)
    {
        var currentLanguage = languageService.GetCurrentLanguage();
        var variants = await languageService.GetContentLanguageVariantsAsync(contentItemGuid);
        var allLanguages = await languageService.GetAllLanguagesAsync();
        var primaryLanguage = await languageService.GetPrimaryLanguageAsync();

        var links = new List<HreflangLink>();

        foreach (var variant in variants.Where(v => v.Exists))
        {
            if (options.OnlyPublishedVariants && !variant.IsPublished)
            {
                continue;
            }

            if (options.ExcludedLanguages.Contains(variant.LanguageCode, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var langInfo = allLanguages.FirstOrDefault(l =>
                l.Code.Equals(variant.LanguageCode, StringComparison.OrdinalIgnoreCase));

            if (langInfo is null || string.IsNullOrEmpty(variant.Url))
            {
                continue;
            }

            var hreflang = GetHreflangValue(variant.LanguageCode, langInfo.FormattingCulture);
            var isCurrent = variant.LanguageCode.Equals(currentLanguage, StringComparison.OrdinalIgnoreCase);
            var isDefault = variant.LanguageCode.Equals(primaryLanguage, StringComparison.OrdinalIgnoreCase);

            if (!options.IncludeSelfReference && isCurrent)
            {
                continue;
            }

            links.Add(new HreflangLink
            {
                Hreflang = hreflang,
                Href = variant.Url,
                Language = GetLanguagePart(hreflang),
                Region = GetRegionPart(hreflang),
                IsCurrent = isCurrent,
                IsDefault = false
            });

            // Add x-default for primary language
            if (options.IncludeXDefault && isDefault)
            {
                links.Add(new HreflangLink
                {
                    Hreflang = "x-default",
                    Href = variant.Url,
                    IsDefault = true,
                    IsCurrent = isCurrent
                });
            }
        }

        return links;
    }

    public async Task<IEnumerable<HreflangLink>> GetHreflangLinksAsync(int webPageItemId)
    {
        var currentLanguage = languageService.GetCurrentLanguage();
        var allLanguages = await languageService.GetAllLanguagesAsync();
        var primaryLanguage = await languageService.GetPrimaryLanguageAsync();

        var links = new List<HreflangLink>();
        var httpContext = httpContextAccessor.HttpContext;
        var baseUrl = httpContext is not null
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : string.Empty;

        foreach (var lang in allLanguages)
        {
            if (options.ExcludedLanguages.Contains(lang.Code, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var url = await webPageUrlRetriever.Retrieve(webPageItemId, lang.Code);
                if (url is null)
                {
                    continue;
                }

                var absoluteUrl = url.AbsoluteUrl ?? $"{baseUrl}{url.RelativePath}";
                var hreflang = GetHreflangValue(lang.Code, lang.FormattingCulture);
                var isCurrent = lang.Code.Equals(currentLanguage, StringComparison.OrdinalIgnoreCase);
                var isDefault = lang.Code.Equals(primaryLanguage, StringComparison.OrdinalIgnoreCase);

                if (!options.IncludeSelfReference && isCurrent)
                {
                    continue;
                }

                links.Add(new HreflangLink
                {
                    Hreflang = hreflang,
                    Href = absoluteUrl,
                    Language = GetLanguagePart(hreflang),
                    Region = GetRegionPart(hreflang),
                    IsCurrent = isCurrent,
                    IsDefault = false
                });

                // Add x-default for primary language
                if (options.IncludeXDefault && isDefault)
                {
                    links.Add(new HreflangLink
                    {
                        Hreflang = "x-default",
                        Href = absoluteUrl,
                        IsDefault = true,
                        IsCurrent = isCurrent
                    });
                }
            }
            catch
            {
                // URL not available for this language - skip
            }
        }

        return links;
    }

    public async Task<string> GenerateHreflangHtmlAsync()
    {
        var links = await GetHreflangLinksAsync();
        return string.Join(Environment.NewLine, links.Select(l => l.ToHtml()));
    }

    public async Task<string> GenerateHreflangHtmlAsync(Guid contentItemGuid)
    {
        var links = await GetHreflangLinksAsync(contentItemGuid);
        return string.Join(Environment.NewLine, links.Select(l => l.ToHtml()));
    }

    public async Task<string?> GetDefaultUrlAsync()
    {
        var links = await GetHreflangLinksAsync();
        return links.FirstOrDefault(l => l.IsDefault)?.Href;
    }

    private string GetHreflangValue(string languageCode, string formattingCulture)
    {
        // Check custom mapping first
        if (options.LanguageToHreflangMap.TryGetValue(languageCode, out var mapped))
        {
            return options.UseLowercaseHreflang ? mapped.ToLowerInvariant() : mapped;
        }

        // Use formatting culture if it contains region info
        var hreflang = formattingCulture.Contains('-') ? formattingCulture : languageCode;

        return options.UseLowercaseHreflang ? hreflang.ToLowerInvariant() : hreflang;
    }

    private static string? GetLanguagePart(string hreflang)
    {
        if (hreflang == "x-default")
        {
            return null;
        }

        var parts = hreflang.Split('-');
        return parts.Length > 0 ? parts[0] : null;
    }

    private static string? GetRegionPart(string hreflang)
    {
        if (hreflang == "x-default")
        {
            return null;
        }

        var parts = hreflang.Split('-');
        return parts.Length > 1 ? parts[1] : null;
    }
}

/// <summary>
/// Tag helper for rendering hreflang links in the &lt;head&gt;.
/// 
/// Usage: &lt;hreflang-links /&gt;
/// </summary>
[HtmlTargetElement("hreflang-links", TagStructure = TagStructure.WithoutEndTag)]
public class HreflangLinksTagHelper : TagHelper
{
    private readonly IHreflangService _hreflangService;

    public HreflangLinksTagHelper(IHreflangService hreflangService)
    {
        _hreflangService = hreflangService;
    }

    /// <summary>
    /// Optional: Specific content item GUID.
    /// </summary>
    [HtmlAttributeName("content-item-guid")]
    public Guid? ContentItemGuid { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Remove the tag itself, just output content

        var html = ContentItemGuid.HasValue
            ? await _hreflangService.GenerateHreflangHtmlAsync(ContentItemGuid.Value)
            : await _hreflangService.GenerateHreflangHtmlAsync();

        output.Content.SetHtmlContent(html);
    }
}

// Note: LanguageSwitcherViewComponent has been removed from this file.
// Use Baseline.Localization.Components.LanguageSelectorViewComponent from the RCL project instead.
