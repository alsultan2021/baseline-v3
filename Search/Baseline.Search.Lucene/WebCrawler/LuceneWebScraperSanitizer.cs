using AngleSharp;
using AngleSharp.Dom;
using System.Collections.Frozen;
using System.Text;

namespace Baseline.Search.Lucene;

/// <summary>
/// Sanitizes HTML content for indexing.
/// </summary>
public sealed class LuceneWebScraperSanitizer
{
    private readonly LuceneSearchOptions _options;

    private static readonly FrozenSet<string> DefaultExcludeTags = new HashSet<string>
    {
        "script", "style", "noscript", "iframe", "svg", "canvas",
        "audio", "video", "object", "embed", "head", "meta", "link"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public LuceneWebScraperSanitizer(LuceneSearchOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Extracts and sanitizes text content from HTML.
    /// </summary>
    public async Task<string> SanitizeHtmlDocumentAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        try
        {
            var config = Configuration.Default;
            using var context = BrowsingContext.New(config);
            using var document = await context.OpenAsync(req => req.Content(html));

            // Remove excluded elements
            RemoveExcludedElements(document);

            // Try to get content from specific selectors first
            var content = ExtractFromSelectors(document, _options.IncludeSelectors);
            if (!string.IsNullOrWhiteSpace(content))
            {
                return NormalizeWhitespace(content);
            }

            // Fall back to body content
            var body = document.Body;
            if (body is null)
            {
                return string.Empty;
            }

            return NormalizeWhitespace(body.TextContent);
        }
        catch
        {
            // If parsing fails, return empty string
            return string.Empty;
        }
    }

    /// <summary>
    /// Extracts meta description from HTML.
    /// </summary>
    public async Task<string?> ExtractMetaDescriptionAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        try
        {
            var config = Configuration.Default;
            using var context = BrowsingContext.New(config);
            using var document = await context.OpenAsync(req => req.Content(html));

            var metaDesc = document.QuerySelector("meta[name='description']");
            return metaDesc?.GetAttribute("content");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts title from HTML.
    /// </summary>
    public async Task<string?> ExtractTitleAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        try
        {
            var config = Configuration.Default;
            using var context = BrowsingContext.New(config);
            using var document = await context.OpenAsync(req => req.Content(html));

            return document.Title;
        }
        catch
        {
            return null;
        }
    }

    private void RemoveExcludedElements(IDocument document)
    {
        // Remove default excluded tags
        foreach (var tag in DefaultExcludeTags)
        {
            foreach (var element in document.QuerySelectorAll(tag).ToArray())
            {
                element.Remove();
            }
        }

        // Remove custom excluded selectors
        foreach (var selector in _options.ExcludeSelectors)
        {
            foreach (var element in document.QuerySelectorAll(selector).ToArray())
            {
                element.Remove();
            }
        }
    }

    private static string? ExtractFromSelectors(IDocument document, List<string> selectors)
    {
        var sb = new StringBuilder();

        foreach (var selector in selectors)
        {
            var elements = document.QuerySelectorAll(selector);
            foreach (var element in elements)
            {
                if (!string.IsNullOrWhiteSpace(element.TextContent))
                {
                    sb.AppendLine(element.TextContent);
                }
            }
        }

        var result = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Replace multiple whitespace with single space
        var sb = new StringBuilder(text.Length);
        var lastWasWhitespace = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasWhitespace)
                {
                    sb.Append(' ');
                    lastWasWhitespace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasWhitespace = false;
            }
        }

        return sb.ToString().Trim();
    }
}
