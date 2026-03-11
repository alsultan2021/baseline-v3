using System.ComponentModel;
using System.Text;
using System.Text.Json;

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
/// AIRA plugin for reading content items and content type metadata.
/// Mimics MCP's <c>read</c> capability — reads fields from content items,
/// lists content types, and inspects content type field definitions.
/// </summary>
[Description("Reads content items, web pages, and content type definitions from the CMS.")]
public sealed class ContentReadPlugin(
    IServiceProvider serviceProvider,
    ILogger<ContentReadPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "ContentRead";

    /// <summary>
    /// Reads all fields of a content item by its GUID.
    /// </summary>
    [KernelFunction("read_content_item")]
    [Description("Reads all field values of a content item (reusable or web page) by its GUID. " +
                 "Returns field names and their values. Requires the content type code name.")]
    public async Task<string> ReadContentItemAsync(
        [Description("Content item GUID")] string contentItemGuid,
        [Description("Content type code name (e.g. ChevalRoyal.BlogPost)")] string contentType,
        [Description("Language code (default: en)")] string? language = null)
    {
        if (!Guid.TryParse(contentItemGuid, out var guid))
        {
            return "Error: Invalid GUID format.";
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IContentQueryExecutor>();

            string lang = language ?? "en";

            var builder = new ContentItemQueryBuilder()
                .ForContentTypes(p => p.OfContentType(contentType).WithContentTypeFields())
                .InLanguage(lang)
                .Parameters(p => p
                    .Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemGUID), guid)));

            var options = new ContentQueryExecutionOptions
            {
                ForPreview = true,
                IncludeSecuredItems = true
            };

            var items = await executor.GetResult(builder, r => r, options);
            var item = items.FirstOrDefault();

            if (item is null)
            {
                return $"Content item {contentItemGuid} not found for type '{contentType}' in language '{lang}'.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Content Item ({contentType})");
            sb.AppendLine();

            foreach (string col in GetFieldNames(contentType))
            {
                if (!item.TryGetValue(col, out object? val))
                {
                    continue;
                }

                string display = val switch
                {
                    null => "(null)",
                    string s when s.Length > 500 => s[..500] + "...",
                    _ => val.ToString() ?? "(null)"
                };
                sb.AppendLine($"- **{col}**: {display}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentRead: Failed to read {Guid}", contentItemGuid);
            return $"Error reading content item: {ex.Message}";
        }
    }

    /// <summary>
    /// Lists all content types in the system.
    /// </summary>
    [KernelFunction("list_content_types")]
    [Description("Lists all content types registered in the CMS. " +
                 "Returns code name, display name, and type (Page, Reusable, Email, Headless). " +
                 "Use this to discover what content types are available before reading or searching.")]
    public async Task<string> ListContentTypesAsync(
        [Description("Filter: 'page', 'reusable', 'email', or 'all' (default: all)")] string? filter = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            var query = DataClassInfoProvider.GetClasses()
                .WhereNotEmpty(nameof(DataClassInfo.ClassContentTypeType));

            // Filter by content type type
            if (!string.IsNullOrEmpty(filter) && filter != "all")
            {
                string typeValue = filter.ToLowerInvariant() switch
                {
                    "page" or "website" => "Website",
                    "reusable" => "Reusable",
                    "email" => "Email",
                    "headless" => "Headless",
                    _ => filter
                };

                query = query.WhereEquals(nameof(DataClassInfo.ClassContentTypeType), typeValue);
            }

            var types = (await query
                .Columns(
                    nameof(DataClassInfo.ClassName),
                    nameof(DataClassInfo.ClassDisplayName),
                    nameof(DataClassInfo.ClassContentTypeType))
                .OrderBy(nameof(DataClassInfo.ClassDisplayName))
                .GetEnumerableTypedResultAsync())
                .ToList();

            if (types.Count == 0)
            {
                return "No content types found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Content Types ({types.Count})");
            sb.AppendLine();
            sb.AppendLine("| Code Name | Display Name | Type |");
            sb.AppendLine("|-----------|-------------|------|");

            foreach (var t in types)
            {
                sb.AppendLine($"| {t.ClassName} | {t.ClassDisplayName} | {t.ClassContentTypeType} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentRead: Failed to list content types");
            return $"Error listing content types: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the field definitions for a content type.
    /// </summary>
    [KernelFunction("get_content_type_fields")]
    [Description("Gets all field definitions for a content type — field names, data types, " +
                 "whether required, and default values. " +
                 "Use this to understand a content type's schema before reading or creating items.")]
    public async Task<string> GetContentTypeFieldsAsync(
        [Description("Content type code name (e.g. ChevalRoyal.BlogPost)")] string contentType)
    {
        try
        {
            var classInfo = DataClassInfoProvider.GetDataClassInfo(contentType);
            if (classInfo is null)
            {
                return $"Content type '{contentType}' not found.";
            }

            var formInfo = new CMS.FormEngine.FormInfo(classInfo.ClassFormDefinition);
            var fields = formInfo.GetFields<CMS.FormEngine.FormFieldInfo>();

            var sb = new StringBuilder();
            sb.AppendLine($"## {classInfo.ClassDisplayName} ({contentType})");
            sb.AppendLine($"Type: {classInfo.ClassContentTypeType}");
            sb.AppendLine();
            sb.AppendLine("| Field | Data Type | Required | Default |");
            sb.AppendLine("|-------|-----------|----------|---------|");

            foreach (var field in fields)
            {
                if (field.Name.StartsWith("ContentItem", StringComparison.Ordinal))
                {
                    continue; // Skip system fields
                }

                string required = field.AllowEmpty ? "No" : "Yes";
                string def = !string.IsNullOrEmpty(field.DefaultValue) ? field.DefaultValue : "-";
                sb.AppendLine($"| {field.Name} | {field.DataType} | {required} | {def} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentRead: Failed to get fields for {Type}", contentType);
            return $"Error reading content type: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads a web page's content by its tree path.
    /// </summary>
    [KernelFunction("read_web_page")]
    [Description("Reads a web page's content fields by its tree path and channel. " +
                 "Returns all field values for the page.")]
    public async Task<string> ReadWebPageAsync(
        [Description("Content type code name (e.g. ChevalRoyal.BlogPost)")] string contentType,
        [Description("Website channel code name")] string channel,
        [Description("Web page tree path (e.g. /blog/my-post)")] string treePath,
        [Description("Language code (default: en)")] string? language = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IContentQueryExecutor>();

            string lang = language ?? "en";

            var builder = new ContentItemQueryBuilder()
                .ForContentTypes(p => p
                    .OfContentType(contentType)
                    .WithContentTypeFields()
                    .ForWebsite(channel))
                .Parameters(p => p
                    .Where(w => w.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemTreePath), treePath)));

            builder.InLanguage(lang);

            var options = new ContentQueryExecutionOptions
            {
                ForPreview = true,
                IncludeSecuredItems = true
            };

            var items = await executor.GetResult(builder, r => r, options);
            var item = items.FirstOrDefault();

            if (item is null)
            {
                return $"Web page not found at '{treePath}' in channel '{channel}' ({lang}).";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Web Page: {treePath}");
            sb.AppendLine($"Type: {contentType} | Channel: {channel} | Language: {lang}");
            sb.AppendLine();

            foreach (string col in GetFieldNames(contentType))
            {
                if (!item.TryGetValue(col, out object? val))
                {
                    continue;
                }

                string display = val switch
                {
                    null => "(null)",
                    string s when s.Length > 500 => s[..500] + "...",
                    _ => val.ToString() ?? "(null)"
                };
                sb.AppendLine($"- **{col}**: {display}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentRead: Failed to read web page at {Path}", treePath);
            return $"Error reading web page: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets non-system field names for a content type via FormInfo.
    /// </summary>
    private static IEnumerable<string> GetFieldNames(string contentType)
    {
        var classInfo = DataClassInfoProvider.GetDataClassInfo(contentType);
        if (classInfo is null)
        {
            yield break;
        }

        var formInfo = new FormInfo(classInfo.ClassFormDefinition);

        foreach (var field in formInfo.GetFields<FormFieldInfo>())
        {
            yield return field.Name;
        }
    }
}
