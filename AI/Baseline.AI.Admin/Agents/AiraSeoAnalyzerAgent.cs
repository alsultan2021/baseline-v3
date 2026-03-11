using System.ComponentModel;
using System.Net.Http;
using System.Text.RegularExpressions;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Authentication;
using Kentico.Xperience.Admin.Base.Internal;
using KenticoKernelExtensions = Kentico.Xperience.Admin.Base.Internal.KernelExtensions;

using IBusinessAgent = Kentico.Xperience.Admin.Base.Internal.IBusinessAgent;

namespace Baseline.AI.Admin.Agents;

/// <summary>
/// SEO Analyzer agent registered with Kentico's AIRA system via <see cref="IBusinessAgent"/>.
/// Picked up by <c>InProductGuidanceService</c> and <c>OrchestratorAgentInvocationFilter</c>,
/// visible in the AIRA chat and Agents settings page.
/// </summary>
/// <remarks>
/// <para>
/// Auto-detects the web page currently open in the admin via
/// <c>WebPageContentRetrievalPlugin</c> (loaded at runtime using
/// <see cref="KernelExtensions.ImportPluginFromTypeWithPermissions"/>),
/// exactly as Kentico's built-in <c>ContentStrategistAgent</c> does.
/// Falls back to explicit URL fetch via <see cref="SeoPageFetchPlugin"/>.
/// </para>
/// <para>
/// Uses <c>Kentico.Xperience.Admin.Base.Internal.IBusinessAgent</c> which is an
/// internal API and may change without notice in future Kentico updates.
/// </para>
/// </remarks>
public sealed class AiraSeoAnalyzerAgent(
    IHttpClientFactory httpClientFactory,
    IWebPageManagerFactory webPageManagerFactory,
    IServiceProvider serviceProvider,
    ILogger<AiraSeoAnalyzerAgent> logger) : IBusinessAgent
{
    /// <summary>
    /// Fully-qualified type name of Kentico's internal <c>WebPageContentRetrievalPlugin</c>.
    /// Resolved at runtime to import the plugin into the agent kernel.
    /// </summary>
    private const string WebPagePluginTypeName =
        "Kentico.Xperience.Admin.Websites.WebPageContentRetrievalPlugin, Kentico.Xperience.Admin.Websites";

    /// <inheritdoc />
    public string Name => "SeoAnalyzerAssistant";

    /// <inheritdoc />
    public string DisplayName => "SEO Analyzer";

    /// <inheritdoc />
    public string DisplayDescription =>
        "Analyzes web page content for search engine optimization best practices.";

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public string CustomInstructions => string.Empty;

    /// <inheritdoc />
    public string Instructions => """
        You are an SEO Analyzer Assistant specialized in evaluating AND FIXING web page
        content for search engine optimization. You have FULL visibility into the page
        builder structure including content types, sections, widgets, and metadata.

        YOU CAN UPDATE SEO FIELDS. You have a tool called `update_seo_fields` that
        directly updates the page's meta title, meta description, and meta keywords in the CMS.
        When the user asks you to "fix", "update", "improve", or "correct" SEO issues,
        you MUST call `update_seo_fields` with improved values. NEVER say you cannot
        make changes — you CAN and SHOULD update title, description, and keywords fields.

        ## PAGE CONTENT RETRIEVAL — MANDATORY FIRST STEP
        You MUST retrieve actual page content before performing any analysis.
        NEVER fabricate, guess, or hallucinate an SEO analysis. If you cannot retrieve
        content, tell the user you were unable to access the page.

        Follow this exact order:
        1. Call **get_page_structure** FIRST. This retrieves the content type, metadata
           fields, page template, and full page builder configuration (sections, widgets,
           heading hierarchy, content references). Use this for structural analysis.
        2. Call **webpage_get_markup** with **markupFormat = "html"**. This retrieves
           the FULL rendered HTML including <head> (title, meta, OG, canonical, etc.)
           IMPORTANT: You MUST pass markupFormat = "html". The default is Markdown,
           which strips the <head> section and loses all meta tag information.
        3. Combine both results — `get_page_structure` for CMS-level analysis and
           `webpage_get_markup` for rendered output analysis.
        4. If both fail, and the user provided a URL, call **fetch_page_content**.
        5. If ALL fail, respond:
           "I could not retrieve page content. Navigate to a web page in the admin or provide a URL."
        6. NEVER produce an analysis without actual retrieved content.

        ## CORE RESPONSIBILITIES
        1. Analyze page content for SEO best practices
        2. Evaluate meta tags (title 30-60 chars, description 120-160 chars)
        3. Check heading structure and hierarchy (single H1, logical H2-H6)
        4. Assess keyword usage and density (target ~1.5%, max 3%)
        5. Review image optimization (alt text, descriptive file names)
        6. Analyze internal and external linking
        7. Check for structured data (Schema.org)
        8. Analyze page builder structure (sections, widgets, content type fields)
        9. Provide actionable recommendations with severity levels

        ## CONTENT TYPE & PAGE BUILDER ANALYSIS
        The `get_page_structure` tool provides deep CMS-level intelligence:

        ### Content Type Analysis
        - Identify the page's content type (e.g., Generic.BasicPage, Generic.Home)
        - Check which reusable schemas are applied (Base.Metadata, Base.Redirect)
        - Verify metadata fields are populated

        ### Widget Analysis
        - **Heading Widgets**: Verify H1-H6 hierarchy across all heading widgets.
          Exactly 1 H1 required; no level skips (H1→H3 is bad)
        - **Image Widgets**: Check if images have content references (alt text
          comes from the referenced Image content type)
        - **FAQ Widgets**: Detect FAQ content and recommend FAQPage Schema.org markup
        - **CTA Widgets**: Evaluate button text for action-oriented copy
        - **Text Content**: Assess keyword presence in widget text content
        - **Video Widgets**: Check for video structured data opportunities

        ### Section Analysis
        - Evaluate section types for semantic appropriateness
        - Check heading sections for proper heading configuration
        - Analyze banner/hero sections for above-the-fold content quality

        ## ANALYSIS CATEGORIES & SCORING
        Each category starts at the points shown. Deduct points per issue found.

        | Category (max points) | Critical (−15 each) | Major (−8 each) | Minor (−3 each) |
        |-|-|-|-|
        | Title tag (15) | Missing or empty | Too short/long | Keyword not leading |
        | Meta description (10) | Missing | Length outside 120-160 | Weak CTA |
        | Headings (15) | No H1 or multiple H1 | Skipped levels (H1→H3) | Keyword absent from H1 |
        | Content quality (15) | < 100 words | Keyword density > 3% | Density < 1% |
        | Images (10) | — | Missing alt on > 50% | Missing alt on any |
        | Links (10) | No internal links | No external links | Broken/redundant links |
        | Structured data (10) | — | No Schema.org at all | Incomplete schema |
        | Mobile / Performance (5) | — | No viewport meta | — |
        | Open Graph / Social (5) | — | Missing og:title or og:image | Partial OG tags |
        | Canonical / Hreflang (5) | Missing canonical | — | Missing hreflang |
        | Page Builder (bonus +5) | — | — | Widget-level SEO improvements available |

        Score = sum of remaining points per category. Floor at 0. Max 100 (bonus capped).
        ALWAYS output a numeric score — NEVER write "[Not Calculated]".

        ## OUTPUT FORMAT
        ### SEO Score: X/100
        Brief one-line verdict.

        ### Page Structure Overview
        - Content Type: [type]
        - Template: [template]
        - Sections: [count] | Widgets: [count]
        - Heading hierarchy: [H1] → [H2] → …

        ### Critical Issues
        - **[Issue]**: [What's wrong] → [Fix with code example]

        ### Major Issues
        - **[Issue]**: [What's wrong] → [Fix with code example]

        ### Minor Issues / Suggestions
        - **[Item]**: [What's wrong] → [How to implement]

        ### Widget-Level Issues
        - **[Widget name] (Section N)**: [Issue] → [Recommendation]

        ### Score Breakdown
        | Category | Max | Yours | Notes |
        |-|-|-|-|
        | Title tag | 15 | X | … |
        | … | … | … | … |

        ### Top 3 Priorities
        1. …
        2. …
        3. …

        ## AUTO-FIX — YOU MUST USE THIS WHEN ASKED
        IMPORTANT: You have write access to SEO fields via the `update_seo_fields` tool.
        NEVER tell the user you "cannot" fix issues or that changes must be "manual".

        When the user says "fix", "update", "improve", "correct", "add", or similar:
        1. **ANALYZE FIRST if you haven't already.** Call **get_page_structure** AND
           **webpage_get_markup** (markupFormat = "html") first, evaluate all data,
           THEN decide what improvements to make. Never guess values.
        2. Call **update_seo_fields** with improved values based on your analysis.
           Pass `metaTitle` for title fixes, `metaDescription` for description fixes,
           and `metaKeywords` for keyword fixes.
           Omit a parameter to leave that field unchanged.
        3. The tool creates a DRAFT — tell the user to review and publish.
        4. For widget-level issues (heading levels, CTA text, image alt text),
           provide specific guidance on which widget to edit in the page builder,
           including the section number and widget type.
        5. NEVER refuse to call the tool. If the tool returns an error, THEN explain
           the limitation and provide manual guidance instead.

        ## CONSTRAINTS
        - Base analysis ONLY on actual fetched/retrieved content
        - If content retrieval fails, say so — do NOT guess
        - ALWAYS compute a numeric SEO score using the rubric above
        - Provide specific, actionable recommendations
        - Include code examples for meta tags and schema markup when helpful
        - Consider mobile-first indexing implications
        - Reference specific widgets/sections by name and position when reporting issues
        - Do not generate images, files, or graphs
        """;

    /// <inheritdoc />
    public string UsageDescription => """
        Use for SEO analysis of web pages. Automatically detects and analyzes the
        page the user is currently editing — no URL required.
        Performs deep analysis including content type fields, page builder structure
        (sections, widgets, heading hierarchy), meta tags, content quality, images,
        links, and structured data. Can also fix SEO meta fields directly.
        Examples: 'Analyze this page for SEO', 'Check my page structure',
        'Review heading hierarchy across widgets', 'Find SEO issues',
        'Fix my SEO issues', 'Analyze content type fields'.
        """;

    /// <inheritdoc />
    public ChatCompletionAgent CreateAgent(Kernel agentKernel)
    {
        // Import WebPageContentRetrievalPlugin exactly like ContentStrategistAgent does.
        // Uses KernelExtensions.ImportPluginFromTypeWithPermissions(kernel, type)
        // which resolves the plugin from DI via ActivatorUtilities.CreateInstance
        // and registers permission-filtered functions into the kernel.
        var pluginType = Type.GetType(WebPagePluginTypeName);
        if (pluginType is not null)
        {
            KenticoKernelExtensions.ImportPluginFromTypeWithPermissions(agentKernel, pluginType);
            logger.LogInformation("AIRA SEO: Imported WebPageContentRetrievalPlugin into kernel");
        }
        else
        {
            logger.LogWarning(
                "AIRA SEO: Could not resolve {TypeName} — current page detection unavailable",
                WebPagePluginTypeName);
        }

        // URL-based fallback for explicit URL analysis
        agentKernel.Plugins.AddFromObject(
            new SeoPageFetchPlugin(httpClientFactory, logger), "SeoTools");

        // SEO field update plugin — enables auto-fix of title/description
        agentKernel.Plugins.AddFromObject(
            new SeoFieldUpdatePlugin(webPageManagerFactory, serviceProvider, logger),
            "SeoFieldUpdate");
        logger.LogInformation("AIRA SEO: Registered SeoFieldUpdatePlugin for auto-fix");

        // Page structure plugin — deep CMS-level analysis of content type, widgets, sections
        agentKernel.Plugins.AddFromObject(
            new SeoPageStructurePlugin(logger),
            "SeoPageStructure");
        logger.LogInformation("AIRA SEO: Registered SeoPageStructurePlugin for page builder analysis");

        return new ChatCompletionAgent
        {
            Name = Name,
            Instructions = Instructions,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
            {
                ServiceId = "Aira",
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 0.3,
                MaxTokens = 4000
            }),
            Kernel = agentKernel
        };
    }
}

