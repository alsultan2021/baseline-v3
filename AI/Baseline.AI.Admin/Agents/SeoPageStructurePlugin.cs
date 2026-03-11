using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using CMS.DataEngine;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Admin.Agents;

/// <summary>
/// Semantic Kernel plugin that reads the page builder JSON configuration
/// and content type fields for the currently open web page. Provides a
/// structured summary of sections, widgets, and SEO-relevant content
/// for deep page-level SEO analysis.
/// </summary>
internal sealed partial class SeoPageStructurePlugin(
    ILogger logger)
{
    /// <inheritdoc cref="SeoFieldUpdatePlugin.WebPageUrlPattern"/>
    [GeneratedRegex(
        @"^/webpages-(?<websiteChannelID>\d+)/(?<languageName>[a-zA-Z0-9_.\-]+)_(?<webPageItemID>\d+)(/.*$|$)",
        RegexOptions.Compiled)]
    private static partial Regex WebPageUrlPattern();

    // SEO-relevant widget property names (case-insensitive matching)
    private static readonly HashSet<string> SeoRelevantProps = new(StringComparer.OrdinalIgnoreCase)
    {
        // Text content
        "headingText", "subtitle", "title", "sectionTitle", "sectionSubtitle",
        "description", "sectionDescription", "sectionHeading",
        "mainTitle", "preHeading", "postHeading",
        // Heading structure
        "headingLevel",
        // Button / CTA
        "primaryButtonText", "secondaryButtonText", "buttonText",
        "primaryButtonUrl", "secondaryButtonUrl", "buttonUrl",
        "ctaText", "tile1CtaText", "tile2CtaText", "tile3CtaText",
        // Tile / card content
        "tile1Title", "tile1Discount", "tile2Title", "tile2Discount",
        "tile3Title", "tile3Discount",
        // FAQ
        "faQ1Question", "faQ1Answer", "faQ2Question", "faQ2Answer",
        "faQ3Question", "faQ3Answer", "faQ4Question", "faQ4Answer",
        "faQ5Question", "faQ5Answer", "faQ6Question", "faQ6Answer",
        // Link / URL
        "linkUrl", "tile1LinkUrl", "tile2LinkUrl", "tile3LinkUrl",
        "videoPopupUrl", "videoBackgroundUrl",
        // Alt text / images
        "altText", "overlayTitle", "overlayText", "badgeText",
        "decorativeText", "rotatingWords",
        // Breadcrumbs / navigation
        "showBreadcrumbs",
        // Visibility / indexing
        "visibility",
        // Content references
        "selectedImages", "backgroundImage", "selectedVideos",
        "selectedFAQs", "selectedPricingPackages", "selectedEnterpriseInfo",
        // Features text
        "features", "conditions", "customHeading",
        // Layout hints
        "showDescriptionAsCaption"
    };

    /// <summary>
    /// Retrieves the full page builder structure (sections, widgets, content type fields)
    /// for the web page currently open in the Kentico admin.
    /// </summary>
    [KernelFunction("get_page_structure")]
    [Description("Retrieves the full page builder structure for a web page. "
        + "Returns content type name, metadata fields, page template, and a structured "
        + "breakdown of all sections and widgets with their SEO-relevant properties "
        + "(headings, text, images, FAQ, CTA). Use this for deep SEO analysis including "
        + "heading hierarchy, content structure, widget configuration, and structured data opportunities. "
        + "Works automatically when viewing a page in admin, or pass explicit pageId and language.")]
    public async Task<string> GetPageStructureAsync(
        [Description("Web page item ID. Omit to auto-detect from current admin page.")] int? pageId = null,
        [Description("Language code (e.g. 'en'). Omit to auto-detect.")] string? language = null)
    {
        try
        {
            int webPageItemId;
            string languageName;

            if (pageId.HasValue && !string.IsNullOrWhiteSpace(language))
            {
                // Explicit parameters — no admin context needed
                webPageItemId = pageId.Value;
                languageName = language;
            }
            else
            {
                // Try auto-detect from admin context
                string? currentPath = GetCurrentUrlPathViaReflection();
                if (string.IsNullOrWhiteSpace(currentPath))
                {
                    return "Error: No page is currently open and no pageId was provided. "
                        + "Either navigate to a web page in the admin content tree, "
                        + "or pass pageId and language parameters explicitly.";
                }

                var match = WebPageUrlPattern().Match(currentPath);
                if (!match.Success)
                {
                    return $"Error: Not viewing a web page. Current path: {currentPath}. "
                        + "Pass pageId and language parameters to analyze a specific page.";
                }

                if (!int.TryParse(match.Groups["webPageItemID"].Value, out webPageItemId))
                {
                    return $"Error: Could not parse web page ID from URL: {currentPath}";
                }

                languageName = language ?? match.Groups["languageName"].Value;
            }

            // Query page data: content type, metadata, page builder JSON
            string query = """
                SELECT
                  ci.ContentItemName,
                  ci.ContentItemID,
                  dc.ClassName,
                  dc.ClassDisplayName,
                  wp.WebPageItemTreePath,
                  wpu.WebPageUrlPath,
                  cd.ContentItemCommonDataVisualBuilderWidgets,
                  cd.ContentItemCommonDataVisualBuilderTemplateConfiguration,
                  cd.MetaData_PageName,
                  cd.MetaData_Title,
                  cd.MetaData_Description,
                  cd.MetaData_Keywords,
                  cd.MetaData_NoIndex,
                  cd.MetaData_ShowInSitemap
                FROM CMS_WebPageItem wp
                JOIN CMS_ContentItem ci ON ci.ContentItemID = wp.WebPageItemContentItemID
                JOIN CMS_Class dc ON dc.ClassID = ci.ContentItemContentTypeID
                JOIN CMS_ContentItemCommonData cd
                  ON cd.ContentItemCommonDataContentItemID = ci.ContentItemID
                  AND cd.ContentItemCommonDataIsLatest = 1
                JOIN CMS_ContentItemLanguageMetadata lm
                  ON lm.ContentItemLanguageMetadataContentItemID = ci.ContentItemID
                JOIN CMS_ContentLanguage cl
                  ON cl.ContentLanguageID = lm.ContentItemLanguageMetadataContentLanguageID
                  AND cl.ContentLanguageName = @LanguageName
                LEFT JOIN CMS_WebPageUrlPath wpu
                  ON wpu.WebPageUrlPathWebPageItemID = wp.WebPageItemID
                WHERE wp.WebPageItemID = @WebPageItemID
                """;

            var parameters = new QueryDataParameters
            {
                { "WebPageItemID", webPageItemId },
                { "LanguageName", languageName }
            };

            var ds = ConnectionHelper.ExecuteQuery(query, parameters, QueryTypeEnum.SQLQuery);

            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                return $"Error: No data found for web page {webPageItemId} in language '{languageName}'.";
            }

            var row = ds.Tables[0].Rows[0];
            var sb = new StringBuilder();

            // Page identity
            string className = row["ClassName"]?.ToString() ?? "Unknown";
            string displayName = row["ClassDisplayName"]?.ToString() ?? "Unknown";
            string treePath = row["WebPageItemTreePath"]?.ToString() ?? "";
            string urlPath = row["WebPageUrlPath"]?.ToString() ?? "";

            sb.AppendLine("# Page Structure Analysis");
            sb.AppendLine();
            sb.AppendLine($"**Content Type:** {className} ({displayName})");
            sb.AppendLine($"**Tree Path:** {treePath}");
            sb.AppendLine($"**URL Path:** /{urlPath}");
            sb.AppendLine();

            // Metadata fields
            sb.AppendLine("## SEO Metadata (Base.Metadata schema)");
            sb.AppendLine($"- **Page Name:** {row["MetaData_PageName"] ?? "(empty)"}");
            sb.AppendLine($"- **Meta Title:** {row["MetaData_Title"] ?? "(empty)"}");
            sb.AppendLine($"- **Meta Description:** {row["MetaData_Description"] ?? "(empty)"}");
            sb.AppendLine($"- **Meta Keywords:** {row["MetaData_Keywords"] ?? "(empty)"}");
            sb.AppendLine($"- **No Index:** {row["MetaData_NoIndex"]}");
            sb.AppendLine($"- **Show in Sitemap:** {row["MetaData_ShowInSitemap"]}");
            sb.AppendLine();

            // Page template
            string? templateJson = row["ContentItemCommonDataVisualBuilderTemplateConfiguration"]?.ToString();
            if (!string.IsNullOrWhiteSpace(templateJson))
            {
                try
                {
                    using var templateDoc = JsonDocument.Parse(templateJson);
                    string templateId = templateDoc.RootElement
                        .GetProperty("identifier").GetString() ?? "Unknown";
                    sb.AppendLine($"**Page Template:** {templateId}");
                    sb.AppendLine();
                }
                catch
                {
                    sb.AppendLine($"**Page Template:** (parse error)");
                    sb.AppendLine();
                }
            }

            // Parse page builder widgets JSON
            string? widgetsJson = row["ContentItemCommonDataVisualBuilderWidgets"]?.ToString();
            if (string.IsNullOrWhiteSpace(widgetsJson))
            {
                sb.AppendLine("## Page Builder Content");
                sb.AppendLine("*No page builder content configured.*");
                return sb.ToString();
            }

            try
            {
                using var doc = JsonDocument.Parse(widgetsJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("editableAreas", out var editableAreas))
                {
                    sb.AppendLine("## Page Builder Content");
                    sb.AppendLine("*No editable areas found.*");
                    return sb.ToString();
                }

                int sectionCount = 0;
                int widgetCount = 0;
                var headingWidgets = new List<(string level, string text)>();

                sb.AppendLine("## Page Builder Structure");
                sb.AppendLine();

                foreach (var area in editableAreas.EnumerateArray())
                {
                    string areaId = area.TryGetProperty("identifier", out var aId)
                        ? aId.GetString() ?? "unknown" : "unknown";

                    if (!area.TryGetProperty("sections", out var sections))
                    {
                        continue;
                    }

                    foreach (var section in sections.EnumerateArray())
                    {
                        sectionCount++;
                        string sectionType = section.TryGetProperty("type", out var st)
                            ? st.GetString() ?? "Unknown" : "Unknown";

                        // Shorten type name for readability
                        string shortSectionType = sectionType.Contains('.')
                            ? sectionType[(sectionType.LastIndexOf('.') + 1)..] : sectionType;

                        sb.AppendLine($"### Section {sectionCount}: {shortSectionType}");
                        sb.AppendLine($"  _Type:_ `{sectionType}`");

                        // Extract SEO-relevant section properties
                        if (section.TryGetProperty("properties", out var sectionProps))
                        {
                            AppendRelevantProperties(sb, sectionProps, "  ");
                        }

                        if (!section.TryGetProperty("zones", out var zones))
                        {
                            sb.AppendLine();
                            continue;
                        }

                        foreach (var zone in zones.EnumerateArray())
                        {
                            string zoneName = zone.TryGetProperty("name", out var zn)
                                ? zn.GetString() ?? "default" : "default";

                            if (!zone.TryGetProperty("widgets", out var widgets))
                            {
                                continue;
                            }

                            foreach (var widget in widgets.EnumerateArray())
                            {
                                widgetCount++;
                                string widgetType = widget.TryGetProperty("type", out var wt)
                                    ? wt.GetString() ?? "Unknown" : "Unknown";

                                string shortWidgetType = widgetType.Contains('.')
                                    ? widgetType[(widgetType.LastIndexOf('.') + 1)..] : widgetType;

                                sb.AppendLine($"  **Widget {widgetCount}: {shortWidgetType}** (zone: {zoneName})");
                                sb.AppendLine($"    _Type:_ `{widgetType}`");

                                // Get variant properties (first variant)
                                if (widget.TryGetProperty("variants", out var variants))
                                {
                                    foreach (var variant in variants.EnumerateArray())
                                    {
                                        if (variant.TryGetProperty("properties", out var wProps))
                                        {
                                            AppendRelevantProperties(sb, wProps, "    ");

                                            // Track heading hierarchy
                                            if (wProps.TryGetProperty("headingLevel", out var hl) &&
                                                wProps.TryGetProperty("headingText", out var ht))
                                            {
                                                string level = hl.GetString() ?? "";
                                                string text = ht.GetString() ?? "";
                                                if (!string.IsNullOrWhiteSpace(text))
                                                {
                                                    // Truncate long text
                                                    if (text.Length > 80)
                                                    {
                                                        text = text[..80] + "...";
                                                    }

                                                    headingWidgets.Add((level, text));
                                                }
                                            }
                                        }

                                        break; // Only first variant
                                    }
                                }

                                sb.AppendLine();
                            }
                        }

                        sb.AppendLine();
                    }
                }

                // Summary
                sb.AppendLine("## Summary");
                sb.AppendLine($"- **Total Sections:** {sectionCount}");
                sb.AppendLine($"- **Total Widgets:** {widgetCount}");
                sb.AppendLine();

                // Heading hierarchy report
                if (headingWidgets.Count > 0)
                {
                    sb.AppendLine("## Heading Hierarchy (from widgets)");
                    foreach (var (level, text) in headingWidgets)
                    {
                        sb.AppendLine($"- **{level}**: {text}");
                    }

                    sb.AppendLine();
                }

                // Content references count
                sb.AppendLine("## SEO Observations");
                bool hasH1 = headingWidgets.Any(h =>
                    h.level.Equals("H1", StringComparison.OrdinalIgnoreCase));
                int h1Count = headingWidgets.Count(h =>
                    h.level.Equals("H1", StringComparison.OrdinalIgnoreCase));

                if (!hasH1)
                {
                    sb.AppendLine("- ⚠ **No H1 heading** found in widget configuration");
                }
                else if (h1Count > 1)
                {
                    sb.AppendLine($"- ⚠ **Multiple H1 headings** ({h1Count}) found — should have exactly 1");
                }
                else
                {
                    sb.AppendLine("- ✓ Single H1 heading present");
                }

                // Check heading level skips
                var levels = headingWidgets
                    .Select(h => h.level.ToUpperInvariant())
                    .Where(l => l.StartsWith('H') && l.Length == 2 && char.IsDigit(l[1]))
                    .Select(l => int.Parse(l[1..]))
                    .OrderBy(l => l)
                    .ToList();

                if (levels.Count > 1)
                {
                    for (int i = 1; i < levels.Count; i++)
                    {
                        if (levels[i] - levels[i - 1] > 1)
                        {
                            sb.AppendLine(
                                $"- ⚠ **Heading level skip**: H{levels[i - 1]} → H{levels[i]} " +
                                $"(missing H{levels[i - 1] + 1})");
                        }
                    }
                }

                // Meta tag checks
                string? metaTitle = row["MetaData_Title"]?.ToString();
                string? metaDesc = row["MetaData_Description"]?.ToString();

                if (string.IsNullOrWhiteSpace(metaTitle))
                {
                    sb.AppendLine("- ⚠ **Meta title is empty**");
                }
                else if (metaTitle.Length < 30 || metaTitle.Length > 60)
                {
                    sb.AppendLine(
                        $"- ⚠ **Meta title length** ({metaTitle.Length} chars) — " +
                        "recommended 30-60 chars");
                }

                if (string.IsNullOrWhiteSpace(metaDesc))
                {
                    sb.AppendLine("- ⚠ **Meta description is empty**");
                }
                else if (metaDesc.Length < 120 || metaDesc.Length > 160)
                {
                    sb.AppendLine(
                        $"- ⚠ **Meta description length** ({metaDesc.Length} chars) — " +
                        "recommended 120-160 chars");
                }

                string? keywords = row["MetaData_Keywords"]?.ToString();
                if (string.IsNullOrWhiteSpace(keywords))
                {
                    sb.AppendLine("- ⚠ **Meta keywords are empty**");
                }

                bool noIndex = row["MetaData_NoIndex"] is true or 1;
                if (noIndex)
                {
                    sb.AppendLine("- ⚠ **Page is set to NoIndex** — not indexed by search engines");
                }

                bool showInSitemap = row["MetaData_ShowInSitemap"] is true or 1;
                if (!showInSitemap)
                {
                    sb.AppendLine("- ℹ Page is NOT included in sitemap");
                }

                // FAQ structured data opportunity
                bool hasFaq = widgetsJson.Contains("FAQ", StringComparison.OrdinalIgnoreCase);
                if (hasFaq)
                {
                    sb.AppendLine("- ℹ **FAQ widget detected** — consider adding FAQPage Schema.org markup");
                }

                return sb.ToString();
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "AIRA SEO: Failed to parse page builder JSON");
                sb.AppendLine("## Page Builder Content");
                sb.AppendLine($"*Error parsing page builder JSON: {ex.Message}*");
                return sb.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA SEO: Failed to get page structure");
            return $"Error retrieving page structure: {ex.Message}";
        }
    }

    /// <summary>
    /// Appends SEO-relevant properties from a widget/section JSON element.
    /// Only includes properties that are meaningful for SEO analysis.
    /// </summary>
    private static void AppendRelevantProperties(StringBuilder sb, JsonElement props, string indent)
    {
        foreach (var prop in props.EnumerateObject())
        {
            if (!SeoRelevantProps.Contains(prop.Name))
            {
                continue;
            }

            string value = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => prop.Value.GetRawText(),
                JsonValueKind.Array => FormatArrayValue(prop.Value),
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(value) || value == "[]")
            {
                continue;
            }

            // Truncate very long text values
            if (value.Length > 120)
            {
                value = value[..120] + "...";
            }

            sb.AppendLine($"{indent}- {prop.Name}: {value}");
        }
    }

    /// <summary>
    /// Formats a JSON array value for display, showing content item references
    /// or simple values.
    /// </summary>
    private static string FormatArrayValue(JsonElement arr)
    {
        var items = new List<string>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.TryGetProperty("identifier", out var id))
            {
                items.Add(id.GetString() ?? "?");
            }
            else
            {
                items.Add(item.GetRawText());
            }
        }

        return items.Count == 0 ? "[]" : $"[{string.Join(", ", items)}]";
    }

    /// <inheritdoc cref="SeoFieldUpdatePlugin.GetCurrentUrlPathViaReflection"/>
    private static string? GetCurrentUrlPathViaReflection()
    {
        try
        {
            var contextType = Type.GetType(
                "Kentico.Xperience.Admin.Base.AiraChatActionContext, Kentico.Xperience.Admin.Base");

            return contextType?
                .GetProperty("CurrentUrlPath",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
                .GetValue(null) as string;
        }
        catch
        {
            return null;
        }
    }
}
