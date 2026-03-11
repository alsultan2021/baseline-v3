using CMS.ContentEngine;
using CMS.Websites;
using Kentico.Content.Web.Mvc;

namespace Baseline.Core;

/// <summary>
/// Default implementation of <see cref="IBaselineContentRetriever"/>.
/// Wraps XbK's IContentRetriever with Baseline-specific enhancements.
/// </summary>
internal sealed class BaselineContentRetriever(
    IContentRetriever contentRetriever,
    IStructuredDataService structuredDataService) : IBaselineContentRetriever
{
    public async Task<IEnumerable<PageWithMetadata<TPage>>> RetrievePagesWithMetadataAsync<TPage>(
        BaselineRetrievePagesParameters parameters,
        bool includeStructuredData = false) where TPage : class, IWebPageFieldsSource
    {
        // Build retrieval parameters for XbK
        var retrieveParams = new RetrievePagesParameters();

        if (!string.IsNullOrEmpty(parameters.PathMatch))
        {
            retrieveParams = new RetrievePagesParameters
            {
                PathMatch = parameters.IncludeChildren
                    ? PathMatch.Children(parameters.PathMatch)
                    : PathMatch.Single(parameters.PathMatch),
                UseLanguageFallbacks = parameters.UseLanguageFallbacks ?? true
            };
        }
        else if (parameters.UseLanguageFallbacks.HasValue)
        {
            retrieveParams = new RetrievePagesParameters
            {
                UseLanguageFallbacks = parameters.UseLanguageFallbacks.Value
            };
        }

        // Configure caching — disable if preview mode
        var isPreview = parameters.ForPreview == true;
        var cacheSettings = isPreview
            ? RetrievalCacheSettings.CacheDisabled
            : parameters.CacheSettings?.Enabled == true
                ? new RetrievalCacheSettings(
                    cacheItemNameSuffix: parameters.CacheSettings.CacheKeySuffix ?? $"Pages_{typeof(TPage).Name}",
                    cacheExpiration: parameters.CacheSettings.CacheExpiration ?? TimeSpan.FromMinutes(10),
                    useSlidingExpiration: parameters.CacheSettings.UseSlidingExpiration)
                : RetrievalCacheSettings.CacheDisabled;

        // Build additional query configuration from parameters
        Action<RetrievePagesQueryParameters>? additionalQuery = null;
        if (parameters.TopN.HasValue || !string.IsNullOrEmpty(parameters.OrderBy) || parameters.UrlLanguageBehavior.HasValue)
        {
            additionalQuery = query =>
            {
                if (parameters.TopN.HasValue)
                {
                    query.TopN(parameters.TopN.Value);
                }

                if (!string.IsNullOrEmpty(parameters.OrderBy))
                {
                    query.OrderBy(parameters.OrderBy);
                }

                if (parameters.UrlLanguageBehavior == BaselineUrlLanguageBehavior.UseFallbackLanguage)
                {
                    query.SetUrlLanguageBehavior(CMS.Websites.UrlLanguageBehavior.UseFallbackLanguage);
                }
                else if (parameters.UrlLanguageBehavior == BaselineUrlLanguageBehavior.UseRequestedLanguage)
                {
                    query.SetUrlLanguageBehavior(CMS.Websites.UrlLanguageBehavior.UseRequestedLanguage);
                }
            };
        }

        // Use XbK's built-in caching via IContentRetriever
        var pages = await contentRetriever.RetrievePages<TPage>(
            retrieveParams,
            additionalQueryConfiguration: additionalQuery,
            cacheSettings: cacheSettings);

        var results = new List<PageWithMetadata<TPage>>();

        foreach (var page in pages)
        {
            string? jsonLd = null;

            if (includeStructuredData)
            {
                jsonLd = await structuredDataService.GenerateArticleJsonLdAsync(page);
            }

            results.Add(new PageWithMetadata<TPage>
            {
                Page = page,
                StructuredDataJsonLd = jsonLd
            });
        }

        return results;
    }

    public async Task<PageWithMetadata<TPage>?> RetrieveCurrentPageWithMetadataAsync<TPage>(
        bool includeStructuredData = false) where TPage : class, IWebPageFieldsSource
    {
        // Current page uses default caching (always enabled)
        var page = await contentRetriever.RetrieveCurrentPage<TPage>();

        if (page is null)
        {
            return null;
        }

        string? jsonLd = null;

        if (includeStructuredData)
        {
            jsonLd = await structuredDataService.GenerateArticleJsonLdAsync(page);
        }

        return new PageWithMetadata<TPage>
        {
            Page = page,
            StructuredDataJsonLd = jsonLd
        };
    }

    public async Task<IEnumerable<TContent>> RetrieveContentItemsAsync<TContent>(
        BaselineRetrieveContentParameters parameters) where TContent : class, IContentItemFieldsSource
    {
        var retrieveParams = new RetrieveContentParameters();

        // Configure caching — disable if preview
        var cacheSettings = parameters.CacheSettings?.Enabled == true
            ? new RetrievalCacheSettings(
                cacheItemNameSuffix: parameters.CacheSettings.CacheKeySuffix ?? $"Content_{typeof(TContent).Name}",
                cacheExpiration: parameters.CacheSettings.CacheExpiration ?? TimeSpan.FromMinutes(10),
                useSlidingExpiration: parameters.CacheSettings.UseSlidingExpiration)
            : RetrievalCacheSettings.CacheDisabled;

        // Build additional query configuration from parameters
        Action<RetrieveContentQueryParameters>? additionalQuery = null;
        if (parameters.TopN.HasValue || !string.IsNullOrEmpty(parameters.OrderBy))
        {
            additionalQuery = query =>
            {
                if (parameters.TopN.HasValue)
                {
                    query.TopN(parameters.TopN.Value);
                }

                if (!string.IsNullOrEmpty(parameters.OrderBy))
                {
                    query.OrderBy(parameters.OrderBy);
                }
            };
        }

        return await contentRetriever.RetrieveContent<TContent>(
            retrieveParams,
            additionalQueryConfiguration: additionalQuery,
            cacheSettings: cacheSettings);
    }
}
