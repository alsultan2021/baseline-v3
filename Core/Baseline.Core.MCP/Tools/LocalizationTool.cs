using System.ComponentModel;
using System.Text.Json;

using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// MCP tools for language and localization discovery.
/// Read-only tools to help AI understand multilingual content structure.
/// </summary>
[McpServerToolType]
public static class LocalizationTool
{
    /// <summary>
    /// Gets all languages configured in the XbK system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetLanguages),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Languages"),
    Description("Gets all languages configured in Xperience by Kentico with fallback chain info")]
    public static async Task<string> GetLanguages(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        [Description("Include fallback chain for each language (default: true)")] bool includeFallbackChain = true)
    {
        var languages = await languageProvider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentLanguageInfo.ContentLanguageGUID),
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageDisplayName),
                nameof(ContentLanguageInfo.ContentLanguageCultureFormat),
                nameof(ContentLanguageInfo.ContentLanguageIsDefault),
                nameof(ContentLanguageInfo.ContentLanguageFallbackContentLanguageID))
            .GetEnumerableTypedResultAsync();

        var languageList = languages.ToList();
        var languageMap = languageList.ToDictionary(l => l.ContentLanguageID);

        var results = languageList.Select(lang =>
        {
            object? fallbackChain = null;

            if (includeFallbackChain && lang.ContentLanguageFallbackContentLanguageID > 0)
            {
                var chain = new List<string>();
                var currentId = lang.ContentLanguageFallbackContentLanguageID;
                var visited = new HashSet<int> { lang.ContentLanguageID };

                while (currentId > 0 && !visited.Contains(currentId))
                {
                    visited.Add(currentId);
                    if (languageMap.TryGetValue(currentId, out var fallback))
                    {
                        chain.Add(fallback.ContentLanguageName);
                        currentId = fallback.ContentLanguageFallbackContentLanguageID;
                    }
                    else
                    {
                        break;
                    }
                }

                fallbackChain = chain;
            }

            return new
            {
                Id = lang.ContentLanguageID,
                Guid = lang.ContentLanguageGUID,
                Code = lang.ContentLanguageName,
                DisplayName = lang.ContentLanguageDisplayName,
                CultureFormat = lang.ContentLanguageCultureFormat,
                IsDefault = lang.ContentLanguageIsDefault,
                FallbackChain = fallbackChain
            };
        });

        var result = new
        {
            TotalCount = languageList.Count,
            DefaultLanguage = languageList.FirstOrDefault(l => l.ContentLanguageIsDefault)?.ContentLanguageName,
            Languages = results
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets language variants available for a content item.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetContentLanguageVariants),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Content Language Variants"),
    Description("Gets all language variants for a specific content item by GUID")]
    public static async Task<string> GetContentLanguageVariants(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        IInfoProvider<ContentItemLanguageMetadataInfo> metadataProvider,
        IInfoProvider<ContentItemInfo> contentItemProvider,
        [Description("Content item GUID")] string contentItemGuid)
    {
        if (!Guid.TryParse(contentItemGuid, out var guid))
        {
            return JsonSerializer.Serialize(new { Error = "Invalid GUID format" }, options.Value.SerializerOptions);
        }

        // Get the content item
        var contentItem = (await contentItemProvider.Get()
            .WhereEquals(nameof(ContentItemInfo.ContentItemGUID), guid)
            .Columns(nameof(ContentItemInfo.ContentItemID), nameof(ContentItemInfo.ContentItemName))
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (contentItem is null)
        {
            return JsonSerializer.Serialize(new { Error = "Content item not found" }, options.Value.SerializerOptions);
        }

        // Get all languages
        var languages = await languageProvider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageDisplayName))
            .GetEnumerableTypedResultAsync();

        var languageMap = languages.ToDictionary(l => l.ContentLanguageID);

        // Get language metadata for this content item
        var metadata = await metadataProvider.Get()
            .WhereEquals(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID), contentItem.ContentItemID)
            .Columns(
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataID),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataDisplayName),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataLatestVersionStatus))
            .GetEnumerableTypedResultAsync();

        var variants = metadata.Select(m =>
        {
            languageMap.TryGetValue(m.ContentItemLanguageMetadataContentLanguageID, out var lang);
            return new
            {
                LanguageId = m.ContentItemLanguageMetadataContentLanguageID,
                LanguageCode = lang?.ContentLanguageName,
                LanguageDisplayName = lang?.ContentLanguageDisplayName,
                DisplayName = m.ContentItemLanguageMetadataDisplayName,
                VersionStatus = m.ContentItemLanguageMetadataLatestVersionStatus.ToString()
            };
        });

        var result = new
        {
            ContentItemId = contentItem.ContentItemID,
            ContentItemName = contentItem.ContentItemName,
            ContentItemGuid = guid,
            Variants = variants,
            AvailableLanguageCodes = variants.Select(v => v.LanguageCode).Where(c => c is not null)
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets localized webpage URLs for a content item across all languages.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetLocalizedWebpageUrls),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Localized Webpage URLs"),
    Description("Gets webpage URLs for a content item in all available languages (useful for hreflang)")]
    public static async Task<string> GetLocalizedWebpageUrls(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        IInfoProvider<WebPageItemInfo> webPageProvider,
        IInfoProvider<WebPageUrlPathInfo> urlPathProvider,
        [Description("WebPage item ID")] int webPageItemId)
    {
        // Get the web page
        var webPage = (await webPageProvider.Get()
            .WhereEquals(nameof(WebPageItemInfo.WebPageItemID), webPageItemId)
            .Columns(
                nameof(WebPageItemInfo.WebPageItemID),
                nameof(WebPageItemInfo.WebPageItemGUID),
                nameof(WebPageItemInfo.WebPageItemName))
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (webPage is null)
        {
            return JsonSerializer.Serialize(new { Error = "Web page not found" }, options.Value.SerializerOptions);
        }

        // Get all languages
        var languages = await languageProvider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageDisplayName),
                nameof(ContentLanguageInfo.ContentLanguageCultureFormat))
            .GetEnumerableTypedResultAsync();

        var languageMap = languages.ToDictionary(l => l.ContentLanguageID);

        // Get URL paths for this web page
        var urlPaths = await urlPathProvider.Get()
            .WhereEquals(nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID), webPageItemId)
            .WhereEquals(nameof(WebPageUrlPathInfo.WebPageUrlPathIsDraft), false)
            .Columns(
                nameof(WebPageUrlPathInfo.WebPageUrlPathID),
                nameof(WebPageUrlPathInfo.WebPageUrlPathContentLanguageID),
                nameof(WebPageUrlPathInfo.WebPageUrlPath),
                nameof(WebPageUrlPathInfo.WebPageUrlPathHash))
            .GetEnumerableTypedResultAsync();

        var localizedUrls = urlPaths.Select(path =>
        {
            languageMap.TryGetValue(path.WebPageUrlPathContentLanguageID, out var lang);
            return new
            {
                LanguageCode = lang?.ContentLanguageName,
                LanguageDisplayName = lang?.ContentLanguageDisplayName,
                CultureFormat = lang?.ContentLanguageCultureFormat,
                UrlPath = path.WebPageUrlPath,
                HreflangValue = lang?.ContentLanguageCultureFormat?.ToLowerInvariant()
            };
        }).OrderBy(u => u.LanguageCode);

        var result = new
        {
            WebPageItemId = webPage.WebPageItemID,
            WebPageItemGuid = webPage.WebPageItemGUID,
            WebPageName = webPage.WebPageItemName,
            LocalizedUrls = localizedUrls,
            AvailableLanguages = localizedUrls.Select(u => u.LanguageCode).Where(c => c is not null)
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Gets content items that need translation to a target language.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetMissingTranslations),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Missing Translations"),
    Description("Finds content items that exist in source language but not in target language")]
    public static async Task<string> GetMissingTranslations(
        IOptions<BaselineMCPConfiguration> options,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        IInfoProvider<ContentItemInfo> contentItemProvider,
        IInfoProvider<ContentItemLanguageMetadataInfo> metadataProvider,
        [Description("Source language code (e.g., 'en')")] string sourceLanguage,
        [Description("Target language code (e.g., 'fr')")] string targetLanguage,
        [Description("Content type name filter (optional)")] string? contentTypeName = null,
        [Description("Maximum results (default: 50)")] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 500);

        // Get language IDs
        var languages = await languageProvider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentLanguageInfo.ContentLanguageName))
            .GetEnumerableTypedResultAsync();

        var langMap = languages.ToDictionary(
            l => l.ContentLanguageName,
            l => l.ContentLanguageID,
            StringComparer.OrdinalIgnoreCase);

        if (!langMap.TryGetValue(sourceLanguage, out var sourceId))
        {
            return JsonSerializer.Serialize(new
            {
                Error = $"Source language '{sourceLanguage}' not found",
                AvailableLanguages = langMap.Keys
            }, options.Value.SerializerOptions);
        }

        if (!langMap.TryGetValue(targetLanguage, out var targetId))
        {
            return JsonSerializer.Serialize(new
            {
                Error = $"Target language '{targetLanguage}' not found",
                AvailableLanguages = langMap.Keys
            }, options.Value.SerializerOptions);
        }

        // Get content items with source language
        var sourceMetadataQuery = metadataProvider.Get()
            .WhereEquals(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID), sourceId)
            .Columns(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID));

        // Get content items with target language
        var targetMetadataQuery = metadataProvider.Get()
            .WhereEquals(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID), targetId)
            .Columns(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID));

        var sourceItems = (await sourceMetadataQuery.GetEnumerableTypedResultAsync())
            .Select(m => m.ContentItemLanguageMetadataContentItemID)
            .ToHashSet();

        var targetItems = (await targetMetadataQuery.GetEnumerableTypedResultAsync())
            .Select(m => m.ContentItemLanguageMetadataContentItemID)
            .ToHashSet();

        // Find items in source but not in target
        var missingItemIds = sourceItems.Except(targetItems).Take(limit).ToList();

        if (missingItemIds.Count == 0)
        {
            return JsonSerializer.Serialize(new
            {
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                MissingCount = 0,
                Message = "All content is translated!"
            }, options.Value.SerializerOptions);
        }

        // Get content item details
        var contentItemQuery = contentItemProvider.Get()
            .WhereIn(nameof(ContentItemInfo.ContentItemID), missingItemIds)
            .Columns(
                nameof(ContentItemInfo.ContentItemID),
                nameof(ContentItemInfo.ContentItemGUID),
                nameof(ContentItemInfo.ContentItemName),
                nameof(ContentItemInfo.ContentItemContentTypeID));

        if (!string.IsNullOrEmpty(contentTypeName))
        {
            // Filter by content type if specified
            contentItemQuery = contentItemQuery.Source(s => s.InnerJoin<DataClassInfo>(
                nameof(ContentItemInfo.ContentItemContentTypeID),
                nameof(DataClassInfo.ClassID)))
                .WhereEquals(nameof(DataClassInfo.ClassName), contentTypeName);
        }

        var contentItems = await contentItemQuery.GetEnumerableTypedResultAsync();

        var missingItems = contentItems.Select(c => new
        {
            ContentItemId = c.ContentItemID,
            ContentItemGuid = c.ContentItemGUID,
            ContentItemName = c.ContentItemName
        });

        var result = new
        {
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            MissingCount = sourceItems.Except(targetItems).Count(),
            Limit = limit,
            MissingItems = missingItems
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    // ==========================================
    // XperienceCommunity.Localization (Nittin) Tools
    // ==========================================

    /// <summary>
    /// Gets all localization keys from the XperienceCommunity.Localization module.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetNittinLocalizationKeys),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Nittin Localization Keys"),
    Description("Gets all localization keys from XperienceCommunity.Localization module (if installed)")]
    public static async Task<string> GetNittinLocalizationKeys(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Filter by key name pattern (optional, uses SQL LIKE)")] string? keyNamePattern = null,
        [Description("Maximum results (default: 100)")] int limit = 100,
        [Description("Number of records to skip (default: 0)")] int offset = 0)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        limit = Math.Clamp(limit, 1, 500);
        offset = Math.Max(0, offset);

        try
        {
            string whereClause = string.IsNullOrEmpty(keyNamePattern)
                ? ""
                : "WHERE LocalizationKeyItemName LIKE @pattern";

            string sql = $@"
                SELECT 
                    LocalizationKeyItemId,
                    LocalizationKeyItemGuid,
                    LocalizationKeyItemName,
                    LocalizationKeyItemDescription
                FROM NittinLocalization_LocalizationKeyItem
                {whereClause}
                ORDER BY LocalizationKeyItemName
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            string countSql = $@"
                SELECT COUNT(*) 
                FROM NittinLocalization_LocalizationKeyItem
                {whereClause}";

            var keys = new List<object>();
            int totalCount = 0;

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            // Get total count
            await using (var countCmd = new SqlCommand(countSql, connection))
            {
                if (!string.IsNullOrEmpty(keyNamePattern))
                {
                    countCmd.Parameters.AddWithValue("@pattern", $"%{keyNamePattern}%");
                }
                totalCount = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            // Get paginated results
            await using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@limit", limit);
                if (!string.IsNullOrEmpty(keyNamePattern))
                {
                    cmd.Parameters.AddWithValue("@pattern", $"%{keyNamePattern}%");
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    keys.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Guid = reader.GetGuid(1),
                        Name = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            }

            var result = new
            {
                TotalCount = totalCount,
                Limit = limit,
                Offset = offset,
                Keys = keys
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed. Install via: dotnet add package XperienceCommunity.Localization",
                Details = ex.Message
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets translations for a specific localization key.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetNittinKeyTranslations),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Nittin Key Translations"),
    Description("Gets all translations for a specific localization key from XperienceCommunity.Localization")]
    public static async Task<string> GetNittinKeyTranslations(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Localization key name (e.g., 'cookies.accept.all')")] string keyName)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(keyName))
        {
            return JsonSerializer.Serialize(new { Error = "Key name is required" }, options.Value.SerializerOptions);
        }

        try
        {
            const string sql = @"
                SELECT 
                    k.LocalizationKeyItemId,
                    k.LocalizationKeyItemName,
                    k.LocalizationKeyItemDescription,
                    t.LocalizationTranslationItemID,
                    t.LocalizationTranslationItemText,
                    l.ContentLanguageID,
                    l.ContentLanguageName,
                    l.ContentLanguageDisplayName
                FROM NittinLocalization_LocalizationKeyItem k
                LEFT JOIN NittinLocalization_LocalizationTranslationItem t 
                    ON k.LocalizationKeyItemId = t.LocalizationTranslationItemLocalizationKeyItemId
                LEFT JOIN CMS_ContentLanguage l 
                    ON t.LocalizationTranslationItemContentLanguageId = l.ContentLanguageID
                WHERE k.LocalizationKeyItemName = @keyName
                ORDER BY l.ContentLanguageName";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@keyName", keyName);

            var translations = new List<object>();
            int? keyId = null;
            string? keyDescription = null;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                keyId ??= reader.GetInt32(0);
                keyDescription ??= reader.IsDBNull(2) ? null : reader.GetString(2);

                if (!reader.IsDBNull(3))
                {
                    translations.Add(new
                    {
                        TranslationId = reader.GetInt32(3),
                        Text = reader.IsDBNull(4) ? null : reader.GetString(4),
                        LanguageId = reader.GetInt32(5),
                        LanguageCode = reader.GetString(6),
                        LanguageDisplayName = reader.GetString(7)
                    });
                }
            }

            if (keyId is null)
            {
                return JsonSerializer.Serialize(new
                {
                    Error = $"Key '{keyName}' not found"
                }, options.Value.SerializerOptions);
            }

            var result = new
            {
                KeyId = keyId,
                KeyName = keyName,
                Description = keyDescription,
                TranslationCount = translations.Count,
                Translations = translations
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets localization keys missing translations for a specific language.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetNittinMissingTranslations),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Nittin Missing Translations"),
    Description("Finds localization keys that don't have translations for a specific language")]
    public static async Task<string> GetNittinMissingTranslations(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Target language code (e.g., 'fr', 'de')")] string targetLanguageCode,
        [Description("Filter by key name pattern (optional)")] string? keyNamePattern = null,
        [Description("Maximum results (default: 100)")] int limit = 100)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(targetLanguageCode))
        {
            return JsonSerializer.Serialize(new { Error = "Target language code is required" }, options.Value.SerializerOptions);
        }

        limit = Math.Clamp(limit, 1, 500);

        try
        {
            string patternFilter = string.IsNullOrEmpty(keyNamePattern)
                ? ""
                : "AND k.LocalizationKeyItemName LIKE @pattern";

            string sql = $@"
                SELECT TOP (@limit)
                    k.LocalizationKeyItemId,
                    k.LocalizationKeyItemName,
                    k.LocalizationKeyItemDescription
                FROM NittinLocalization_LocalizationKeyItem k
                WHERE NOT EXISTS (
                    SELECT 1 FROM NittinLocalization_LocalizationTranslationItem t
                    INNER JOIN CMS_ContentLanguage l 
                        ON t.LocalizationTranslationItemContentLanguageId = l.ContentLanguageID
                    WHERE t.LocalizationTranslationItemLocalizationKeyItemId = k.LocalizationKeyItemId
                    AND l.ContentLanguageName = @langCode
                )
                {patternFilter}
                ORDER BY k.LocalizationKeyItemName";

            string countSql = $@"
                SELECT COUNT(*)
                FROM NittinLocalization_LocalizationKeyItem k
                WHERE NOT EXISTS (
                    SELECT 1 FROM NittinLocalization_LocalizationTranslationItem t
                    INNER JOIN CMS_ContentLanguage l 
                        ON t.LocalizationTranslationItemContentLanguageId = l.ContentLanguageID
                    WHERE t.LocalizationTranslationItemLocalizationKeyItemId = k.LocalizationKeyItemId
                    AND l.ContentLanguageName = @langCode
                )
                {patternFilter}";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            int totalMissing = 0;
            await using (var countCmd = new SqlCommand(countSql, connection))
            {
                countCmd.Parameters.AddWithValue("@langCode", targetLanguageCode);
                if (!string.IsNullOrEmpty(keyNamePattern))
                {
                    countCmd.Parameters.AddWithValue("@pattern", $"%{keyNamePattern}%");
                }
                totalMissing = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            var missingKeys = new List<object>();
            await using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@limit", limit);
                cmd.Parameters.AddWithValue("@langCode", targetLanguageCode);
                if (!string.IsNullOrEmpty(keyNamePattern))
                {
                    cmd.Parameters.AddWithValue("@pattern", $"%{keyNamePattern}%");
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    missingKeys.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    });
                }
            }

            var result = new
            {
                TargetLanguage = targetLanguageCode,
                TotalMissing = totalMissing,
                Limit = limit,
                MissingKeys = missingKeys
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets translation coverage statistics per language.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetNittinTranslationStats),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Nittin Translation Statistics"),
    Description("Gets translation coverage statistics for each configured language")]
    public static async Task<string> GetNittinTranslationStats(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        try
        {
            const string sql = @"
                SELECT 
                    l.ContentLanguageID,
                    l.ContentLanguageName,
                    l.ContentLanguageDisplayName,
                    COUNT(t.LocalizationTranslationItemID) as TranslatedCount,
                    (SELECT COUNT(*) FROM NittinLocalization_LocalizationKeyItem) as TotalKeys
                FROM CMS_ContentLanguage l
                LEFT JOIN NittinLocalization_LocalizationTranslationItem t 
                    ON l.ContentLanguageID = t.LocalizationTranslationItemContentLanguageId
                GROUP BY l.ContentLanguageID, l.ContentLanguageName, l.ContentLanguageDisplayName
                ORDER BY l.ContentLanguageName";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(sql, connection);

            var stats = new List<object>();
            int totalKeys = 0;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int translated = reader.GetInt32(3);
                totalKeys = reader.GetInt32(4);
                double percentage = totalKeys > 0 ? Math.Round((double)translated / totalKeys * 100, 1) : 0;

                stats.Add(new
                {
                    LanguageId = reader.GetInt32(0),
                    LanguageCode = reader.GetString(1),
                    LanguageDisplayName = reader.GetString(2),
                    TranslatedKeys = translated,
                    MissingKeys = totalKeys - translated,
                    CoveragePercent = percentage
                });
            }

            var result = new
            {
                TotalKeys = totalKeys,
                Languages = stats
            };

            return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Searches for localization keys and their translations.
    /// </summary>
    [McpServerTool(
        Name = nameof(SearchNittinTranslations),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Search Nittin Translations"),
    Description("Searches localization keys and translation text for a given term")]
    public static async Task<string> SearchNittinTranslations(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Search term (searches in key names and translation text)")] string searchTerm,
        [Description("Filter by language code (optional)")] string? languageCode = null,
        [Description("Maximum results (default: 50)")] int limit = 50)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return JsonSerializer.Serialize(new { Error = "Search term is required" }, options.Value.SerializerOptions);
        }

        limit = Math.Clamp(limit, 1, 200);

        try
        {
            string langFilter = string.IsNullOrEmpty(languageCode)
                ? ""
                : "AND l.ContentLanguageName = @langCode";

            string sql = $@"
                SELECT TOP (@limit)
                    k.LocalizationKeyItemId,
                    k.LocalizationKeyItemName,
                    t.LocalizationTranslationItemText,
                    l.ContentLanguageName,
                    l.ContentLanguageDisplayName
                FROM NittinLocalization_LocalizationKeyItem k
                LEFT JOIN NittinLocalization_LocalizationTranslationItem t 
                    ON k.LocalizationKeyItemId = t.LocalizationTranslationItemLocalizationKeyItemId
                LEFT JOIN CMS_ContentLanguage l 
                    ON t.LocalizationTranslationItemContentLanguageId = l.ContentLanguageID
                WHERE (
                    k.LocalizationKeyItemName LIKE @search
                    OR t.LocalizationTranslationItemText LIKE @search
                )
                {langFilter}
                ORDER BY k.LocalizationKeyItemName, l.ContentLanguageName";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
            if (!string.IsNullOrEmpty(languageCode))
            {
                cmd.Parameters.AddWithValue("@langCode", languageCode);
            }

            var results = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    KeyId = reader.GetInt32(0),
                    KeyName = reader.GetString(1),
                    TranslationText = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LanguageCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LanguageDisplayName = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            var searchResult = new
            {
                SearchTerm = searchTerm,
                LanguageFilter = languageCode,
                ResultCount = results.Count,
                Results = results
            };

            return JsonSerializer.Serialize(searchResult, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }

    // ==========================================
    // Write Operations (Destructive)
    // ==========================================

    /// <summary>
    /// Creates a new localization key.
    /// </summary>
    [McpServerTool(
        Name = nameof(AddNittinLocalizationKey),
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Add Nittin Localization Key"),
    Description("Creates a new localization key in XperienceCommunity.Localization")]
    public static async Task<string> AddNittinLocalizationKey(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Unique key name (e.g., 'cookies.accept.all')")] string keyName,
        [Description("Optional description of the key's purpose")] string? description = null)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(keyName))
        {
            return JsonSerializer.Serialize(new { Error = "Key name is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Check if key already exists
            const string checkSql = @"
                SELECT LocalizationKeyItemId 
                FROM NittinLocalization_LocalizationKeyItem 
                WHERE LocalizationKeyItemName = @keyName";

            const string insertSql = @"
                INSERT INTO NittinLocalization_LocalizationKeyItem 
                    (LocalizationKeyItemGuid, LocalizationKeyItemName, LocalizationKeyItemDescription)
                OUTPUT INSERTED.LocalizationKeyItemId
                VALUES (NEWID(), @keyName, @description)";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            // Check for existing key
            await using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@keyName", keyName);
                var existing = await checkCmd.ExecuteScalarAsync();
                if (existing is not null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        Error = $"Key '{keyName}' already exists",
                        ExistingKeyId = (int)existing
                    }, options.Value.SerializerOptions);
                }
            }

            // Insert new key
            await using var insertCmd = new SqlCommand(insertSql, connection);
            insertCmd.Parameters.AddWithValue("@keyName", keyName);
            insertCmd.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);

            var newId = (int)(await insertCmd.ExecuteScalarAsync() ?? 0);

            return JsonSerializer.Serialize(new
            {
                Success = true,
                KeyId = newId,
                KeyName = keyName,
                Description = description,
                Message = $"Localization key '{keyName}' created successfully"
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Adds or updates a translation for a localization key.
    /// </summary>
    [McpServerTool(
        Name = nameof(SetNittinTranslation),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Set Nittin Translation"),
    Description("Adds or updates a translation for a localization key (upsert)")]
    public static async Task<string> SetNittinTranslation(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Localization key name (e.g., 'cookies.accept.all')")] string keyName,
        [Description("Language code (e.g., 'en', 'fr', 'de')")] string languageCode,
        [Description("Translation text")] string translationText)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(keyName))
        {
            return JsonSerializer.Serialize(new { Error = "Key name is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return JsonSerializer.Serialize(new { Error = "Language code is required" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(translationText))
        {
            return JsonSerializer.Serialize(new { Error = "Translation text is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Get key ID and language ID
            const string lookupSql = @"
                SELECT k.LocalizationKeyItemId, l.ContentLanguageID
                FROM NittinLocalization_LocalizationKeyItem k
                CROSS JOIN CMS_ContentLanguage l
                WHERE k.LocalizationKeyItemName = @keyName
                AND l.ContentLanguageName = @langCode";

            // Check existing translation
            const string checkSql = @"
                SELECT LocalizationTranslationItemID
                FROM NittinLocalization_LocalizationTranslationItem
                WHERE LocalizationTranslationItemLocalizationKeyItemId = @keyId
                AND LocalizationTranslationItemContentLanguageId = @langId";

            // Insert new translation
            const string insertSql = @"
                INSERT INTO NittinLocalization_LocalizationTranslationItem
                    (LocalizationTranslationItemGuid, LocalizationTranslationItemLocalizationKeyItemId, 
                     LocalizationTranslationItemContentLanguageId, LocalizationTranslationItemText)
                OUTPUT INSERTED.LocalizationTranslationItemID
                VALUES (NEWID(), @keyId, @langId, @text)";

            // Update existing translation
            const string updateSql = @"
                UPDATE NittinLocalization_LocalizationTranslationItem
                SET LocalizationTranslationItemText = @text
                WHERE LocalizationTranslationItemID = @transId";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            int keyId = 0;
            int langId = 0;

            // Get key and language IDs
            await using (var lookupCmd = new SqlCommand(lookupSql, connection))
            {
                lookupCmd.Parameters.AddWithValue("@keyName", keyName);
                lookupCmd.Parameters.AddWithValue("@langCode", languageCode);

                await using var reader = await lookupCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return JsonSerializer.Serialize(new
                    {
                        Error = $"Key '{keyName}' or language '{languageCode}' not found",
                        Hint = "Use GetNittinLocalizationKeys to list keys, GetLanguages to list languages"
                    }, options.Value.SerializerOptions);
                }

                keyId = reader.GetInt32(0);
                langId = reader.GetInt32(1);
            }

            // Check if translation exists
            int? existingTransId = null;
            await using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@keyId", keyId);
                checkCmd.Parameters.AddWithValue("@langId", langId);
                var result = await checkCmd.ExecuteScalarAsync();
                if (result is not null)
                {
                    existingTransId = (int)result;
                }
            }

            bool isUpdate = existingTransId.HasValue;
            int translationId;

            if (isUpdate)
            {
                // Update existing
                await using var updateCmd = new SqlCommand(updateSql, connection);
                updateCmd.Parameters.AddWithValue("@text", translationText);
                updateCmd.Parameters.AddWithValue("@transId", existingTransId!.Value);
                await updateCmd.ExecuteNonQueryAsync();
                translationId = existingTransId!.Value;
            }
            else
            {
                // Insert new
                await using var insertCmd = new SqlCommand(insertSql, connection);
                insertCmd.Parameters.AddWithValue("@keyId", keyId);
                insertCmd.Parameters.AddWithValue("@langId", langId);
                insertCmd.Parameters.AddWithValue("@text", translationText);
                translationId = (int)(await insertCmd.ExecuteScalarAsync() ?? 0);
            }

            return JsonSerializer.Serialize(new
            {
                Success = true,
                Action = isUpdate ? "Updated" : "Created",
                TranslationId = translationId,
                KeyName = keyName,
                LanguageCode = languageCode,
                Text = translationText,
                Message = isUpdate
                    ? $"Translation updated for '{keyName}' in {languageCode}"
                    : $"Translation created for '{keyName}' in {languageCode}"
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Deletes a localization key and all its translations.
    /// </summary>
    [McpServerTool(
        Name = nameof(DeleteNittinLocalizationKey),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Delete Nittin Localization Key"),
    Description("Deletes a localization key and all its translations (use with caution)")]
    public static async Task<string> DeleteNittinLocalizationKey(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Localization key name to delete")] string keyName)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(keyName))
        {
            return JsonSerializer.Serialize(new { Error = "Key name is required" }, options.Value.SerializerOptions);
        }

        try
        {
            // Get key ID first
            const string lookupSql = @"
                SELECT LocalizationKeyItemId 
                FROM NittinLocalization_LocalizationKeyItem 
                WHERE LocalizationKeyItemName = @keyName";

            // Delete translations first (FK constraint)
            const string deleteTransSql = @"
                DELETE FROM NittinLocalization_LocalizationTranslationItem
                WHERE LocalizationTranslationItemLocalizationKeyItemId = @keyId";

            // Delete key
            const string deleteKeySql = @"
                DELETE FROM NittinLocalization_LocalizationKeyItem
                WHERE LocalizationKeyItemId = @keyId";

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            int keyId;
            await using (var lookupCmd = new SqlCommand(lookupSql, connection))
            {
                lookupCmd.Parameters.AddWithValue("@keyName", keyName);
                var result = await lookupCmd.ExecuteScalarAsync();
                if (result is null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        Error = $"Key '{keyName}' not found"
                    }, options.Value.SerializerOptions);
                }
                keyId = (int)result;
            }

            // Delete translations
            int translationsDeleted;
            await using (var deleteTransCmd = new SqlCommand(deleteTransSql, connection))
            {
                deleteTransCmd.Parameters.AddWithValue("@keyId", keyId);
                translationsDeleted = await deleteTransCmd.ExecuteNonQueryAsync();
            }

            // Delete key
            await using (var deleteKeyCmd = new SqlCommand(deleteKeySql, connection))
            {
                deleteKeyCmd.Parameters.AddWithValue("@keyId", keyId);
                await deleteKeyCmd.ExecuteNonQueryAsync();
            }

            return JsonSerializer.Serialize(new
            {
                Success = true,
                DeletedKeyId = keyId,
                DeletedKeyName = keyName,
                TranslationsDeleted = translationsDeleted,
                Message = $"Deleted key '{keyName}' and {translationsDeleted} translation(s)"
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "XperienceCommunity.Localization tables not found",
                Message = "The NittinLocalization module may not be installed."
            }, options.Value.SerializerOptions);
        }
    }
}
