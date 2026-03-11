using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Core.Seo;

/// <summary>
/// Default <see cref="ISitemapService"/> implementation.
/// Aggregates entries from all registered <see cref="ISitemapEntryProvider"/> instances
/// and generates sitemap XML per the <a href="https://www.sitemaps.org/protocol.html">Sitemap protocol</a>.
/// </summary>
public class SitemapService(
    IEnumerable<ISitemapEntryProvider> entryProviders,
    IOptions<BaselineCoreOptions> options,
    ILogger<SitemapService> logger) : ISitemapService
{
    private readonly RobotsTxtOptions _robotsOptions = options.Value.RobotsTxt;

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string? language = null, CancellationToken cancellationToken = default)
    {
        var entries = await GetEntriesAsync(language, cancellationToken);
        return BuildSitemapXml(entries);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateIndexAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = _robotsOptions.SitemapBaseUrl?.TrimEnd('/') ?? string.Empty;
        var languages = _robotsOptions.SitemapLanguages;

        if (languages.Count == 0)
        {
            logger.LogWarning("GenerateIndexAsync called but no SitemapLanguages configured");
            return BuildSitemapIndexXml([new SitemapIndexEntry($"{baseUrl}/sitemap.xml", DateTime.UtcNow)]);
        }

        var indexEntries = languages
            .Select(lang => new SitemapIndexEntry($"{baseUrl}/sitemap-{lang}.xml", DateTime.UtcNow))
            .ToList();

        return BuildSitemapIndexXml(indexEntries);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SitemapEntry>> GetEntriesAsync(
        string? language = null, CancellationToken cancellationToken = default)
    {
        var ordered = entryProviders.OrderByDescending(p => p.Priority);
        var allEntries = new List<SitemapEntry>();

        foreach (var provider in ordered)
        {
            try
            {
                var entries = await provider.GetEntriesAsync(language, cancellationToken);
                allEntries.AddRange(entries);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sitemap provider {Provider} failed", provider.GetType().Name);
            }
        }

        return allEntries;
    }

    private static string BuildSitemapXml(IEnumerable<SitemapEntry> entries)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        // Add xhtml namespace for hreflang
        writer.WriteAttributeString("xmlns", "xhtml", null, "http://www.w3.org/1999/xhtml");

        foreach (var entry in entries)
        {
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", entry.Url);

            if (entry.LastModified.HasValue)
                writer.WriteElementString("lastmod", entry.LastModified.Value.ToString("yyyy-MM-dd"));

            if (entry.ChangeFrequency.HasValue)
                writer.WriteElementString("changefreq", entry.ChangeFrequency.Value.ToString().ToLowerInvariant());

            writer.WriteElementString("priority", entry.Priority.ToString("F1"));

            // hreflang alternates
            if (entry.AlternateLanguageUrls is { Count: > 0 })
            {
                foreach (var (lang, url) in entry.AlternateLanguageUrls)
                {
                    writer.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
                    writer.WriteAttributeString("rel", "alternate");
                    writer.WriteAttributeString("hreflang", lang);
                    writer.WriteAttributeString("href", url);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement(); // url
        }

        writer.WriteEndElement(); // urlset
        writer.WriteEndDocument();
        writer.Flush();

        return sb.ToString();
    }

    private static string BuildSitemapIndexXml(IEnumerable<SitemapIndexEntry> entries)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

        foreach (var entry in entries)
        {
            writer.WriteStartElement("sitemap");
            writer.WriteElementString("loc", entry.Url);
            writer.WriteElementString("lastmod", entry.LastModified.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();

        return sb.ToString();
    }

    private record SitemapIndexEntry(string Url, DateTime LastModified);
}