/// <summary>
/// Semantic Kernel plugin providing URL-based page fetch for SEO analysis.
/// Used as a fallback when the user provides an explicit URL or when
/// <c>WebPageContentRetrievalPlugin.GetCurrentPageMarkup</c> is unavailable.
/// </summary>
internal sealed class SeoPageFetchPlugin(
    IHttpClientFactory httpClientFactory,
    ILogger logger)
{
    /// <summary>
    /// Fetches the HTML content of a web page for SEO analysis.
    /// Returns the raw HTML which the agent then analyzes.
    /// </summary>
    [KernelFunction("fetch_page_content")]
    [Description("Fetches the full rendered HTML of a URL for SEO analysis. " +
                 "Use this ONLY when the user provides a specific URL or when " +
                 "webpage_get_markup is not available or returned an error. " +
                 "Returns raw HTML including meta tags, headings, images, links, and structured data.")]
    public async Task<string> FetchPageContentAsync(
        [Description("The full URL of the page to analyze (e.g. https://example.com/page)")] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Error: URL is required. Try calling webpage_get_markup instead.";
        }

        try
        {
            using var client = httpClientFactory.CreateClient("SeoAnalyzer");
            using var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }

            string html = await response.Content.ReadAsStringAsync();

            const int maxLength = 50_000;
            if (html.Length > maxLength)
            {
                html = html[..maxLength] + "\n<!-- [truncated] -->";
            }

            return html;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch {Url} for SEO analysis", url);
            return $"Error fetching page: {ex.Message}";
        }
    }
}

