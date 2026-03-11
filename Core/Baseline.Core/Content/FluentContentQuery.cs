using System.Collections.Concurrent;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;

namespace Baseline.Core.Content;

/// <summary>
/// Factory for creating fluent content query builders via DI.
/// Replaces the static Content class to avoid thread-safety issues.
/// 
/// Usage:
/// var posts = await contentQuery.Of&lt;BlogPost&gt;()
///     .Where("IsPublished", true)
///     .OrderByDescending("PublishDate")
///     .Take(10)
///     .ExecuteAsync();
/// </summary>
public interface IContentQuery
{
    /// <summary>
    /// Start a fluent query for the specified content type.
    /// </summary>
    FluentContentQueryBuilder<T> Of<T>() where T : class, new();

    /// <summary>
    /// Start a fluent query for web pages of the specified type.
    /// </summary>
    FluentWebPageQueryBuilder<T> Pages<T>() where T : class, IWebPageFieldsSource, new();
}

/// <summary>
/// Scoped implementation of <see cref="IContentQuery"/>.
/// Resolves IContentQueryExecutor and IWebsiteChannelContext per-request via DI.
/// </summary>
internal sealed class ContentQuery(
    IContentQueryExecutor queryExecutor,
    IWebsiteChannelContext channelContext) : IContentQuery
{
    public FluentContentQueryBuilder<T> Of<T>() where T : class, new() =>
        new(queryExecutor, channelContext);

    public FluentWebPageQueryBuilder<T> Pages<T>() where T : class, IWebPageFieldsSource, new() =>
        new(queryExecutor, channelContext);
}

/// <summary>
/// Fluent builder for reusable content item queries.
/// </summary>
public class FluentContentQueryBuilder<T> where T : class, new()
{
    // Cache reflection lookups for CONTENT_TYPE_NAME per type
    private static readonly ConcurrentDictionary<Type, string> ContentTypeNameCache = new();

    private protected readonly IContentQueryExecutor QueryExecutor;
    private protected readonly IWebsiteChannelContext ChannelContext;
    private readonly List<Action<WhereParameters>> _whereActions = [];
    private string? _orderByField;
    private bool _orderDescending;
    private int? _topN;
    private int _offset;
    private bool _forPreview;
    private bool _includeSecuredItems;
    private string? _language;
    private int _linkedItemsDepth;

    internal FluentContentQueryBuilder(IContentQueryExecutor queryExecutor, IWebsiteChannelContext channelContext)
    {
        QueryExecutor = queryExecutor;
        ChannelContext = channelContext;
        _forPreview = channelContext.IsPreview;
    }

    /// <summary>
    /// Add a where condition using the field name.
    /// </summary>
    public FluentContentQueryBuilder<T> Where(string fieldName, object value)
    {
        _whereActions.Add(w => w.WhereEquals(fieldName, value));
        return this;
    }

    /// <summary>
    /// Add multiple where conditions.
    /// </summary>
    public FluentContentQueryBuilder<T> Where(Action<WhereParameters> configure)
    {
        _whereActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Filter by published status (requires IsPublished field).
    /// </summary>
    public FluentContentQueryBuilder<T> Published()
    {
        _whereActions.Add(w => w.WhereEquals("IsPublished", true));
        return this;
    }

    /// <summary>
    /// Order by field ascending.
    /// </summary>
    public FluentContentQueryBuilder<T> OrderBy(string fieldName)
    {
        _orderByField = fieldName;
        _orderDescending = false;
        return this;
    }

    /// <summary>
    /// Order by field descending.
    /// </summary>
    public FluentContentQueryBuilder<T> OrderByDescending(string fieldName)
    {
        _orderByField = fieldName;
        _orderDescending = true;
        return this;
    }

    /// <summary>
    /// Limit results to N items.
    /// </summary>
    public FluentContentQueryBuilder<T> Take(int count)
    {
        _topN = count;
        return this;
    }

    /// <summary>
    /// Skip N items (for pagination).
    /// </summary>
    public FluentContentQueryBuilder<T> Skip(int count)
    {
        _offset = count;
        return this;
    }

    /// <summary>
    /// Include linked content items.
    /// </summary>
    public FluentContentQueryBuilder<T> IncludeLinkedItems(int depth = 1)
    {
        _linkedItemsDepth = depth;
        return this;
    }

    /// <summary>
    /// Force preview mode.
    /// </summary>
    public FluentContentQueryBuilder<T> ForPreview(bool preview = true)
    {
        _forPreview = preview;
        return this;
    }

    /// <summary>
    /// Include secured items in results (default: false).
    /// </summary>
    public FluentContentQueryBuilder<T> IncludeSecuredItems(bool include = true)
    {
        _includeSecuredItems = include;
        return this;
    }

    /// <summary>
    /// Set specific language.
    /// </summary>
    public FluentContentQueryBuilder<T> InLanguage(string language)
    {
        _language = language;
        return this;
    }

    /// <summary>
    /// Execute the query and return results.
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var builder = BuildQuery();
        var options = BuildOptions();

        return await QueryExecutor.GetMappedResult<T>(builder, options, cancellationToken);
    }

