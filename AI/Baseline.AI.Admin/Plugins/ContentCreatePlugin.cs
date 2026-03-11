using System.ComponentModel;
using System.Text;

using Baseline.AI;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Kentico.Xperience.Admin.Base.Authentication;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Admin.Plugins;

/// <summary>
/// AIRA plugin for creating new content items and web pages.
/// Mimics MCP's <c>create</c> capability — creates drafts that the user can review and publish.
/// Requires admin authentication for user context.
/// </summary>
[Description("Creates new content items and web pages as drafts that can be reviewed and published.")]
public sealed class ContentCreatePlugin(
    IServiceProvider serviceProvider,
    ILogger<ContentCreatePlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "ContentCreate";

    /// <summary>
    /// Creates a new reusable content item.
    /// </summary>
    [KernelFunction("create_content_item")]
    [Description("Creates a new reusable content item as a draft. " +
                 "Provide field values as semicolon-separated 'FieldName=Value' pairs. " +
                 "Use get_content_type_fields to discover required fields first.")]
    public async Task<string> CreateContentItemAsync(
        [Description("Content type code name (e.g. ChevalRoyal.BlogPostContent)")] string contentType,
        [Description("Display name for the new item")] string displayName,
        [Description("Language code (e.g. en)")] string languageName,
        [Description("Field values as semicolon-separated 'FieldName=Value' pairs")] string? fieldValues = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            int userId = await GetCurrentAdminUserIdAsync(scope);
            if (userId <= 0)
            {
                return "Error: Could not determine admin user. Ensure you are logged in.";
            }

            var classInfo = DataClassInfoProvider.GetDataClassInfo(contentType);
            if (classInfo is null)
            {
                return $"Content type '{contentType}' not found. Use list_content_types to see available types.";
            }

            var fields = ParseFieldValues(fieldValues);

            var managerFactory = scope.ServiceProvider.GetRequiredService<IContentItemManagerFactory>();
            var manager = managerFactory.Create(userId);

            string codeName = GenerateCodeName(displayName);
            var itemData = new ContentItemData(fields);

            var createParams = new CreateContentItemParameters(
                contentType, codeName, displayName, languageName, workspaceName: null);

            int contentItemId = await manager.Create(createParams, itemData);

            if (contentItemId <= 0)
            {
                return "Error: Failed to create content item.";
            }

            logger.LogInformation("ContentCreate: Created {Type} '{Name}' (ID: {Id})",
                contentType, displayName, contentItemId);

            return $"Content item created successfully!\n" +
                   $"- **Type**: {contentType}\n" +
                   $"- **Name**: {displayName}\n" +
                   $"- **Code Name**: {codeName}\n" +
                   $"- **ID**: {contentItemId}\n" +
                   $"- **Language**: {languageName}\n\n" +
                   $"⚠ Review and publish when ready.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentCreate: Failed to create {Type}", contentType);
            return $"Error creating content item: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a new web page.
    /// </summary>
    [KernelFunction("create_web_page")]
    [Description("Creates a new web page as a draft under a given parent path. " +
                 "Provide field values as semicolon-separated 'FieldName=Value' pairs. " +
                 "Use get_content_type_fields to discover required fields first.")]
    public async Task<string> CreateWebPageAsync(
        [Description("Content type code name (e.g. ChevalRoyal.BlogPost)")] string contentType,
        [Description("Website channel code name")] string channel,
        [Description("Display name for the new page")] string displayName,
        [Description("Language code (e.g. en)")] string languageName,
        [Description("Parent tree path (e.g. /blog). Use / for root.")] string parentPath,
        [Description("Field values as semicolon-separated 'FieldName=Value' pairs")] string? fieldValues = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            int userId = await GetCurrentAdminUserIdAsync(scope);
            if (userId <= 0)
            {
                return "Error: Could not determine admin user. Ensure you are logged in.";
            }

            var classInfo = DataClassInfoProvider.GetDataClassInfo(contentType);
            if (classInfo is null)
            {
                return $"Content type '{contentType}' not found.";
            }

            // Resolve channel ID
            var channelInfo = (await ChannelInfo.Provider.Get()
                .WhereEquals(nameof(ChannelInfo.ChannelName), channel)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (channelInfo is null)
            {
                return $"Channel '{channel}' not found. Use list_channels to see available channels.";
            }

            // Resolve website channel
            var websiteChannel = (await WebsiteChannelInfo.Provider.Get()
                .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), channelInfo.ChannelID)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (websiteChannel is null)
            {
                return $"'{channel}' is not a website channel.";
            }

            // Resolve parent page ID from tree path
            int? parentPageId = null;
            if (parentPath != "/")
            {
                var parentPage = (await WebPageItemInfo.Provider.Get()
                    .WhereEquals(nameof(WebPageItemInfo.WebPageItemTreePath), parentPath)
                    .WhereEquals(nameof(WebPageItemInfo.WebPageItemWebsiteChannelID), websiteChannel.WebsiteChannelID)
                    .TopN(1)
                    .GetEnumerableTypedResultAsync())
                    .FirstOrDefault();

                parentPageId = parentPage?.WebPageItemID;
            }

            var fields = ParseFieldValues(fieldValues);

            var webPageManagerFactory = scope.ServiceProvider.GetRequiredService<IWebPageManagerFactory>();
            var webPageManager = webPageManagerFactory.Create(websiteChannel.WebsiteChannelID, userId);

            var itemData = new ContentItemData(fields);
            var contentItemParams = new ContentItemParameters(contentType, itemData);

            var createParams = new CreateWebPageParameters(displayName, languageName, contentItemParams);

            if (parentPageId.HasValue)
            {
                createParams.ParentWebPageItemID = parentPageId.Value;
            }

            int webPageItemId = await webPageManager.Create(createParams);

            if (webPageItemId <= 0)
            {
                return "Error: Failed to create web page.";
            }

            logger.LogInformation("ContentCreate: Created web page {Type} '{Name}' (ID: {Id}) under {Parent}",
                contentType, displayName, webPageItemId, parentPath);

            return $"Web page created successfully!\n" +
                   $"- **Type**: {contentType}\n" +
                   $"- **Name**: {displayName}\n" +
                   $"- **Channel**: {channel}\n" +
                   $"- **Parent**: {parentPath}\n" +
                   $"- **Page ID**: {webPageItemId}\n" +
                   $"- **Language**: {languageName}\n\n" +
                   $"⚠ Review and publish when ready.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentCreate: Failed to create web page {Type}", contentType);
            return $"Error creating web page: {ex.Message}";
        }
    }

    private static async Task<int> GetCurrentAdminUserIdAsync(IServiceScope scope)
    {
        var userAccessor = scope.ServiceProvider.GetService<IAuthenticatedUserAccessor>();
        if (userAccessor is null)
        {
            return 0;
        }

        var user = await userAccessor.Get();
        return user?.UserID ?? 0;
    }

    private static Dictionary<string, object?> ParseFieldValues(string? fieldValues)
    {
        var result = new Dictionary<string, object?>();
        if (string.IsNullOrWhiteSpace(fieldValues))
        {
            return result;
        }

        foreach (var pair in fieldValues.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eqIdx = pair.IndexOf('=');
            if (eqIdx <= 0)
            {
                continue;
            }

            string key = pair[..eqIdx].Trim();
            string value = pair[(eqIdx + 1)..].Trim();

            if (!string.IsNullOrEmpty(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string GenerateCodeName(string displayName)
    {
        string code = displayName
            .ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("'", "")
            .Replace("\"", "");

        code = System.Text.RegularExpressions.Regex.Replace(code, @"[^a-z0-9\-]", "");
        code = System.Text.RegularExpressions.Regex.Replace(code, @"-{2,}", "-");
        code = code.Trim('-');

        return $"{code}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}