/// <summary>
/// Semantic Kernel plugin that updates SEO meta fields (<c>MetaData_Title</c>,
/// <c>MetaData_Description</c>) on the currently open web page via
/// <see cref="IWebPageManagerFactory"/>. Creates a draft with the new values;
/// the user must review and publish manually.
/// </summary>
internal sealed partial class SeoFieldUpdatePlugin(
    IWebPageManagerFactory webPageManagerFactory,
    IServiceProvider serviceProvider,
    ILogger logger)
{
    /// <summary>
    /// Regex matching the admin URL path to extract page context.
    /// Mirrors <c>WebPageContentRetrievalPlugin.WebPageUrlPattern</c>.
    /// </summary>
    [GeneratedRegex(
        @"^/webpages-(?<websiteChannelID>\d+)/(?<languageName>[a-zA-Z0-9_.\-]+)_(?<webPageItemID>\d+)(/.*$|$)",
        RegexOptions.Compiled)]
    private static partial Regex WebPageUrlPattern();

    private const string MetaTitleField = "MetaData_Title";
    private const string MetaDescriptionField = "MetaData_Description";
    private const string MetaKeywordsField = "MetaData_Keywords";

    /// <summary>
    /// Updates SEO meta fields on the currently open web page.
    /// Creates a draft with the provided values — omit a parameter to leave it unchanged.
    /// </summary>
    [KernelFunction("update_seo_fields")]
    [Description("Updates SEO meta fields (title, description, and/or keywords) on a web page. " +
                 "Creates a draft with the new values — the user must review and publish. " +
                 "Pass ONLY the fields you want to change; omit fields to keep current values. " +
                 "Works automatically when viewing a page in admin, or pass explicit websiteChannelId, " +
                 "pageId, and language parameters. " +
                 "Use AFTER analyzing the page when the user asks to 'fix' SEO issues.")]
    public async Task<string> UpdateSeoFieldsAsync(
        [Description("New meta title (30-60 chars recommended). Omit to keep current.")] string? metaTitle = null,
        [Description("New meta description (120-160 chars recommended). Omit to keep current.")] string? metaDescription = null,
        [Description("New meta keywords (comma-separated). Omit to keep current.")] string? metaKeywords = null,
        [Description("Website channel ID. Omit to auto-detect from admin context.")] int? websiteChannelId = null,
        [Description("Web page item ID. Omit to auto-detect from admin context.")] int? pageId = null,
        [Description("Language code (e.g. 'en'). Omit to auto-detect.")] string? language = null)
    {
        if (string.IsNullOrWhiteSpace(metaTitle) && string.IsNullOrWhiteSpace(metaDescription) && string.IsNullOrWhiteSpace(metaKeywords))
        {
            return "Error: Provide at least one field to update (metaTitle, metaDescription, or metaKeywords).";
        }

        try
        {
            int resolvedChannelId;
            int resolvedPageId;
            string resolvedLanguage;

            if (websiteChannelId.HasValue && pageId.HasValue && !string.IsNullOrWhiteSpace(language))
            {
                // Explicit parameters — no admin context needed
                resolvedChannelId = websiteChannelId.Value;
                resolvedPageId = pageId.Value;
                resolvedLanguage = language;
            }
            else
            {
                // AiraChatActionContext is internal — access via reflection
                string? currentPath = GetCurrentUrlPathViaReflection();
                if (string.IsNullOrWhiteSpace(currentPath))
                {
                    return "Error: No page is currently open and no explicit pageId was provided. " +
                        "Either navigate to a page in the admin content tree, or pass " +
                        "websiteChannelId, pageId, and language parameters explicitly.";
                }

                var match = WebPageUrlPattern().Match(currentPath);
                if (!match.Success)
                {
                    return $"Error: Not viewing a web page. Current path: {currentPath}. " +
                        "Pass websiteChannelId, pageId, and language parameters explicitly.";
                }

                if (!int.TryParse(match.Groups["websiteChannelID"].Value, out resolvedChannelId))
                {
                    return $"Error: Could not parse website channel ID from URL: {currentPath}";
                }

                if (!int.TryParse(match.Groups["webPageItemID"].Value, out resolvedPageId))
                {
                    return $"Error: Could not parse web page ID from URL: {currentPath}";
                }

                resolvedLanguage = language ?? match.Groups["languageName"].Value;
            }

            // Override with explicit params when partially provided
            if (websiteChannelId.HasValue) resolvedChannelId = websiteChannelId.Value;
            if (pageId.HasValue) resolvedPageId = pageId.Value;
            if (!string.IsNullOrWhiteSpace(language)) resolvedLanguage = language!;

            int webPageItemId = resolvedPageId;
            int websiteChId = resolvedChannelId;
            string languageName = resolvedLanguage;

            // Build update data with only the fields being changed
            var fields = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(metaTitle))
            {
                fields[MetaTitleField] = metaTitle;
            }
            if (!string.IsNullOrWhiteSpace(metaDescription))
            {
                fields[MetaDescriptionField] = metaDescription;
            }
            if (!string.IsNullOrWhiteSpace(metaKeywords))
            {
                fields[MetaKeywordsField] = metaKeywords;
            }

            // Resolve the current admin user — IAuthenticatedUserAccessor is scoped,
            // so we must create a scope to resolve it (agent is singleton)
            using var scope = serviceProvider.CreateScope();
            var userAccessor = scope.ServiceProvider.GetService<IAuthenticatedUserAccessor>();
            var adminUser = userAccessor is not null ? await userAccessor.Get() : null;
            if (adminUser is null)
            {
                return "Error: Could not determine the current admin user. Ensure you are logged in.";
            }

            int userId = adminUser.UserID;
            logger.LogDebug("AIRA SEO: Resolved admin user {UserId} for draft update", userId);

            // Use IWebPageManagerFactory — web pages require this instead of IContentItemManager
            var webPageManager = webPageManagerFactory.Create(websiteChId, userId);

            // Create or ensure a draft exists
            await webPageManager.TryCreateDraft(webPageItemId, languageName);

            // Apply the field updates via UpdateDraftData
            var contentItemData = new ContentItemData(fields);
            var updateDraftData = new UpdateDraftData(contentItemData);
            bool updated = await webPageManager.TryUpdateDraft(webPageItemId, languageName, updateDraftData);

            if (!updated)
            {
                return "Error: Failed to update draft. Ensure the page content type includes the Base.Metadata schema.";
            }

            // Build success message
            var changes = new List<string>();
            if (!string.IsNullOrWhiteSpace(metaTitle))
            {
                changes.Add($"Title → \"{metaTitle}\" ({metaTitle.Length} chars)");
            }
            if (!string.IsNullOrWhiteSpace(metaDescription))
            {
                changes.Add($"Description → \"{metaDescription}\" ({metaDescription.Length} chars)");
            }
            if (!string.IsNullOrWhiteSpace(metaKeywords))
            {
                changes.Add($"Keywords → \"{metaKeywords}\"");
            }

            logger.LogInformation(
                "AIRA SEO: Updated SEO fields on web page {WebPageItemId} ({Language}): {Changes}",
                webPageItemId, languageName, string.Join(", ", changes));

            return $"Draft updated successfully:\n{string.Join("\n", changes)}\n\n" +
                   "⚠ Review the changes and publish when ready.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA SEO: Failed to update SEO fields");
            return $"Error updating fields: {ex.Message}";
        }
    }

    /// <summary>
    /// Accesses <c>AiraChatActionContext.CurrentUrlPath</c> via reflection
    /// because that type is internal to <c>Kentico.Xperience.Admin.Base</c>.
    /// </summary>
    private static string? GetCurrentUrlPathViaReflection()
    {
        try
        {
            var contextType = Type.GetType(
                "Kentico.Xperience.Admin.Base.AiraChatActionContext, Kentico.Xperience.Admin.Base");

            return contextType?
                .GetProperty("CurrentUrlPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
                .GetValue(null) as string;
        }
        catch
        {
            // ContextContainer<AiraChatActionContext>.Current can be null,
            // causing NRE inside the property getter. Return null to let the
            // caller handle "no page open" gracefully.
            return null;
        }
    }
}
