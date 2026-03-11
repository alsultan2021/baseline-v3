using CMS.Websites.Routing;
using CSharpFunctionalExtensions;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using CMS.Websites;
using Microsoft.Extensions.Logging;

namespace Baseline.Core;

/// <summary>
/// V3 implementation of page context repository.
/// Uses RoutedWebPage from IWebPageDataContextRetriever which provides basic page info.
/// </summary>
public class PageContextRepository(
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IWebsiteChannelContext websiteChannelContext,
    IUrlResolver urlResolver,
    ILogger<PageContextRepository> logger) : IPageContextRepository
{
    public async Task<Maybe<PageIdentity>> GetCurrentPageAsync()
    {
        if (!webPageDataContextRetriever.TryRetrieve(out var data))
        {
            return Maybe<PageIdentity>.None;
        }

        return await BuildPageIdentityAsync(data.WebPage);
    }

    public async Task<Maybe<PageIdentity>> GetPageAsync(TreeIdentity identity)
    {
        // For now, only support PageID lookup via current context
        if (!identity.PageID.HasValue)
        {
            return Maybe<PageIdentity>.None;
        }

        // Check if current page matches
        if (webPageDataContextRetriever.TryRetrieve(out var data)
            && data.WebPage.WebPageItemID == identity.PageID.Value)
        {
            return await BuildPageIdentityAsync(data.WebPage);
        }

        return Maybe<PageIdentity>.None;
    }

    public async Task<Maybe<PageIdentity>> GetPageAsync(TreeCultureIdentity identity)
    {
        if (!identity.PageID.HasValue)
        {
            return Maybe<PageIdentity>.None;
        }

        // Check if current page matches
        if (webPageDataContextRetriever.TryRetrieve(out var data)
            && data.WebPage.WebPageItemID == identity.PageID.Value)
        {
            return await BuildPageIdentityAsync(data.WebPage);
        }

        return Maybe<PageIdentity>.None;
    }

    private async Task<Maybe<PageIdentity>> BuildPageIdentityAsync(RoutedWebPage webPage)
    {
        try
        {
            var language = webPage.LanguageName ?? preferredLanguageRetriever.Get();
            var urlResult = await webPageUrlRetriever.Retrieve(webPage.WebPageItemID, language);
            var relativeUrl = urlResult.RelativePath ?? string.Empty;

            // RoutedWebPage only provides: WebPageItemID, WebPageItemGUID, LanguageName, ContentTypeName
            var pageIdentity = new PageIdentity
            {
                Name = webPage.ContentTypeName, // Best available - actual name not in RoutedWebPage
                PageID = webPage.WebPageItemID,
                PageGuid = webPage.WebPageItemGUID,
                Culture = language,
                RelativeUrl = relativeUrl,
                AbsoluteUrl = urlResolver.GetAbsoluteUrl(relativeUrl),
                ChannelID = websiteChannelContext.WebsiteChannelID,
                PageType = webPage.ContentTypeName
            };

            return Maybe<PageIdentity>.From(pageIdentity);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PageContextRepository: failed to build page identity for WebPageItemID {Id}", webPage.WebPageItemID);
            return Maybe<PageIdentity>.None;
        }
    }
}
