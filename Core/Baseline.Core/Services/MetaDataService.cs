using Microsoft.Extensions.Options;

namespace Baseline.Core;

/// <summary>
/// Scoped implementation of IMetaDataService.
/// Stores and retrieves page metadata per HTTP request scope.
/// This replaces project-specific services like WebPageMetaService.
/// </summary>
public class MetaDataService(
    IOptions<BaselineCoreOptions> options,
    IBaselineContentRetriever contentRetriever,
    ILanguageService languageService) : IMetaDataService
{
    private readonly BaselineCoreOptions _options = options.Value;
    private readonly IBaselineContentRetriever _contentRetriever = contentRetriever;
    private readonly ILanguageService _languageService = languageService;

    /// <summary>
    /// Current page metadata stored for this request scope.
    /// </summary>
    private BaselinePageMetaData _currentMetaData = new();

    /// <summary>
    /// Sets page metadata from IBaseMetadata fields (Kentico reusable schema).
    /// Call this from page templates/controllers.
    /// </summary>
    public void SetFromBaseMetadata(IBaseMetadata metaFields, string? ogImageUrl = null, Guid? pageGuid = null)
    {
        var ogLocale = GetOgLocale();

        _currentMetaData = new BaselinePageMetaData
        {
            PageGuid = pageGuid,
            Title = metaFields.MetaData_Title ?? metaFields.MetaData_PageName ?? string.Empty,
            Description = metaFields.MetaData_Description ?? string.Empty,
            Keywords = metaFields.MetaData_Keywords,
            Robots = metaFields.MetaData_NoIndex ? "noindex, nofollow" : null,
            OpenGraph = new OpenGraphData
            {
                Title = metaFields.MetaData_Title ?? metaFields.MetaData_PageName,
                Description = metaFields.MetaData_Description,
                Image = ogImageUrl ?? GetOgImageFromMetadata(metaFields),
                Type = "website",
                Locale = ogLocale.Current,
                AlternateLocales = ogLocale.Alternates
            },
            TwitterCard = new TwitterCardData
            {
                Title = metaFields.MetaData_Title ?? metaFields.MetaData_PageName,
                Description = metaFields.MetaData_Description,
                Image = ogImageUrl ?? GetOgImageFromMetadata(metaFields)
            }
        };
    }

    /// <summary>
    /// Sets page metadata directly.
    /// </summary>
    public void SetMetaData(BaselinePageMetaData metaData) => _currentMetaData = metaData;

    /// <summary>
    /// Sets page metadata from simple values.
    /// Use for content types that don't implement IBaseMetadata.
    /// </summary>
    public void SetSimpleMetadata(string title, string? description = null, string? ogImageUrl = null, bool noIndex = false, Guid? pageGuid = null)
    {
        var ogLocale = GetOgLocale();

        _currentMetaData = new BaselinePageMetaData
        {
            PageGuid = pageGuid,
            Title = title,
            Description = description,
            Robots = noIndex ? "noindex, nofollow" : null,
            OpenGraph = new OpenGraphData
            {
                Title = title,
                Description = description,
                Image = ogImageUrl,
                Type = "website",
                Locale = ogLocale.Current,
                AlternateLocales = ogLocale.Alternates
            },
            TwitterCard = new TwitterCardData
            {
                Title = title,
                Description = description,
                Image = ogImageUrl
            }
        };
    }

    /// <summary>
    /// Updates specific fields while preserving others.
    /// </summary>
    public void UpdateTitle(string title) => _currentMetaData.Title = title;

    /// <summary>
    /// Applies title pattern from global settings (e.g., "{0} | Site Name").
    /// </summary>
    public void ApplyTitlePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return;
        var formattedTitle = string.Format(pattern, _currentMetaData.Title).Trim('|', ' ');
        _currentMetaData.Title = formattedTitle;
    }

    public async Task<BaselinePageMetaData> GetPageMetaDataAsync()
    {
        await PopulateOgAlternateLocalesAsync();
        return _currentMetaData;
    }

    public async Task<BaselinePageMetaData> GetMetaDataForContentAsync(int contentItemId)
    {
        await Task.CompletedTask;
        return new BaselinePageMetaData();
    }

    public async Task<string> GenerateMetaTagsAsync()
    {
        var meta = await GetPageMetaDataAsync();
        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(meta.Description))
            sb.AppendLine($"<meta name=\"description\" content=\"{HtmlEncode(meta.Description)}\" />");

        if (!string.IsNullOrEmpty(meta.Keywords))
            sb.AppendLine($"<meta name=\"keywords\" content=\"{HtmlEncode(meta.Keywords)}\" />");

        if (!string.IsNullOrEmpty(meta.Robots))
            sb.AppendLine($"<meta name=\"robots\" content=\"{meta.Robots}\" />");

        if (!string.IsNullOrEmpty(meta.CanonicalUrl))
            sb.AppendLine($"<link rel=\"canonical\" href=\"{meta.CanonicalUrl}\" />");

        // Open Graph tags
        if (meta.OpenGraph is { } og)
        {
            if (!string.IsNullOrEmpty(og.Title))
                sb.AppendLine($"<meta property=\"og:title\" content=\"{HtmlEncode(og.Title)}\" />");
            if (!string.IsNullOrEmpty(og.Description))
                sb.AppendLine($"<meta property=\"og:description\" content=\"{HtmlEncode(og.Description)}\" />");
            if (!string.IsNullOrEmpty(og.Image))
                sb.AppendLine($"<meta property=\"og:image\" content=\"{og.Image}\" />");
            if (!string.IsNullOrEmpty(og.Type))
                sb.AppendLine($"<meta property=\"og:type\" content=\"{og.Type}\" />");
            if (!string.IsNullOrEmpty(og.Url))
                sb.AppendLine($"<meta property=\"og:url\" content=\"{og.Url}\" />");
            if (!string.IsNullOrEmpty(og.Locale))
                sb.AppendLine($"<meta property=\"og:locale\" content=\"{og.Locale}\" />");
            if (og.AlternateLocales?.Any() == true)
            {
                foreach (var altLocale in og.AlternateLocales)
                    sb.AppendLine($"<meta property=\"og:locale:alternate\" content=\"{altLocale}\" />");
            }
        }

        // Twitter Card tags
        if (meta.TwitterCard is { } tc)
        {
            sb.AppendLine($"<meta name=\"twitter:card\" content=\"{tc.Card}\" />");
            if (!string.IsNullOrEmpty(tc.Title))
                sb.AppendLine($"<meta name=\"twitter:title\" content=\"{HtmlEncode(tc.Title)}\" />");
            if (!string.IsNullOrEmpty(tc.Description))
                sb.AppendLine($"<meta name=\"twitter:description\" content=\"{HtmlEncode(tc.Description)}\" />");
            if (!string.IsNullOrEmpty(tc.Image))
                sb.AppendLine($"<meta name=\"twitter:image\" content=\"{tc.Image}\" />");
        }

        // Alternate links
        foreach (var alt in meta.AlternateLinks)
        {
            sb.AppendLine($"<link rel=\"alternate\" hreflang=\"{alt.Hreflang}\" href=\"{alt.Href}\" />");
        }

        return sb.ToString();
    }

    public async Task<OpenGraphData> GetOpenGraphDataAsync()
    {
        var meta = await GetPageMetaDataAsync();
        return meta.OpenGraph ?? new OpenGraphData();
    }

    public async Task<TwitterCardData> GetTwitterCardDataAsync()
    {
        var meta = await GetPageMetaDataAsync();
        return meta.TwitterCard ?? new TwitterCardData();
    }

    /// <summary>
    /// Gets the current OG locale and alternate locales from the language service.
    /// OG locale format uses underscores: "en_US", "fr_FR".
    /// </summary>
    private (string? Current, IEnumerable<string>? Alternates) GetOgLocale()
    {
        var currentLang = _languageService.GetCurrentLanguage();
        var currentLocale = CultureCodeToOgLocale(currentLang.CultureCode);

        // Fire-and-forget async call is avoided; use sync approach
        // Alternate locales are populated async via GetAvailableLanguagesAsync
        return (currentLocale, null);
    }

    /// <summary>
    /// Populates OG locale including alternate locales (async path).
    /// Call when alternate locales are needed (e.g., in GetPageMetaDataAsync).
    /// </summary>
    private async Task PopulateOgAlternateLocalesAsync()
    {
        if (_currentMetaData.OpenGraph is null) return;

        var allLanguages = await _languageService.GetAvailableLanguagesAsync();
        var currentCode = _languageService.GetCurrentLanguage().CultureCode;

        _currentMetaData.OpenGraph.AlternateLocales = allLanguages
            .Where(l => !string.Equals(l.CultureCode, currentCode, StringComparison.OrdinalIgnoreCase))
            .Select(l => CultureCodeToOgLocale(l.CultureCode))
            .Where(l => l is not null)
            .Cast<string>()
            .ToArray();
    }

    /// <summary>
    /// Converts a culture code like "en-US" to OG locale format "en_US".
    /// </summary>
    private static string? CultureCodeToOgLocale(string? cultureCode) =>
        cultureCode?.Replace('-', '_');

    /// <summary>
    /// Extracts OG image URL from IBaseMetadata.MetaData_OGImage collection.
    /// Delegates to shared <see cref="OgImageHelper.ExtractImageUrl"/> for fallback chain.
    /// </summary>
    private static string? GetOgImageFromMetadata(IBaseMetadata metaFields)
    {
        if (metaFields.MetaData_OGImage?.Any() != true)
            return null;

        return OgImageHelper.ExtractImageUrl(metaFields.MetaData_OGImage.FirstOrDefault());
    }

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}

