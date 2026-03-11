using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Baseline.TabbedPages;

/// <summary>
/// Default implementation of ITabbedPageService using IContentRetriever.
/// Leverages implicit caching, language handling, and preview mode from IContentRetriever.
/// Falls back to IContentQueryExecutor for dynamic field-mapped queries (batch content).
/// </summary>
public class TabbedPageService(
    IContentRetriever contentRetriever,
    IContentQueryExecutor contentQueryExecutor,
    IWebsiteChannelContext websiteChannelContext,
    IOptions<BaselineTabbedPagesOptions> options,
    ILogger<TabbedPageService> logger) : ITabbedPageService
{
    private readonly BaselineTabbedPagesOptions _options = options.Value;
    private TabFieldMappingOptions Fields => _options.FieldMapping;

    /// <summary>
    /// Reads a string field from the container using the configured field name mapping.
    /// Falls back to ContentItemName when the mapping is null (field doesn't exist on content type).
    /// </summary>
    private static string ReadTitle(IContentQueryDataContainer container, string? fieldName) =>
        (fieldName is not null ? container.GetValue<string>(fieldName) : null)
        ?? container.ContentItemName;

    /// <summary>
    /// Reads a boolean field from the container. Returns false when the mapping is null.
    /// </summary>
    private static bool ReadBool(IContentQueryDataContainer container, string? fieldName) =>
        fieldName is not null && container.GetValue<bool>(fieldName);

    /// <summary>
    /// Reads an optional string field from the container. Returns null when the mapping is null.
    /// </summary>
    private static string? ReadOptionalString(IContentQueryDataContainer container, string? fieldName) =>
        fieldName is not null ? container.GetValue<string>(fieldName) : null;

    /// <inheritdoc/>
    public async Task<IEnumerable<TabItem>> GetTabsAsync(int pageId)
    {
        logger.LogDebug("Getting tabs for page {PageId}", pageId);

        // IContentRetriever handles channel, language, preview, and caching implicitly
        var tabs = await contentRetriever.RetrievePagesOfContentTypes<IWebPageFieldsSource>(
            [_options.TabContentTypeName],
            new RetrievePagesOfContentTypesParameters
            {
                IncludeSecuredItems = true
            },
            additionalQueryConfiguration: query => query
                .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemParentID), pageId))
                .OrderBy(nameof(WebPageFields.WebPageItemOrder)),
            cacheSettings: new RetrievalCacheSettings(
                cacheItemNameSuffix: $"tabs|children|{pageId}",
                cacheExpiration: TimeSpan.FromMinutes(_options.CacheDurationMinutes)));

        return tabs.Select((t, idx) =>
        {
            var title = t.SystemFields.ContentItemName;
            return new TabItem
            {
                Id = t.SystemFields.ContentItemID,
                PageId = pageId,
                Title = title,
                Slug = GenerateSlug(title),
                Order = t.SystemFields.WebPageItemOrder,
                IsDefault = idx == 0, // First tab is default when no IsDefault field exists
                Description = null,
                Icon = null,
                IsVisible = true,
                IsEnabled = true
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<TabItem?> GetTabAsync(int tabId)
    {
        logger.LogDebug("Getting tab {TabId}", tabId);

        var tabs = await contentRetriever.RetrievePagesOfContentTypes<IWebPageFieldsSource>(
            [_options.TabContentTypeName],
            new RetrievePagesOfContentTypesParameters
            {
                IncludeSecuredItems = true
            },
            additionalQueryConfiguration: query => query
                .Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemID), tabId)),
            cacheSettings: new RetrievalCacheSettings(
                cacheItemNameSuffix: $"tab|byid|{tabId}",
                cacheExpiration: TimeSpan.FromMinutes(_options.CacheDurationMinutes)));

        var t = tabs.FirstOrDefault();
        if (t is null) return null;

        var title = t.SystemFields.ContentItemName;
        return new TabItem
        {
            Id = t.SystemFields.ContentItemID,
            PageId = t.SystemFields.WebPageItemParentID,
            Title = title,
            Slug = GenerateSlug(title),
            Order = t.SystemFields.WebPageItemOrder,
            IsDefault = false,
            Description = null,
            Icon = null,
            IsVisible = true,
            IsEnabled = true
        };
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Slugs are computed from tab titles (not stored in DB), so querying by slug
    /// requires loading all tabs first. GetTabsAsync is cached, making this efficient.
    /// </remarks>
    public async Task<TabItem?> GetTabBySlugAsync(int pageId, string slug)
    {
        logger.LogDebug("Getting tab by slug {Slug} for page {PageId}", slug, pageId);
        var tabs = await GetTabsAsync(pageId);
        return tabs.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<TabItem?> GetDefaultTabAsync(int pageId)
    {
        logger.LogDebug("Getting default tab for page {PageId}", pageId);
        var tabs = await GetTabsAsync(pageId);
        return tabs.FirstOrDefault(t => t.IsDefault) ?? tabs.FirstOrDefault();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses IContentQueryExecutor for dynamic field access via configurable field mappings.
    /// Content fields (HTML, UsesPageBuilder) aren't on the typed model so typed retrieval
    /// can't map them. Falls back to container-based access.
    /// </remarks>
    public async Task<TabContent?> GetTabContentAsync(int tabId)
    {
        logger.LogDebug("Getting content for tab {TabId}", tabId);

        // If no content field is mapped, return empty content (no custom field on content type)
        if (Fields.ContentFieldName is null)
        {
            return new TabContent
            {
                TabId = tabId,
                Html = string.Empty,
                ContentType = _options.TabContentTypeName,
                UsesPageBuilder = false,
                LastModified = DateTime.UtcNow
            };
        }

        var builder = new ContentItemQueryBuilder()
            .ForContentType(_options.TabContentTypeName, query => query
                .Where(where => where.WhereEquals(nameof(ContentItemFields.ContentItemID), tabId)));

        var queryOptions = new ContentQueryExecutionOptions
        {
            ForPreview = websiteChannelContext.IsPreview,
            IncludeSecuredItems = true
        };

        var contents = await contentQueryExecutor.GetResult(builder, container =>
        {
            return new TabContent
            {
                TabId = tabId,
                Html = ReadOptionalString(container, Fields.ContentFieldName) ?? string.Empty,
                ContentType = _options.TabContentTypeName,
                UsesPageBuilder = ReadBool(container, Fields.UsesPageBuilderFieldName),
                LastModified = container.GetValue<DateTime>("ContentItemCommonDataModifiedWhen")
            };
        }, queryOptions);

        return contents.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<Dictionary<int, TabContent>> GetTabContentsAsync(IEnumerable<int> tabIds)
    {
        var ids = tabIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        logger.LogDebug("Getting content for {Count} tabs in batch", ids.Count);

        // If no content field is mapped, return empty content for all
        if (Fields.ContentFieldName is null)
        {
            return ids.ToDictionary(
                id => id,
                id => new TabContent
                {
                    TabId = id,
                    Html = string.Empty,
                    ContentType = _options.TabContentTypeName,
                    UsesPageBuilder = false,
                    LastModified = DateTime.UtcNow
                });
        }

        var builder = new ContentItemQueryBuilder()
            .ForContentType(_options.TabContentTypeName, query => query
                .Where(where => where.WhereIn(nameof(ContentItemFields.ContentItemID), ids)));

        var queryOptions = new ContentQueryExecutionOptions
        {
            ForPreview = websiteChannelContext.IsPreview,
            IncludeSecuredItems = true
        };

        var contents = await contentQueryExecutor.GetResult(builder, container =>
        {
            var id = container.ContentItemID;
            return new TabContent
            {
                TabId = id,
                Html = ReadOptionalString(container, Fields.ContentFieldName) ?? string.Empty,
                ContentType = _options.TabContentTypeName,
                UsesPageBuilder = ReadBool(container, Fields.UsesPageBuilderFieldName),
                LastModified = container.GetValue<DateTime>("ContentItemCommonDataModifiedWhen")
            };
        }, queryOptions);

        return contents.ToDictionary(c => c.TabId, c => c);
    }

    /// <summary>
    /// Generates a URL-friendly slug from a title.
    /// Handles accented characters, strips non-alphanumeric chars, and collapses hyphens.
    /// </summary>
    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        // Decompose accented characters and strip diacritics
        var normalized = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Replace non-alphanumeric with hyphens, collapse consecutive hyphens, trim
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
