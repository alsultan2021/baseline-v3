using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

using Baseline.AI;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Plugins;

/// <summary>
/// AIRA plugin providing URL-based web page fetching — raw HTML and extracted text.
/// Mimics MCP's <c>fetch</c> capability.
/// </summary>
[Description("Fetches web pages by URL — returns raw HTML or extracted text content.")]
public sealed partial class WebFetchPlugin(
    IHttpClientFactory httpClientFactory,
    ILogger<WebFetchPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "WebFetch";

    private const int MaxHtmlLength = 80_000;
    private const int MaxTextLength = 40_000;

    [GeneratedRegex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StyleTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiNewlineRegex();

    /// <summary>
    /// Fetches raw HTML from a URL.
    /// </summary>
    [KernelFunction("fetch_webpage")]
    [Description("Fetches the raw HTML of a web page by URL. " +
                 "Returns full HTML including meta tags, scripts, and structure. " +
                 "Use fetch_webpage_text for a cleaner text-only version.")]
    public async Task<string> FetchWebpageAsync(
        [Description("Full URL to fetch (e.g. https://example.com/page)")] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Error: URL is required.";
        }

        try
        {
            using var client = httpClientFactory.CreateClient("AiraWebFetch");
            using var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }

            string html = await response.Content.ReadAsStringAsync();

            if (html.Length > MaxHtmlLength)
            {
                html = html[..MaxHtmlLength] + "\n<!-- [truncated] -->";
            }

            return html;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WebFetch: Failed to fetch {Url}", url);
            return $"Error fetching page: {ex.Message}";
        }
    }

    /// <summary>
    /// Fetches a URL and extracts visible text content (strips HTML).
    /// </summary>
    [KernelFunction("fetch_webpage_text")]
    [Description("Fetches a web page and returns only the visible text content — " +
                 "strips HTML tags, scripts, and styles. " +
                 "Best for reading/analyzing page content without HTML noise.")]
    public async Task<string> FetchWebpageTextAsync(
        [Description("Full URL to fetch (e.g. https://example.com/page)")] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Error: URL is required.";
        }

        try
        {
            using var client = httpClientFactory.CreateClient("AiraWebFetch");
            using var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }

            string html = await response.Content.ReadAsStringAsync();
            string text = ExtractText(html);

            if (text.Length > MaxTextLength)
            {
                text = text[..MaxTextLength] + "\n[truncated]";
            }

            return text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WebFetch: Failed to fetch text from {Url}", url);
            return $"Error fetching page: {ex.Message}";
        }
    }

    private static string ExtractText(string html)
    {
        // Remove script and style blocks
        string cleaned = ScriptTagRegex().Replace(html, " ");
        cleaned = StyleTagRegex().Replace(cleaned, " ");

        // Strip tags
        cleaned = HtmlTagRegex().Replace(cleaned, " ");

        // Decode HTML entities
        cleaned = System.Net.WebUtility.HtmlDecode(cleaned);

        // Collapse whitespace
        cleaned = WhitespaceRegex().Replace(cleaned, " ");
        cleaned = MultiNewlineRegex().Replace(cleaned, "\n\n");

        return cleaned.Trim();
    }
}