    /// <summary>
    /// Execute the query and return the first result or null.
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var results = await Take(1).ExecuteAsync(cancellationToken);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Execute the query and return results with total count.
    /// Executes a separate count query for accurate pagination.
    /// </summary>
    public async Task<PagedResult<T>> ExecutePagedAsync(CancellationToken cancellationToken = default)
    {
        var builder = BuildQuery();
        var options = BuildOptions();

        // Execute the paged query
        var results = await QueryExecutor.GetMappedResult<T>(builder, options, cancellationToken);
        var resultList = results.ToList();

        // Execute a separate count query (no TopN/Offset) for accurate total
        var totalCount = await GetTotalCountAsync(options, cancellationToken);

        var pageSize = _topN ?? resultList.Count;
        var currentPage = pageSize > 0 ? (_offset / pageSize) + 1 : 1;

        return new PagedResult<T>(resultList, totalCount, pageSize, currentPage);
    }

    private async Task<int> GetTotalCountAsync(
        ContentQueryExecutionOptions options,
        CancellationToken cancellationToken)
    {
        var contentTypeName = ResolveContentTypeName();
        var countBuilder = new ContentItemQueryBuilder();

        countBuilder.ForContentType(contentTypeName, query =>
        {
            foreach (var whereAction in _whereActions)
            {
                query.Where(whereAction);
            }
        });

        if (!string.IsNullOrEmpty(_language))
        {
            countBuilder.InLanguage(_language);
        }

        var countResults = await QueryExecutor.GetMappedResult<T>(countBuilder, options, cancellationToken);
        return countResults.Count();
    }

    private protected virtual ContentItemQueryBuilder BuildQuery()
    {
        var contentTypeName = ResolveContentTypeName();
        var builder = new ContentItemQueryBuilder();

        builder.ForContentType(contentTypeName, query =>
        {
            foreach (var whereAction in _whereActions)
            {
                query.Where(whereAction);
            }

            if (!string.IsNullOrEmpty(_orderByField))
            {
                var direction = _orderDescending ? OrderDirection.Descending : OrderDirection.Ascending;
                query.OrderBy(new OrderByColumn(_orderByField, direction));
            }

            if (_offset > 0 && _topN.HasValue)
            {
                query.Offset(_offset, _topN.Value);
            }
            else if (_topN.HasValue)
            {
                query.TopN(_topN.Value);
            }

            if (_linkedItemsDepth > 0)
            {
                query.WithLinkedItems(_linkedItemsDepth);
            }
        });

        if (!string.IsNullOrEmpty(_language))
        {
            builder.InLanguage(_language);
        }

        return builder;
    }

    private protected ContentQueryExecutionOptions BuildOptions()
    {
        return new ContentQueryExecutionOptions
        {
            ForPreview = _forPreview,
            IncludeSecuredItems = _includeSecuredItems
        };
    }

    private protected static string ResolveContentTypeName()
    {
        return ContentTypeNameCache.GetOrAdd(typeof(T), static type =>
        {
            var field = type.GetField("CONTENT_TYPE_NAME");
            if (field is not null && field.GetValue(null) is string name)
                return name;

            var typeName = type.Name;
            var ns = type.Namespace ?? "";
            var parts = ns.Split('.');

            return parts.Length >= 2 ? $"{parts[0]}.{typeName}" : typeName;
        });
    }
}

/// <summary>
/// Fluent builder for web page queries with path matching and ForWebsite support.
/// </summary>
public class FluentWebPageQueryBuilder<T> : FluentContentQueryBuilder<T>
    where T : class, IWebPageFieldsSource, new()
{
    private PathMatch? _pathMatchType;

    internal FluentWebPageQueryBuilder(IContentQueryExecutor queryExecutor, IWebsiteChannelContext channelContext)
        : base(queryExecutor, channelContext)
    {
    }

    /// <summary>
    /// Filter by path prefix (e.g., "/blog") — returns children only.
    /// </summary>
    public FluentWebPageQueryBuilder<T> UnderPath(string path)
    {
        _pathMatchType = PathMatch.Children(path);
        return this;
    }

    /// <summary>
    /// Filter to include a specific section including the parent.
    /// </summary>
    public FluentWebPageQueryBuilder<T> InSection(string path)
    {
        _pathMatchType = PathMatch.Section(path);
        return this;
    }

    /// <summary>
    /// Overrides base BuildQuery to apply ForWebsite and path matching.
    /// </summary>
    private protected override ContentItemQueryBuilder BuildQuery()
    {
        var contentTypeName = ResolveContentTypeName();
        var builder = new ContentItemQueryBuilder();

        builder.ForContentType(contentTypeName, query =>
        {
            // Apply ForWebsite with optional path match
            if (_pathMatchType is not null)
            {
                query.ForWebsite(ChannelContext.WebsiteChannelName, _pathMatchType);
            }
            else
            {
                query.ForWebsite(ChannelContext.WebsiteChannelName);
            }
        });

        return builder;
    }
}

/// <summary>
/// Result container with pagination metadata.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageSize,
    int CurrentPage)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}
