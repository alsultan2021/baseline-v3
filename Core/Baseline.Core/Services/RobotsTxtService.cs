using System.Text;
using CMS.ContentEngine;
using CMS.DataEngine;
using Microsoft.AspNetCore.Http;

namespace Baseline.Core;

/// <summary>
/// Default implementation of <see cref="IRobotsTxtService"/>.
/// Supports multi-language sitemap generation using Kentico's language configuration.
/// </summary>
internal sealed class RobotsTxtService(
    RobotsTxtOptions options,
    IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
    IHttpContextAccessor httpContextAccessor) : IRobotsTxtService
{
    public async Task<string> GenerateAsync()
    {
        var sb = new StringBuilder();

        if (options.Rules.Count == 0)
        {
            // Default permissive rule
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
        }
        else
        {
            foreach (var rule in options.Rules)
            {
                sb.AppendLine($"User-agent: {rule.UserAgent}");

                foreach (var allow in rule.Allow)
                {
                    sb.AppendLine($"Allow: {allow}");
                }

                foreach (var disallow in rule.Disallow)
                {
                    sb.AppendLine($"Disallow: {disallow}");
                }

                if (rule.CrawlDelay.HasValue)
                {
                    sb.AppendLine($"Crawl-delay: {rule.CrawlDelay}");
                }

                sb.AppendLine();
            }
        }

        // Add sitemaps
        if (options.IncludeSitemap)
        {
            sb.AppendLine();
            var baseUrl = GetBaseUrl();

            if (options.EnableLanguageSpecificSitemaps)
            {
                // Generate language-specific sitemap entries
                var languages = await GetSitemapLanguagesAsync();
                foreach (var language in languages)
                {
                    sb.AppendLine($"Sitemap: {baseUrl}/sitemap-{language}.xml");
                }

                // Also include main sitemap index
                sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
            }
            else
            {
                // Single sitemap for all languages
                sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
            }
        }

        foreach (var sitemap in options.AdditionalSitemaps)
        {
            sb.AppendLine($"Sitemap: {sitemap}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Gets the list of languages for sitemap generation.
    /// Uses configured languages or fetches all available from Kentico.
    /// </summary>
    private async Task<IEnumerable<string>> GetSitemapLanguagesAsync()
    {
        if (options.SitemapLanguages.Count > 0)
        {
            return options.SitemapLanguages;
        }

        // Fetch all languages from Kentico
        var languages = await contentLanguageProvider.Get()
            .GetEnumerableTypedResultAsync();

        return languages.Select(l => l.ContentLanguageName);
    }

    /// <summary>
    /// Gets the base URL for sitemap URLs.
    /// </summary>
    private string GetBaseUrl()
    {
        if (!string.IsNullOrEmpty(options.SitemapBaseUrl))
        {
            return options.SitemapBaseUrl.TrimEnd('/');
        }

        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return string.Empty;
        }

        return $"{request.Scheme}://{request.Host}";
    }
}
