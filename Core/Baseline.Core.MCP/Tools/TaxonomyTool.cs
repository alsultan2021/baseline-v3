using System.ComponentModel;
using System.Text.Json;

using CMS.ContentEngine;
using CMS.DataEngine;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// Taxonomy tools for viewing tag structures and assigning tags to content items.
/// Note: Taxonomy creation/modification handled by official Kentico MCP.
/// </summary>
[McpServerToolType]
public static class TaxonomyTool
{
    /// <summary>
    /// Retrieves all taxonomies and their tag structures as structured JSON.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetTaxonomies),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Taxonomies"),
    Description("Retrieves all taxonomies and their hierarchical tag structures as structured JSON")]
    public static async Task<string> GetTaxonomies(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<TaxonomyInfo> taxonomyProvider,
        IInfoProvider<TagInfo> tagProvider,
        [Description("Maximum number of taxonomies (default: 10)")] int limit = 10,
        [Description("Include tags in response (default: true)")] bool includeTags = true,
        [Description("Maximum tags per taxonomy (default: 50)")] int maxTagsPerTaxonomy = 50)
    {
        limit = Math.Clamp(limit, 1, 100);
        maxTagsPerTaxonomy = Math.Clamp(maxTagsPerTaxonomy, 1, 500);

        var taxonomies = await taxonomyProvider.Get()
            .TopN(limit)
            .Columns(
                nameof(TaxonomyInfo.TaxonomyID),
                nameof(TaxonomyInfo.TaxonomyGUID),
                nameof(TaxonomyInfo.TaxonomyName),
                nameof(TaxonomyInfo.TaxonomyTitle),
                nameof(TaxonomyInfo.TaxonomyDescription))
            .GetEnumerableTypedResultAsync();

        var taxonomyResults = new List<object>();

        foreach (var taxonomy in taxonomies)
        {
            object? tagsData = null;

            if (includeTags)
            {
                var tags = await tagProvider.Get()
                    .WhereEquals(nameof(TagInfo.TagTaxonomyID), taxonomy.TaxonomyID)
                    .TopN(maxTagsPerTaxonomy)
                    .Columns(
                        nameof(TagInfo.TagID),
                        nameof(TagInfo.TagGUID),
                        nameof(TagInfo.TagName),
                        nameof(TagInfo.TagTitle),
                        nameof(TagInfo.TagParentID),
                        nameof(TagInfo.TagOrder))
                    .OrderBy(nameof(TagInfo.TagOrder))
                    .GetEnumerableTypedResultAsync();

                tagsData = tags.Select(t => new
                {
                    Id = t.TagID,
                    Guid = t.TagGUID,
                    Name = t.TagName,
                    Title = t.TagTitle,
                    ParentId = t.TagParentID > 0 ? t.TagParentID : (int?)null,
                    Order = t.TagOrder
                });
            }

            taxonomyResults.Add(new
            {
                Id = taxonomy.TaxonomyID,
                Guid = taxonomy.TaxonomyGUID,
                Name = taxonomy.TaxonomyName,
                Title = taxonomy.TaxonomyTitle,
                Description = taxonomy.TaxonomyDescription,
                Tags = tagsData
            });
        }

        var result = new
        {
            TotalCount = taxonomyResults.Count,
            Taxonomies = taxonomyResults
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets tags for a specific taxonomy as structured JSON.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetTaxonomyTags),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Taxonomy Tags"),
    Description("Gets all tags within a specific taxonomy as structured JSON with hierarchy information")]
    public static async Task<string> GetTaxonomyTags(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<TaxonomyInfo> taxonomyProvider,
        IInfoProvider<TagInfo> tagProvider,
        [Description("Taxonomy code name")] string taxonomyName,
        [Description("Maximum tags to return (default: 100)")] int limit = 100,
        [Description("Build hierarchical tree structure (default: false)")] bool hierarchical = false)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var taxonomy = (await taxonomyProvider.Get()
            .WhereEquals(nameof(TaxonomyInfo.TaxonomyName), taxonomyName)
            .Columns(
                nameof(TaxonomyInfo.TaxonomyID),
                nameof(TaxonomyInfo.TaxonomyGUID),
                nameof(TaxonomyInfo.TaxonomyName),
                nameof(TaxonomyInfo.TaxonomyTitle))
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (taxonomy is null)
        {
            return JsonSerializer.Serialize(new
            {
                Error = $"Taxonomy '{taxonomyName}' not found.",
                AvailableTaxonomies = (await taxonomyProvider.Get()
                    .Columns(nameof(TaxonomyInfo.TaxonomyName))
                    .GetEnumerableTypedResultAsync())
                    .Select(t => t.TaxonomyName)
            }, options.Value.SerializerOptions);
        }

        var tags = await tagProvider.Get()
            .WhereEquals(nameof(TagInfo.TagTaxonomyID), taxonomy.TaxonomyID)
            .TopN(limit)
            .Columns(
                nameof(TagInfo.TagID),
                nameof(TagInfo.TagGUID),
                nameof(TagInfo.TagName),
                nameof(TagInfo.TagTitle),
                nameof(TagInfo.TagParentID),
                nameof(TagInfo.TagOrder))
            .OrderBy(nameof(TagInfo.TagOrder))
            .GetEnumerableTypedResultAsync();

        var tagsList = tags.ToList();

        object tagsResult;

        if (hierarchical)
        {
            // Build tree structure
            tagsResult = BuildTagHierarchy(tagsList, 0);
        }
        else
        {
            // Flat list with parent references
            tagsResult = tagsList.Select(t => new
            {
                Id = t.TagID,
                Guid = t.TagGUID,
                Name = t.TagName,
                Title = t.TagTitle,
                ParentId = t.TagParentID > 0 ? t.TagParentID : (int?)null,
                Order = t.TagOrder,
                Level = CalculateTagLevel(tagsList, t)
            });
        }

        var result = new
        {
            Taxonomy = new
            {
                Id = taxonomy.TaxonomyID,
                Guid = taxonomy.TaxonomyGUID,
                Name = taxonomy.TaxonomyName,
                Title = taxonomy.TaxonomyTitle
            },
            TotalTags = tagsList.Count,
            Hierarchical = hierarchical,
            Tags = tagsResult
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    private static List<object> BuildTagHierarchy(List<TagInfo> allTags, int parentId)
    {
        return allTags
            .Where(t => t.TagParentID == parentId)
            .OrderBy(t => t.TagOrder)
            .Select(t => new
            {
                Id = t.TagID,
                Guid = t.TagGUID,
                Name = t.TagName,
                Title = t.TagTitle,
                Order = t.TagOrder,
                Children = BuildTagHierarchy(allTags, t.TagID)
            })
            .Cast<object>()
            .ToList();
    }

    private static int CalculateTagLevel(List<TagInfo> allTags, TagInfo tag)
    {
        int level = 0;
        int? parentId = tag.TagParentID > 0 ? tag.TagParentID : null;

        while (parentId.HasValue)
        {
            level++;
            var parent = allTags.FirstOrDefault(t => t.TagID == parentId.Value);
            parentId = parent?.TagParentID > 0 ? parent.TagParentID : null;
        }

        return level;
    }

    /// <summary>
    /// Sets taxonomy tags on a content item's taxonomy field.
    /// </summary>
    [McpServerTool(
        Name = nameof(SetTaxonomyTags),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Set Taxonomy Tags"),
    Description("Updates taxonomy tags on a content item. Requires a draft to exist or creates one.")]
    public static async Task<string> SetTaxonomyTags(
        IOptions<BaselineMCPConfiguration> options,
        IContentItemManagerFactory contentItemManagerFactory,
        IInfoProvider<TagInfo> tagProvider,
        IContentQueryExecutor contentQueryExecutor,
        [Description("Content item ID")] int contentItemId,
        [Description("Language code (e.g., 'en')")] string languageCode,
        [Description("Field name that holds taxonomy tags (e.g., 'BlogPostPageBlogType')")] string taxonomyFieldName,
        [Description("Comma-separated tag GUIDs to assign")] string tagGuids,
        [Description("User ID for audit trail (default: 0 = system)")] int userId = 0)
    {
        if (contentItemId <= 0)
        {
            return JsonSerializer.Serialize(new { Error = "Content item ID is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return JsonSerializer.Serialize(new { Error = "Language code is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(taxonomyFieldName))
        {
            return JsonSerializer.Serialize(new { Error = "Taxonomy field name is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Parse tag GUIDs
            var tagReferences = new List<TagReference>();
            if (!string.IsNullOrWhiteSpace(tagGuids))
            {
                var guidStrings = tagGuids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var guidStr in guidStrings)
                {
                    if (Guid.TryParse(guidStr, out var tagGuid))
                    {
                        tagReferences.Add(new TagReference { Identifier = tagGuid });
                    }
                    else
                    {
                        return JsonSerializer.Serialize(new
                        {
                            Error = $"Invalid tag GUID: '{guidStr}'",
                            Hint = "Use GetTaxonomyTags to find valid tag GUIDs"
                        }, options.Value.SerializerOptions);
                    }
                }
            }

            // Validate tags exist
            if (tagReferences.Count > 0)
            {
                var tagGuidsToValidate = tagReferences.Select(t => t.Identifier).ToList();
                var existingTags = await tagProvider.Get()
                    .WhereIn(nameof(TagInfo.TagGUID), tagGuidsToValidate)
                    .Columns(nameof(TagInfo.TagGUID), nameof(TagInfo.TagName))
                    .GetEnumerableTypedResultAsync();

                var existingGuids = existingTags.Select(t => t.TagGUID).ToHashSet();
                var missingGuids = tagGuidsToValidate.Where(g => !existingGuids.Contains(g)).ToList();

                if (missingGuids.Count > 0)
                {
                    return JsonSerializer.Serialize(new
                    {
                        Error = "Some tag GUIDs not found",
                        MissingGuids = missingGuids,
                        Hint = "Use GetTaxonomyTags to find valid tag GUIDs"
                    }, options.Value.SerializerOptions);
                }
            }

            // Create content item manager
            var manager = contentItemManagerFactory.Create(userId);

            // Try to create a draft first (in case item is published/unpublished)
            await manager.TryCreateDraft(contentItemId, languageCode);

            // Prepare update data with taxonomy field
            var updateData = new ContentItemData(new Dictionary<string, object>
            {
                { taxonomyFieldName, tagReferences }
            });

            // Update the draft
            var updated = await manager.TryUpdateDraft(contentItemId, languageCode, updateData);

            if (!updated)
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = "Failed to update content item draft",
                    Hint = "Ensure the content item exists and has a draft version"
                }, options.Value.SerializerOptions);
            }

            var result = new
            {
                Success = true,
                ContentItemId = contentItemId,
                LanguageCode = languageCode,
                FieldName = taxonomyFieldName,
                TagsAssigned = tagReferences.Count,
                TagGuids = tagReferences.Select(t => t.Identifier),
                Message = $"Successfully assigned {tagReferences.Count} tag(s) to '{taxonomyFieldName}'"
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Failed to set taxonomy tags",
                Message = ex.Message,
                ContentItemId = contentItemId
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Creates a new taxonomy.
    /// </summary>
    [McpServerTool(
        Name = nameof(CreateTaxonomy),
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Create Taxonomy"),
    Description("Creates a new taxonomy for organizing tags")]
    public static async Task<string> CreateTaxonomy(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<TaxonomyInfo> taxonomyProvider,
        [Description("Taxonomy code name (e.g., 'ArticleCategory')")] string taxonomyName,
        [Description("Display title for the taxonomy")] string taxonomyTitle,
        [Description("Description of the taxonomy (optional)")] string? description = null)
    {
        if (string.IsNullOrWhiteSpace(taxonomyName))
        {
            return JsonSerializer.Serialize(new { Error = "Taxonomy name is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(taxonomyTitle))
        {
            return JsonSerializer.Serialize(new { Error = "Taxonomy title is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Check if taxonomy already exists
            var existing = (await taxonomyProvider.Get()
                .WhereEquals(nameof(TaxonomyInfo.TaxonomyName), taxonomyName)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (existing is not null)
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = $"Taxonomy '{taxonomyName}' already exists",
                    ExistingId = existing.TaxonomyID,
                    ExistingGuid = existing.TaxonomyGUID
                }, options.Value.SerializerOptions);
            }

            // Create new taxonomy
            var taxonomy = new TaxonomyInfo
            {
                TaxonomyName = taxonomyName,
                TaxonomyTitle = taxonomyTitle,
                TaxonomyDescription = description ?? string.Empty
            };

            await taxonomyProvider.SetAsync(taxonomy);

            var result = new
            {
                Success = true,
                TaxonomyId = taxonomy.TaxonomyID,
                TaxonomyGuid = taxonomy.TaxonomyGUID,
                TaxonomyName = taxonomy.TaxonomyName,
                TaxonomyTitle = taxonomy.TaxonomyTitle,
                Message = $"Taxonomy '{taxonomyName}' created successfully"
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Failed to create taxonomy",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Creates a new tag within a taxonomy.
    /// </summary>
    [McpServerTool(
        Name = nameof(CreateTag),
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Create Tag"),
    Description("Creates a new tag within a taxonomy")]
    public static async Task<string> CreateTag(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<TaxonomyInfo> taxonomyProvider,
        IInfoProvider<TagInfo> tagProvider,
        [Description("Taxonomy code name where the tag will be created")] string taxonomyName,
        [Description("Tag code name (e.g., 'Animals')")] string tagName,
        [Description("Display title for the tag")] string tagTitle,
        [Description("Description of the tag (optional)")] string? description = null,
        [Description("Parent tag GUID for hierarchical tags (optional)")] string? parentTagGuid = null,
        [Description("Sort order (default: 0)")] int order = 0)
    {
        if (string.IsNullOrWhiteSpace(taxonomyName))
        {
            return JsonSerializer.Serialize(new { Error = "Taxonomy name is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return JsonSerializer.Serialize(new { Error = "Tag name is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(tagTitle))
        {
            return JsonSerializer.Serialize(new { Error = "Tag title is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Find taxonomy
            var taxonomy = (await taxonomyProvider.Get()
                .WhereEquals(nameof(TaxonomyInfo.TaxonomyName), taxonomyName)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (taxonomy is null)
            {
                return JsonSerializer.Serialize(new
                {
                    Error = $"Taxonomy '{taxonomyName}' not found",
                    Hint = "Use CreateTaxonomy to create the taxonomy first, or GetTaxonomies to list existing ones"
                }, options.Value.SerializerOptions);
            }

            // Check if tag already exists in this taxonomy
            var existingTag = (await tagProvider.Get()
                .WhereEquals(nameof(TagInfo.TagTaxonomyID), taxonomy.TaxonomyID)
                .WhereEquals(nameof(TagInfo.TagName), tagName)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (existingTag is not null)
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Error = $"Tag '{tagName}' already exists in taxonomy '{taxonomyName}'",
                    ExistingId = existingTag.TagID,
                    ExistingGuid = existingTag.TagGUID
                }, options.Value.SerializerOptions);
            }

            // Resolve parent tag if provided
            int parentId = 0;
            if (!string.IsNullOrWhiteSpace(parentTagGuid) && Guid.TryParse(parentTagGuid, out var parentGuid))
            {
                var parentTag = (await tagProvider.Get()
                    .WhereEquals(nameof(TagInfo.TagGUID), parentGuid)
                    .WhereEquals(nameof(TagInfo.TagTaxonomyID), taxonomy.TaxonomyID)
                    .TopN(1)
                    .GetEnumerableTypedResultAsync())
                    .FirstOrDefault();

                if (parentTag is null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        Error = $"Parent tag with GUID '{parentTagGuid}' not found in taxonomy '{taxonomyName}'",
                        Hint = "Use GetTaxonomyTags to find valid parent tag GUIDs"
                    }, options.Value.SerializerOptions);
                }

                parentId = parentTag.TagID;
            }

            // Create new tag
            var tag = new TagInfo
            {
                TagTaxonomyID = taxonomy.TaxonomyID,
                TagName = tagName,
                TagTitle = tagTitle,
                TagDescription = description ?? string.Empty,
                TagParentID = parentId,
                TagOrder = order
            };

            await tagProvider.SetAsync(tag);

            var result = new
            {
                Success = true,
                TagId = tag.TagID,
                TagGuid = tag.TagGUID,
                TagName = tag.TagName,
                TagTitle = tag.TagTitle,
                TaxonomyName = taxonomyName,
                ParentId = parentId > 0 ? parentId : (int?)null,
                Message = $"Tag '{tagName}' created in taxonomy '{taxonomyName}'"
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Failed to create tag",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Translates a tag's title and description for a specific language.
    /// </summary>
    [McpServerTool(
        Name = nameof(TranslateTag),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Translate Tag"),
    Description("Adds or updates a translation for a tag's title and description in a specific language by updating TagMetadata JSON")]
    public static async Task<string> TranslateTag(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<TagInfo> tagProvider,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        [Description("Tag GUID to translate")] string tagGuid,
        [Description("Target language code (e.g., 'fr', 'de')")] string languageCode,
        [Description("Translated title for the tag")] string translatedTitle,
        [Description("Translated description (optional)")] string? translatedDescription = null)
    {
        if (string.IsNullOrWhiteSpace(tagGuid) || !Guid.TryParse(tagGuid, out var guid))
        {
            return JsonSerializer.Serialize(new { Error = "Valid tag GUID is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return JsonSerializer.Serialize(new { Error = "Language code is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(translatedTitle))
        {
            return JsonSerializer.Serialize(new { Error = "Translated title is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Verify language exists and get its GUID
            var language = (await languageProvider.Get()
                .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), languageCode)
                .Columns(nameof(ContentLanguageInfo.ContentLanguageGUID), nameof(ContentLanguageInfo.ContentLanguageName))
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (language is null)
            {
                var availableLanguages = await languageProvider.Get()
                    .Columns(nameof(ContentLanguageInfo.ContentLanguageName))
                    .GetEnumerableTypedResultAsync();

                return JsonSerializer.Serialize(new
                {
                    Error = $"Language '{languageCode}' not found",
                    AvailableLanguages = availableLanguages.Select(l => l.ContentLanguageName)
                }, options.Value.SerializerOptions);
            }

            // Find the tag
            var tag = (await tagProvider.Get()
                .WhereEquals(nameof(TagInfo.TagGUID), guid)
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (tag is null)
            {
                return JsonSerializer.Serialize(new
                {
                    Error = $"Tag with GUID '{tagGuid}' not found",
                    Hint = "Use GetTaxonomyTags to find valid tag GUIDs"
                }, options.Value.SerializerOptions);
            }

            // Parse existing TagMetadata JSON or create new structure
            var translations = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(tag.TagMetadata))
            {
                try
                {
                    var metadataDoc = JsonDocument.Parse(tag.TagMetadata);
                    if (metadataDoc.RootElement.TryGetProperty("Translations", out var translationsElement))
                    {
                        foreach (var prop in translationsElement.EnumerateObject())
                        {
                            translations[prop.Name] = new
                            {
                                Title = prop.Value.TryGetProperty("Title", out var titleElement) ? titleElement.GetString() : null,
                                Description = prop.Value.TryGetProperty("Description", out var descElement) ? descElement.GetString() : null
                            };
                        }
                    }
                }
                catch (JsonException)
                {
                    // Invalid JSON, start fresh
                    translations.Clear();
                }
            }

            // Add/update the translation for this language
            var langGuidStr = language.ContentLanguageGUID.ToString();
            translations[langGuidStr] = new
            {
                Title = translatedTitle,
                Description = translatedDescription
            };

            // Serialize back to JSON
            var newMetadata = new { Translations = translations };
            tag.TagMetadata = JsonSerializer.Serialize(newMetadata);

            // Restore original title if it has macro syntax
            if (tag.TagTitle?.StartsWith("{$") == true)
            {
                tag.TagTitle = tag.TagName;
            }

            // Save the tag
            await tagProvider.SetAsync(tag);

            var result = new
            {
                Success = true,
                TagGuid = guid,
                TagName = tag.TagName,
                Language = languageCode,
                LanguageGuid = language.ContentLanguageGUID,
                Message = $"Tag translation applied for '{tag.TagName}' in '{languageCode}'"
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Failed to translate tag",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }
}
