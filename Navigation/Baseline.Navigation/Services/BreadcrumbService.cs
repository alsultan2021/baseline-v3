using System.Globalization;
using Baseline.Core;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Websites;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Navigation;

/// <summary>
/// Implementation of breadcrumb service using Xperience by Kentico content tree.
/// Uses IWebPageDataContextRetriever for page context and IWebPageUrlRetriever
/// for the actual page URL (safe in Page Builder edit mode).
/// </summary>
public class BreadcrumbService(
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IContentRetriever contentRetriever,
    IInfoProvider<ContentItemLanguageMetadataInfo> metadataProvider,
    IWebPageUrlRetriever webPageUrlRetriever,
    IHttpContextAccessor httpContextAccessor,
    IOptions<BaselineNavigationOptions> options,
    ILogger<BreadcrumbService> logger) : IBreadcrumbService
{
    private readonly BreadcrumbOptions _options = options.Value.Breadcrumbs;

    public async Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbsAsync()
    {
        if (!webPageDataContextRetriever.TryRetrieve(out var context))
        {
            return [];
        }

        // Use IWebPageUrlRetriever to get the actual page URL — Request.Path
        // is unreliable in Page Builder edit mode where it contains the
        // internal widget AJAX path instead of the real page URL.
        string currentPath;
        try
        {
            var urlResult = await webPageUrlRetriever.Retrieve(
                context.WebPage.WebPageItemID,
                context.WebPage.LanguageName);
            currentPath = urlResult.RelativePath ?? "/";
            // IWebPageUrlRetriever may return a virtual path like "~/contact" —
            // strip the leading ~ so path segments are parsed correctly.
            if (currentPath.StartsWith("~/", StringComparison.Ordinal))
                currentPath = currentPath[1..];
        }
        catch
        {
            currentPath = httpContextAccessor.HttpContext?.Request.Path.Value ?? "/";
        }

        // Use the page context to get the actual CMS page title for the current page
        string? currentPageTitle = null;
        try
        {
            var currentPage = await contentRetriever.RetrieveCurrentPage<IWebPageFieldsSource>();
            if (currentPage is not null)
            {
                var metadata = await metadataProvider.Get()
                    .WhereEquals(
                        nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID),
                        currentPage.SystemFields.ContentItemID)
                    .WhereEquals(
                        nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID),
                        currentPage.SystemFields.ContentItemCommonDataContentLanguageID)
                    .TopN(1)
                    .GetEnumerableTypedResultAsync();

                currentPageTitle = metadata.FirstOrDefault()?.ContentItemLanguageMetadataDisplayName;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "BreadcrumbService: could not retrieve current page title, falling back to URL segment");
        }

        return BuildBreadcrumbs(currentPath, currentPageTitle);
    }

    public Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbsForPathAsync(string path)
    {
        return Task.FromResult<IEnumerable<BreadcrumbItem>>(BuildBreadcrumbs(path, currentPageTitle: null));
    }

    public async Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbsForContentAsync(int contentItemId)
    {
        // Fall back to context-based breadcrumbs
        return await GetBreadcrumbsAsync();
    }

    private IEnumerable<BreadcrumbItem> BuildBreadcrumbs(string path, string? currentPageTitle)
    {
        // Normalize path
        path = path.TrimStart('/');

        var breadcrumbs = new List<BreadcrumbItem>();

        // Add home if configured
        if (_options.IncludeHome)
        {
            breadcrumbs.Add(new BreadcrumbItem(_options.HomeLabel, "/", 1));
        }

        // Parse path segments and build breadcrumbs
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Strip language prefix (e.g. "fr") if the first segment matches the current culture's two-letter code
        var currentLangCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (segments.Length > 0
            && segments[0].Length == 2
            && segments[0].Equals(currentLangCode, StringComparison.OrdinalIgnoreCase))
        {
            segments = segments[1..];
        }
        var currentPath = string.Empty;
        var position = breadcrumbs.Count + 1;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            currentPath += "/" + segment;

            var isLast = i == segments.Length - 1;

            // Use actual CMS page title for the current (last) segment if available
            var name = isLast && !string.IsNullOrWhiteSpace(currentPageTitle)
                ? currentPageTitle
                : FormatSegmentAsTitle(segment);

            var url = isLast && !_options.CurrentPageIsLink ? string.Empty : currentPath;

            breadcrumbs.Add(new BreadcrumbItem(name, url, position++));
        }

        // Limit to max items
        if (breadcrumbs.Count > _options.MaxItems)
        {
            breadcrumbs = breadcrumbs.TakeLast(_options.MaxItems).ToList();
        }

        return breadcrumbs;
    }

    private static string FormatSegmentAsTitle(string segment)
    {
        // Convert URL segment to readable title
        // e.g., "about-us" -> "About Us"
        return string.Join(' ',
            segment.Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}
