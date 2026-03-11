using System.ComponentModel;
using System.Text;

using Baseline.AI;

using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Websites;

using Kentico.Xperience.Admin.Base.Authentication;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Admin.Plugins;

/// <summary>
/// AIRA plugin for editing content items and web pages.
/// Mimics MCP's <c>edit</c> capability — creates drafts with updated field values.
/// Requires admin authentication for user context.
/// </summary>
[Description("Edits content items and web pages — creates drafts with updated field values.")]
public sealed class ContentEditPlugin(
    IServiceProvider serviceProvider,
    ILogger<ContentEditPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "ContentEdit";

    /// <summary>
    /// Updates fields on a web page (creates/updates a draft).
    /// </summary>
    [KernelFunction("update_web_page")]
    [Description("Updates one or more fields on a web page by creating/updating a draft. " +
                 "The user must review and publish the draft manually. " +
                 "Provide field names and values as semicolon-separated pairs: 'FieldName=Value;Field2=Value2'.")]
    public async Task<string> UpdateWebPageAsync(
        [Description("Web page item ID (integer)")] int webPageItemId,
        [Description("Language code (e.g. en)")] string languageName,
        [Description("Website channel ID (integer)")] int websiteChannelId,
        [Description("Field updates as semicolon-separated 'FieldName=Value' pairs")] string fieldUpdates)
    {
        if (string.IsNullOrWhiteSpace(fieldUpdates))
        {
            return "Error: fieldUpdates is required. Format: 'FieldName=Value;Field2=Value2'";
        }

        try
        {
            using var scope = serviceProvider.CreateScope();

            int userId = await GetCurrentAdminUserIdAsync(scope);
            if (userId <= 0)
            {
                return "Error: Could not determine admin user. Ensure you are logged in.";
            }

            var fields = ParseFieldUpdates(fieldUpdates);
            if (fields.Count == 0)
            {
                return "Error: No valid field updates parsed. Format: 'FieldName=Value;Field2=Value2'";
            }

            var webPageManagerFactory = scope.ServiceProvider.GetRequiredService<IWebPageManagerFactory>();
            var manager = webPageManagerFactory.Create(websiteChannelId, userId);

            // Ensure a draft exists
            await manager.TryCreateDraft(webPageItemId, languageName);

            // Apply field updates
            var contentItemData = new ContentItemData(fields);
            var updateData = new UpdateDraftData(contentItemData);
            bool updated = await manager.TryUpdateDraft(webPageItemId, languageName, updateData);

            if (!updated)
            {
                return "Error: Failed to update draft. Verify the field names are valid for this content type.";
            }

            var changes = fields.Select(f => $"- **{f.Key}** → {Truncate(f.Value?.ToString(), 100)}");

            logger.LogInformation("ContentEdit: Updated web page {Id} ({Lang}): {Fields}",
                webPageItemId, languageName, string.Join(", ", fields.Keys));

            return $"Draft updated successfully ({fields.Count} field(s)):\n{string.Join("\n", changes)}\n\n⚠ Review and publish when ready.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentEdit: Failed to update web page {Id}", webPageItemId);
            return $"Error updating web page: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates fields on a reusable content item.
    /// </summary>
    [KernelFunction("update_content_item")]
    [Description("Updates one or more fields on a reusable content item by creating/updating a draft. " +
                 "Provide field names and values as semicolon-separated pairs: 'FieldName=Value;Field2=Value2'.")]
    public async Task<string> UpdateContentItemAsync(
        [Description("Content item GUID")] string contentItemGuid,
        [Description("Language code (e.g. en)")] string languageName,
        [Description("Field updates as semicolon-separated 'FieldName=Value' pairs")] string fieldUpdates)
    {
        if (!Guid.TryParse(contentItemGuid, out var guid))
        {
            return "Error: Invalid GUID format.";
        }

        if (string.IsNullOrWhiteSpace(fieldUpdates))
        {
            return "Error: fieldUpdates is required.";
        }

        try
        {
            using var scope = serviceProvider.CreateScope();

            int userId = await GetCurrentAdminUserIdAsync(scope);
            if (userId <= 0)
            {
                return "Error: Could not determine admin user. Ensure you are logged in.";
            }

            var fields = ParseFieldUpdates(fieldUpdates);
            if (fields.Count == 0)
            {
                return "Error: No valid field updates parsed.";
            }

            // Resolve content item ID from GUID
            var itemInfo = await scope.ServiceProvider
                .GetRequiredService<IInfoProvider<ContentItemInfo>>()
                .GetAsync(guid);

            if (itemInfo is null)
            {
                return $"Content item {contentItemGuid} not found.";
            }

            var managerFactory = scope.ServiceProvider.GetRequiredService<IContentItemManagerFactory>();
            var manager = managerFactory.Create(userId);

            // Ensure a draft exists, then update
            await manager.TryCreateDraft(itemInfo.ContentItemID, languageName);

            var contentItemData = new ContentItemData(fields);
            bool updated = await manager.TryUpdateDraft(itemInfo.ContentItemID, languageName, contentItemData);

            if (!updated)
            {
                return "Error: Failed to update content item draft.";
            }

            logger.LogInformation("ContentEdit: Updated content item {Guid} ({Lang}): {Fields}",
                contentItemGuid, languageName, string.Join(", ", fields.Keys));

            return $"Draft updated ({fields.Count} field(s)). ⚠ Review and publish when ready.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentEdit: Failed to update content item {Guid}", contentItemGuid);
            return $"Error updating content item: {ex.Message}";
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

    private static Dictionary<string, object?> ParseFieldUpdates(string fieldUpdates)
    {
        var result = new Dictionary<string, object?>();

        foreach (var pair in fieldUpdates.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "(empty)";
        }

        return text.Length > maxLength ? text[..maxLength] + "..." : text;
    }
}
