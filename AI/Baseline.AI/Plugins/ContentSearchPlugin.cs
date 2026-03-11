using System.ComponentModel;
using System.Text;

using Baseline.AI;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Websites;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Plugins;

/// <summary>
/// AIRA plugin for searching content items and web pages.
/// Mimics MCP's <c>search</c> capability — queries across content types and channels.
/// </summary>
[Description("Searches content items and web pages across the CMS by keyword, type, and channel.")]
public sealed class ContentSearchPlugin(
    IServiceProvider serviceProvider,
    ILogger<ContentSearchPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "ContentSearch";

    /// <summary>
    /// Searches reusable content items by content type.
    /// </summary>
    [KernelFunction("search_content")]
    [Description("Searches reusable content items of a given content type. " +
                 "Returns content item GUID, name, and key fields. " +
                 "Use list_content_types first to find available types.")]
    public async Task<string> SearchContentAsync(
        [Description("Content type code name (e.g. ChevalRoyal.BlogPostContent)")] string contentType,
        [Description("Language code (default: en)")] string? language = null,
        [Description("Maximum results to return (default: 20)")] int? limit = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IContentQueryExecutor>();

            string lang = language ?? "en";
            int max = Math.Clamp(limit ?? 20, 1, 100);

            var builder = new ContentItemQueryBuilder()
                .ForContentTypes(p => p.OfContentType(contentType).WithContentTypeFields())
                .InLanguage(lang)
                .Parameters(p => p.TopN(max));

            var options = new ContentQueryExecutionOptions
            {
                ForPreview = true,
                IncludeSecuredItems = true
            };

            var items = (await executor.GetResult(builder, r => r, options)).ToList();

            if (items.Count == 0)
            {
                return $"No content items found for type '{contentType}' in language '{lang}'.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Content Items: {contentType} ({items.Count} results)");
            sb.AppendLine();

            foreach (var item in items)
            {
                string? guid = item.TryGetValue(nameof(ContentItemFields.ContentItemGUID), out object? g) ? g?.ToString() : null;
                string? name = item.TryGetValue(nameof(ContentItemFields.ContentItemName), out object? n) ? n?.ToString() : null;

                sb.AppendLine($"### {name ?? "(unnamed)"} `{guid}`");

                // Show first few custom fields (skip system fields)
                int fieldCount = 0;
                foreach (string col in GetCustomFieldNames(contentType))
                {
                    if (!item.TryGetValue(col, out object? val))
                    {
                        continue;
                    }

                    string display = val switch
                    {
                        null => "(null)",
                        string s when s.Length > 200 => s[..200] + "...",
                        _ => val.ToString() ?? "(null)"
                    };
                    sb.AppendLine($"- **{col}**: {display}");

                    if (++fieldCount >= 8) // Limit fields shown per item
                    {
                        sb.AppendLine("- _(more fields available — use read_content_item for full details)_");
                        break;
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentSearch: Failed to search {Type}", contentType);
            return $"Error searching content: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches web pages in a specific channel.
    /// </summary>
    [KernelFunction("search_web_pages")]
    [Description("Searches web pages by content type and channel. " +
                 "Optionally filter by tree path prefix. " +
                 "Returns page title, URL path, GUID, and key fields.")]
    public async Task<string> SearchWebPagesAsync(
        [Description("Content type code name (e.g. ChevalRoyal.BlogPost)")] string contentType,
        [Description("Website channel code name")] string channel,
        [Description("Language code (default: en)")] string? language = null,
        [Description("Tree path prefix filter (e.g. /blog). Omit for all pages.")] string? pathPrefix = null,
        [Description("Maximum results to return (default: 20)")] int? limit = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IContentQueryExecutor>();

            string lang = language ?? "en";
            int max = Math.Clamp(limit ?? 20, 1, 100);

            var builder = new ContentItemQueryBuilder()
                .ForContentTypes(p =>
                {
                    p.OfContentType(contentType).WithContentTypeFields().ForWebsite(channel);
                })
                .InLanguage(lang)
                .Parameters(p =>
                {
                    p.TopN(max);

                    if (!string.IsNullOrEmpty(pathPrefix))
                    {
                        p.Where(w => w.WhereStartsWith(
                            nameof(IWebPageContentQueryDataContainer.WebPageItemTreePath), pathPrefix));
                    }
                });

            var options = new ContentQueryExecutionOptions
            {
                ForPreview = true,
                IncludeSecuredItems = true
            };

            var items = (await executor.GetResult(builder, r => r, options)).ToList();

            if (items.Count == 0)
            {
                return $"No web pages found for type '{contentType}' in channel '{channel}' ({lang}).";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Web Pages: {contentType} in {channel} ({items.Count} results)");
            sb.AppendLine();
            sb.AppendLine("| Name | Tree Path | GUID |");
            sb.AppendLine("|------|-----------|------|");

            foreach (var item in items)
            {
                string? name = item.TryGetValue(nameof(ContentItemFields.ContentItemName), out object? n) ? n?.ToString() : "(unnamed)";
                string? path = item.TryGetValue(nameof(IWebPageContentQueryDataContainer.WebPageItemTreePath), out object? p) ? p?.ToString() : "-";
                string? guid = item.TryGetValue(nameof(ContentItemFields.ContentItemGUID), out object? g) ? g?.ToString() : "-";

                sb.AppendLine($"| {name} | {path} | {guid} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentSearch: Failed to search web pages in {Channel}", channel);
            return $"Error searching web pages: {ex.Message}";
        }
    }

    /// <summary>
    /// Lists website channels.
    /// </summary>
    [KernelFunction("list_channels")]
    [Description("Lists all website channels configured in the CMS. " +
                 "Use this to find channel names for search_web_pages.")]
    public async Task<string> ListChannelsAsync()
    {
        try
        {
            var channels = (await ChannelInfo.Provider.Get()
                .Columns(
                    nameof(ChannelInfo.ChannelID),
                    nameof(ChannelInfo.ChannelName),
                    nameof(ChannelInfo.ChannelDisplayName),
                    nameof(ChannelInfo.ChannelType))
                .OrderBy(nameof(ChannelInfo.ChannelDisplayName))
                .GetEnumerableTypedResultAsync())
                .ToList();

            if (channels.Count == 0)
            {
                return "No channels found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Channels ({channels.Count})");
            sb.AppendLine();
            sb.AppendLine("| Name | Display Name | Type |");
            sb.AppendLine("|------|-------------|------|");

            foreach (var ch in channels)
            {
                sb.AppendLine($"| {ch.ChannelName} | {ch.ChannelDisplayName} | {ch.ChannelType} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentSearch: Failed to list channels");
            return $"Error listing channels: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets custom (non-system) field names for a content type via FormInfo.
    /// </summary>
    private static IEnumerable<string> GetCustomFieldNames(string contentType)
    {
        var classInfo = DataClassInfoProvider.GetDataClassInfo(contentType);
        if (classInfo is null)
        {
            yield break;
        }

        var formInfo = new FormInfo(classInfo.ClassFormDefinition);

        foreach (var field in formInfo.GetFields<FormFieldInfo>())
        {
            if (field.Name.StartsWith("ContentItem", StringComparison.Ordinal) ||
                field.Name.StartsWith("System", StringComparison.Ordinal))
            {
                continue;
            }

            yield return field.Name;
        }
    }
}
