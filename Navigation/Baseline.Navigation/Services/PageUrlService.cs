using CMS.Websites;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Baseline.Navigation;

/// <summary>
/// Implementation of page URL service using IWebPageUrlRetriever for URL resolution.
/// </summary>
public class PageUrlService(
    IWebPageUrlRetriever webPageUrlRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PageUrlService> logger) : IPageUrlService
{
    public async Task<string?> GetUrlAsync(int webPageItemId)
    {
        try
        {
            var languageName = preferredLanguageRetriever.Get();
            var url = await webPageUrlRetriever.Retrieve(webPageItemId, languageName);
            return url?.RelativePath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PageUrlService: failed to retrieve URL for web page item {Id}", webPageItemId);
            return null;
        }
    }

    public async Task<string?> GetUrlAsync(Guid contentItemGuid)
    {
        try
        {
            var languageName = preferredLanguageRetriever.Get();
            var url = await webPageUrlRetriever.Retrieve(contentItemGuid, languageName);
            return url?.RelativePath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PageUrlService: failed to retrieve URL for GUID {Guid}", contentItemGuid);
            return null;
        }
    }

    public async Task<string?> GetAbsoluteUrlAsync(int webPageItemId)
    {
        var relativePath = await GetUrlAsync(webPageItemId);
        if (relativePath is null)
        {
            return null;
        }

        return GetAbsoluteUrl(relativePath);
    }

    public Task<string?> GetCanonicalUrlAsync()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return Task.FromResult<string?>(null);
        }

        var canonicalUrl = $"{request.Scheme}://{request.Host}{request.Path}";
        return Task.FromResult<string?>(canonicalUrl);
    }

    private string GetAbsoluteUrl(string relativePath)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return relativePath;
        }

        return $"{request.Scheme}://{request.Host}{relativePath}";
    }
}