/// <summary>
/// Default implementation of IHtmlMinificationService.
/// Preserves whitespace inside pre, code, script, textarea, and style elements.
/// </summary>
public partial class HtmlMinificationService(IOptions<BaselineCoreOptions> options) : IHtmlMinificationService
{
    private readonly BaselineCoreOptions _options = options.Value;

    // Matches blocks that must preserve whitespace (pre, code, script, textarea, style)
    [System.Text.RegularExpressions.GeneratedRegex(
        @"(<(?:pre|code|script|textarea|style)\b[^>]*>.*?</(?:pre|code|script|textarea|style)>)",
        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex PreservedBlockRegex();

    // Matches multiple whitespace outside preserved blocks
    [System.Text.RegularExpressions.GeneratedRegex(@"\s{2,}")]
    private static partial System.Text.RegularExpressions.Regex MultiWhitespaceRegex();

    public bool IsEnabled => _options.EnableHtmlMinification;

    public string Minify(string html)
    {
        if (!IsEnabled || string.IsNullOrEmpty(html))
            return html;

        // Extract preserved blocks, replace with placeholders, minify, then restore
        var preserved = new List<string>();
        var withPlaceholders = PreservedBlockRegex().Replace(html, match =>
        {
            var index = preserved.Count;
            preserved.Add(match.Value);
            return $"\x00PRESERVE_{index}\x00";
        });

        // Collapse multiple whitespace to single space (outside preserved blocks)
        var minified = MultiWhitespaceRegex().Replace(withPlaceholders, " ");

        // Restore preserved blocks
        for (var i = 0; i < preserved.Count; i++)
        {
            minified = minified.Replace($"\x00PRESERVE_{i}\x00", preserved[i]);
        }

        return minified;
    }
}

/// <summary>
/// Default implementation of IAdsTxtService.
/// </summary>
public class AdsTxtService(IOptions<BaselineCoreOptions> options) : IAdsTxtService
{
    private readonly BaselineCoreOptions _options = options.Value;

    public Task<string> GenerateAsync()
    {
        if (!_options.AdsTxt.Enabled)
            return Task.FromResult(string.Empty);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# ads.txt");
        sb.AppendLine($"# Generated by Baseline v3 on {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();

        foreach (var entry in _options.AdsTxt.Entries)
        {
            sb.AppendLine(entry);
        }

        return Task.FromResult(sb.ToString());
    }
}
