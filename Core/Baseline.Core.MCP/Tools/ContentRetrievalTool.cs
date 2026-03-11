using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// Read-only content retrieval tools for MCP.
/// Note: Content type creation/modification handled by official Kentico MCP (xperience-management-api).
/// </summary>
[McpServerToolType]
public static class ContentRetrievalTool
{
    /// <summary>
    /// Retrieves webpage URLs for a website channel with pagination support.
    /// Uses low-level ObjectQuery to avoid model type registration requirements.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetAllWebpageUrlsByChannel),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get web page URLs by channel"),
    Description("Retrieves webpage URLs for a website channel with optional pagination and culture filter")]
    public static async Task<string> GetAllWebpageUrlsByChannel(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<WebsiteChannelInfo> channelProvider,
        IInfoProvider<WebPageUrlPathInfo> urlPathProvider,
        [Description("The name of the website channel")] string channelName,
        [Description("Culture code filter (optional, e.g., 'en-US')")] string? culture = null,
        [Description("Page size (default: 100, max: 500)")] int pageSize = 100,
        [Description("Page number (1-based, default: 1)")] int page = 1,
        CancellationToken cancellationToken = default)
    {
        // Validate and cap page size
        pageSize = Math.Clamp(pageSize, 1, 500);
        page = Math.Max(1, page);

        // Resolve website channel: try exact ChannelName, then partial match on ChannelName or ChannelDisplayName
        var webChannel = await ResolveWebsiteChannel(channelProvider, channelName, cancellationToken)
            ?? throw new ArgumentException($"No channel found with the name '{channelName}'.", nameof(channelName));

        // Build query using ObjectQuery to avoid model type mapping issues
        var query = urlPathProvider.Get()
            .Source(s => s.Join<WebPageItemInfo>(
                nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID),
                nameof(WebPageItemInfo.WebPageItemID)))
            .WhereEquals(
                $"CMS_WebPageItem.{nameof(WebPageItemInfo.WebPageItemWebsiteChannelID)}",
                webChannel.WebsiteChannelID)
            .WhereEquals(nameof(WebPageUrlPathInfo.WebPageUrlPathIsDraft), false);

        if (!string.IsNullOrWhiteSpace(culture))
        {
            query = query
                .Source(s => s.Join<ContentLanguageInfo>(
                    nameof(WebPageUrlPathInfo.WebPageUrlPathContentLanguageID),
                    nameof(ContentLanguageInfo.ContentLanguageID)))
                .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), culture);
        }

        var allUrlPaths = (await query
            .Columns(
                nameof(WebPageUrlPathInfo.WebPageUrlPath),
                nameof(WebPageItemInfo.WebPageItemName),
                nameof(WebPageItemInfo.WebPageItemGUID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .ToList();

        int totalCount = allUrlPaths.Count;
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        string host = $"{(options.Value.UseHttps ? "https" : "http")}://{webChannel.WebsiteChannelDomain}";

        var urls = allUrlPaths
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                Url = $"{host}{p.WebPageUrlPath}",
                Name = p.GetValue(nameof(WebPageItemInfo.WebPageItemName))?.ToString() ?? "",
                WebPageItemGuid = p.GetValue(nameof(WebPageItemInfo.WebPageItemGUID)) is Guid guid
                    ? guid
                    : Guid.Empty
            });

        var result = new
        {
            Channel = channelName,
            Culture = culture ?? "all",
            Pagination = new
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            },
            Items = urls
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Resolves a <see cref="WebsiteChannelInfo"/> by exact code name, partial code name, or display name.
    /// </summary>
    private static async Task<WebsiteChannelInfo?> ResolveWebsiteChannel(
        IInfoProvider<WebsiteChannelInfo> channelProvider,
        string channelName,
        CancellationToken cancellationToken)
    {
        // 1. Exact match on ChannelName
        var channels = await channelProvider.Get()
            .Source(s => s.Join<ChannelInfo>(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), nameof(ChannelInfo.ChannelID)))
            .WhereEquals(nameof(ChannelInfo.ChannelName), channelName)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        if (channels.FirstOrDefault() is WebsiteChannelInfo exact)
        {
            return exact;
        }

        // 2. Partial match on ChannelName (case-insensitive via SQL)
        channels = await channelProvider.Get()
            .Source(s => s.Join<ChannelInfo>(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), nameof(ChannelInfo.ChannelID)))
            .WhereContains(nameof(ChannelInfo.ChannelName), channelName)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        if (channels.FirstOrDefault() is WebsiteChannelInfo partial)
        {
            return partial;
        }

        // 3. Partial match on ChannelDisplayName (case-insensitive via SQL)
        channels = await channelProvider.Get()
            .Source(s => s.Join<ChannelInfo>(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), nameof(ChannelInfo.ChannelID)))
            .WhereContains(nameof(ChannelInfo.ChannelDisplayName), channelName)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        if (channels.FirstOrDefault() is WebsiteChannelInfo display)
        {
            return display;
        }

        // 4. If only one website channel exists, return it (common single-channel setup)
        channels = await channelProvider.Get()
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var allChannels = channels.ToList();
        return allChannels.Count == 1 ? allChannels[0] : null;
    }

    /// <summary>
    /// Gets all content types (read-only listing).
    /// </summary>
    [McpServerTool(
        Name = nameof(GetContentTypes),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get all content types"),
    Description("Gets a list of all content types in the application")]
    public static async Task<string> GetContentTypes(
        IOptions<BaselineMCPConfiguration> options)
    {
        var dataClasses = await DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassType), "Content")
            .Columns(nameof(DataClassInfo.ClassDisplayName), nameof(DataClassInfo.ClassName), nameof(DataClassInfo.ClassContentTypeType))
            .GetEnumerableTypedResultAsync();

        var result = dataClasses.Select(dc => new
        {
            dc.ClassName,
            dc.ClassDisplayName,
            dc.ClassContentTypeType
        });

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets reusable field schemas (read-only).
    /// </summary>
    [McpServerTool(
        Name = nameof(GetReusableFieldSchemas),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get all reusable field schemas"),
    Description("Gets a list of all reusable field schemas in the application")]
    public static async Task<string> GetReusableFieldSchemas(
        IOptions<BaselineMCPConfiguration> options)
    {
        var schemas = await DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassType), "ReusableFieldSchema")
            .Columns(nameof(DataClassInfo.ClassName), nameof(DataClassInfo.ClassDisplayName), nameof(DataClassInfo.ClassGUID))
            .GetEnumerableTypedResultAsync();

        var result = schemas.Select(s => new
        {
            s.ClassName,
            s.ClassDisplayName,
            s.ClassGUID
        });

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets content type details (read-only).
    /// </summary>
    [McpServerTool(
        Name = nameof(GetContentTypeDetails),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get content type details"),
    Description("Gets the content type information for the given content type")]
    public static string GetContentTypeDetails(
        IOptions<BaselineMCPConfiguration> options,
        [Description("The content type name")] string contentType)
    {
        var dc = DataClassInfoProvider.GetDataClassInfo(contentType)
            ?? throw new ArgumentException($"No content type found: '{contentType}'.", nameof(contentType));

        var formInfo = new FormInfo(dc.ClassFormDefinition);
        var fields = formInfo.GetFields(true, true);

        var result = new
        {
            dc.ClassName,
            dc.ClassDisplayName,
            dc.ClassContentTypeType,
            dc.ClassGUID,
            Fields = fields.Select(f => new
            {
                f.Name,
                f.Caption,
                f.DataType,
                f.AllowEmpty,
                f.DefaultValue
            })
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets all valid content type icon names.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetAllContentTypeIcons),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get all content type icons"),
    Description("Gets all valid icons for content types")]
    public static string GetAllContentTypeIcons(IOptions<BaselineMCPConfiguration> options)
    {
        var allIconNames = typeof(Kentico.Xperience.Admin.Base.Icons)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(value => value != null)
            .ToList();

        return JsonSerializer.Serialize(allIconNames, options.Value.SerializerOptions);
    }
}
