using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Baseline.Search.Lucene;

/// <summary>
/// Service for crawling web pages for indexing.
/// </summary>
public sealed class LuceneWebCrawlerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LuceneWebCrawlerService> _logger;
    private readonly LuceneSearchOptions _options;

    public LuceneWebCrawlerService(
        HttpClient httpClient,
        ILogger<LuceneWebCrawlerService> logger,
        LuceneSearchOptions options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;

        // Configure HttpClient
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "BaselineSearchCrawler/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        if (!string.IsNullOrWhiteSpace(_options.WebCrawlerBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.WebCrawlerBaseUrl);
        }
    }

    /// <summary>
    /// Crawls a page by URL path.
    /// </summary>
    /// <param name="urlPath">The URL path to crawl (can be relative or absolute).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTML content of the page.</returns>
    public async Task<string> CrawlPageAsync(
        string urlPath,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableWebCrawling)
        {
            return string.Empty;
        }

        try
        {
            var normalizedPath = NormalizeUrl(urlPath);

            _logger.LogDebug("Crawling page: {Url}", normalizedPath);

            var response = await _httpClient.GetAsync(normalizedPath, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to crawl page {Url}. Status: {StatusCode}",
                    normalizedPath,
                    response.StatusCode);
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug(
                "Successfully crawled page {Url}. Content length: {Length}",
                normalizedPath,
                content.Length);

            return content;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to {Url} timed out", urlPath);
            return string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error crawling page {Url}", urlPath);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling page {Url}", urlPath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Crawls multiple pages in parallel.
    /// </summary>
    /// <param name="urlPaths">The URL paths to crawl.</param>
    /// <param name="maxConcurrency">Maximum concurrent requests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of URL to HTML content.</returns>
    public async Task<Dictionary<string, string>> CrawlPagesAsync(
        IEnumerable<string> urlPaths,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string>();
        var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = urlPaths.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var content = await CrawlPageAsync(url, cancellationToken);
                return (url, content);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var crawlResults = await Task.WhenAll(tasks);

        foreach (var (url, content) in crawlResults)
        {
            results[url] = content;
        }

        return results;
    }

    private static string NormalizeUrl(string urlPath)
    {
        // Remove leading tilde and slashes for relative paths
        return urlPath.TrimStart('~').TrimStart('/');
    }
}
