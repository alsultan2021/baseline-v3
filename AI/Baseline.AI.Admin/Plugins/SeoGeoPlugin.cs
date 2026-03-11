using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using Kentico.Xperience.Admin.Base.Authentication;

namespace Baseline.AI.Admin.Plugins;

/// <summary>
/// AIRA plugin providing combined SEO + GEO (Generative Engine Optimization) analysis.
/// Analyzes pages for both traditional search engine and AI platform discoverability,
/// and offers one-click fixes for metadata, headings, structured data, and content clarity.
/// </summary>
/// <remarks>
/// Complements <c>AiraSeoAnalyzerAgent</c> (specialized agent with deep page builder
/// analysis). This plugin provides lighter-weight, tool-callable analysis accessible
/// from any AIRA conversation without routing to the SEO agent.
/// </remarks>
[Description("Analyzes pages for SEO and GEO (AI engine optimization), provides scores, " +
             "recommendations, and one-click metadata fixes.")]
public sealed partial class SeoGeoPlugin(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<SeoGeoPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "SeoGeo";

    private const int MaxHtmlLength = 80_000;

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleTagRegex();

    [GeneratedRegex(@"<meta\s[^>]*name\s*=\s*""description""[^>]*content\s*=\s*""([^""]*)""",
        RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex(@"<meta\s[^>]*content\s*=\s*""([^""]*)""[^>]*name\s*=\s*""description""",
        RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescriptionAltRegex();

    [GeneratedRegex(@"<(h[1-6])[^>]*>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<script\s[^>]*type\s*=\s*""application/ld\+json""[^>]*>(.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex StructuredDataRegex();

    [GeneratedRegex(@"<img\s[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();

    [GeneratedRegex(@"alt\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex AltAttrRegex();

    [GeneratedRegex(@"<meta\s[^>]*property\s*=\s*""og:([^""]*)""[^>]*content\s*=\s*""([^""]*)""",
        RegexOptions.IgnoreCase)]
    private static partial Regex OgTagRegex();

    [GeneratedRegex(@"<link\s[^>]*rel\s*=\s*""canonical""[^>]*href\s*=\s*""([^""]*)""",
        RegexOptions.IgnoreCase)]
    private static partial Regex CanonicalRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    // ──────────────────────────────────────────────────────────────
    //  Combined SEO + GEO analysis
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Performs a combined SEO + GEO analysis on a URL, returning a scored report
    /// with actionable recommendations for both search engines and AI platforms.
    /// </summary>
    [KernelFunction("analyze_seo_geo")]
    [Description("Analyzes a URL for both SEO (search engine optimization) and GEO (generative " +
                 "engine optimization / AI discoverability). Returns a scored report with " +
                 "actionable recommendations and severity levels. Covers: title, meta description, " +
                 "headings, content clarity, structured data, images, Open Graph, canonical, " +
                 "and AI-readability factors.")]
    public async Task<string> AnalyzeSeoGeoAsync(
        [Description("Full URL of the page to analyze (e.g. https://chevalroyal.com/blog/post)")] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Error: Provide a URL to analyze.";
        }

        try
        {
            string html = await FetchHtmlAsync(url);
            if (html.StartsWith("Error:"))
            {
                return html;
            }

            var analysis = AnalyzeHtml(html, url);
            return FormatReport(analysis, url);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SeoGeo: analyze failed for {Url}", url);
            return $"Error analyzing page: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  One-click SEO field fix
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the SEO metadata fields on a web page draft.
    /// </summary>
    [KernelFunction("fix_seo_fields")]
    [Description("Updates SEO metadata on a web page in the CMS. Creates/updates a draft with " +
                 "improved meta title, description, and/or keywords. The user must review and " +
                 "publish. Pass ONLY fields you want to change.")]
    public async Task<string> FixSeoFieldsAsync(
        [Description("Web page item ID (from CMS)")] int webPageItemId,
        [Description("Website channel ID")] int websiteChannelId,
        [Description("Language code (e.g. en)")] string languageName,
        [Description("Improved meta title (30-60 chars). Omit to keep current.")] string? metaTitle = null,
        [Description("Improved meta description (120-160 chars). Omit to keep current.")] string? metaDescription = null,
        [Description("Meta keywords (comma-separated). Omit to keep current.")] string? metaKeywords = null)
    {
        if (string.IsNullOrWhiteSpace(metaTitle) &&
            string.IsNullOrWhiteSpace(metaDescription) &&
            string.IsNullOrWhiteSpace(metaKeywords))
        {
            return "Error: Provide at least one field to update.";
        }

        try
        {
            using var scope = serviceProvider.CreateScope();

            var userAccessor = scope.ServiceProvider.GetService<IAuthenticatedUserAccessor>();
            var adminUser = userAccessor is not null ? await userAccessor.Get() : null;
            if (adminUser is null)
            {
                return "Error: Could not resolve admin user. Ensure you are logged in.";
            }

            var webPageManagerFactory = scope.ServiceProvider
                .GetRequiredService<IWebPageManagerFactory>();
            var manager = webPageManagerFactory.Create(websiteChannelId, adminUser.UserID);

            var fields = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(metaTitle))
            {
                fields["MetaData_Title"] = metaTitle;
            }

            if (!string.IsNullOrWhiteSpace(metaDescription))
            {
                fields["MetaData_Description"] = metaDescription;
            }

            if (!string.IsNullOrWhiteSpace(metaKeywords))
            {
                fields["MetaData_Keywords"] = metaKeywords;
            }

            await manager.TryCreateDraft(webPageItemId, languageName);

            var data = new ContentItemData(fields);
            var updateData = new UpdateDraftData(data);
            bool ok = await manager.TryUpdateDraft(webPageItemId, languageName, updateData);

            if (!ok)
            {
                return "Error: Failed to update draft. Ensure the page uses the Base.Metadata schema.";
            }

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

            logger.LogInformation("SeoGeo: Fixed SEO fields on page {Id}: {Changes}",
                webPageItemId, string.Join(", ", changes));

            return $"Draft updated:\n{string.Join("\n", changes)}\n\n⚠ Review and publish when ready.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SeoGeo: fix_seo_fields failed for page {Id}", webPageItemId);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  GEO-specific analysis
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates how well a page is optimized for AI-powered discovery systems (GEO).
    /// </summary>
    [KernelFunction("analyze_geo")]
    [Description("Evaluates page content for Generative Engine Optimization (GEO) — how well " +
                 "AI systems like ChatGPT, Perplexity, and Google AI Overviews can understand, " +
                 "cite, and reference the content. Checks: structured data quality, content " +
                 "clarity, direct answer patterns, entity markup, FAQ structure, and " +
                 "authoritative sourcing.")]
    public async Task<string> AnalyzeGeoAsync(
        [Description("Full URL of the page to analyze")] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Error: Provide a URL to analyze.";
        }

        try
        {
            string html = await FetchHtmlAsync(url);
            if (html.StartsWith("Error:"))
            {
                return html;
            }

            return FormatGeoReport(html, url);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SeoGeo: GEO analysis failed for {Url}", url);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Bulk audit
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Audits multiple web pages for SEO/GEO issues and returns a summary table.
    /// </summary>
    [KernelFunction("bulk_seo_audit")]
    [Description("Audits multiple URLs for SEO/GEO issues. Returns a summary table with scores " +
                 "and top issues per page. Provide up to 10 URLs.")]
    public async Task<string> BulkSeoAuditAsync(
        [Description("Comma-separated URLs to audit (max 10)")] string urls)
    {
        if (string.IsNullOrWhiteSpace(urls))
        {
            return "Error: Provide comma-separated URLs.";
        }

        var urlList = urls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(10)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("## Bulk SEO/GEO Audit");
        sb.AppendLine();
        sb.AppendLine("| Page | SEO Score | GEO Score | Top Issue |");
        sb.AppendLine("|------|-----------|-----------|-----------|");

        foreach (string pageUrl in urlList)
        {
            try
            {
                string html = await FetchHtmlAsync(pageUrl);
                if (html.StartsWith("Error:"))
                {
                    sb.AppendLine($"| {TruncateUrl(pageUrl)} | — | — | Fetch error |");
                    continue;
                }

                var analysis = AnalyzeHtml(html, pageUrl);
                int geoScore = CalculateGeoScore(html);
                string topIssue = analysis.Issues.Count > 0 ? analysis.Issues[0].Message : "None";

                sb.AppendLine($"| {TruncateUrl(pageUrl)} | {analysis.SeoScore}/100 | {geoScore}/100 | {topIssue} |");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"| {TruncateUrl(pageUrl)} | — | — | {ex.Message} |");
            }
        }

        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════
    //  Private helpers
    // ══════════════════════════════════════════════════════════════

    private async Task<string> FetchHtmlAsync(string url)
    {
        using var client = httpClientFactory.CreateClient("AiraWebFetch");

        try
        {
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"Error: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }

            string html = await response.Content.ReadAsStringAsync();
            return html.Length > MaxHtmlLength ? html[..MaxHtmlLength] : html;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private SeoGeoAnalysis AnalyzeHtml(string html, string url)
    {
        var analysis = new SeoGeoAnalysis { Url = url };

        // Title
        var titleMatch = TitleTagRegex().Match(html);
        analysis.Title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : null;

        // Meta description
        var descMatch = MetaDescriptionRegex().Match(html);
        if (!descMatch.Success)
        {
            descMatch = MetaDescriptionAltRegex().Match(html);
        }

        analysis.MetaDescription = descMatch.Success ? descMatch.Groups[1].Value.Trim() : null;

        // Headings
        foreach (Match m in HeadingRegex().Matches(html))
        {
            string level = m.Groups[1].Value.ToUpperInvariant();
            string text = HtmlTagRegex().Replace(m.Groups[2].Value, "").Trim();
            analysis.Headings.Add((level, text));
        }

        // Structured data
        foreach (Match m in StructuredDataRegex().Matches(html))
        {
            analysis.StructuredDataBlocks.Add(m.Groups[1].Value.Trim());
        }

        // Images
        foreach (Match m in ImgTagRegex().Matches(html))
        {
            var altMatch = AltAttrRegex().Match(m.Value);
            bool hasAlt = altMatch.Success && !string.IsNullOrWhiteSpace(altMatch.Groups[1].Value);
            analysis.Images.Add((m.Value, hasAlt));
        }

        // Open Graph
        foreach (Match m in OgTagRegex().Matches(html))
        {
            analysis.OgTags[m.Groups[1].Value] = m.Groups[2].Value;
        }

        // Canonical
        var canonicalMatch = CanonicalRegex().Match(html);
        analysis.Canonical = canonicalMatch.Success ? canonicalMatch.Groups[1].Value : null;

        // Content text
        string bodyText = ExtractBodyText(html);
        analysis.WordCount = bodyText.Split([' ', '\n', '\r', '\t'],
            StringSplitOptions.RemoveEmptyEntries).Length;

        // Score
        CalculateSeoScore(analysis);
        analysis.GeoScore = CalculateGeoScore(html);

        return analysis;
    }

    private static void CalculateSeoScore(SeoGeoAnalysis a)
    {
        int score = 100;
        var issues = a.Issues;

        // Title (15 pts)
        if (string.IsNullOrEmpty(a.Title))
        {
            score -= 15;
            issues.Add(new("Critical", "Missing <title> tag"));
        }
        else if (a.Title.Length < 30 || a.Title.Length > 60)
        {
            score -= 8;
            issues.Add(new("Major", $"Title length {a.Title.Length} chars (optimal: 30-60)"));
        }

        // Meta description (10 pts)
        if (string.IsNullOrEmpty(a.MetaDescription))
        {
            score -= 10;
            issues.Add(new("Critical", "Missing meta description"));
        }
        else if (a.MetaDescription.Length < 120 || a.MetaDescription.Length > 160)
        {
            score -= 5;
            issues.Add(new("Minor", $"Meta description {a.MetaDescription.Length} chars (optimal: 120-160)"));
        }

        // Headings (15 pts)
        int h1Count = a.Headings.Count(h => h.Level == "H1");
        if (h1Count == 0)
        {
            score -= 15;
            issues.Add(new("Critical", "No H1 heading found"));
        }
        else if (h1Count > 1)
        {
            score -= 8;
            issues.Add(new("Major", $"Multiple H1 headings ({h1Count})"));
        }

        // Check heading hierarchy
        var levels = a.Headings.Select(h => int.Parse(h.Level[1..])).ToList();
        for (int i = 1; i < levels.Count; i++)
        {
            if (levels[i] > levels[i - 1] + 1)
            {
                score -= 3;
                issues.Add(new("Minor", $"Heading skip: H{levels[i - 1]} → H{levels[i]}"));
                break;
            }
        }

        // Content (15 pts)
        if (a.WordCount < 100)
        {
            score -= 15;
            issues.Add(new("Critical", $"Thin content: only {a.WordCount} words"));
        }
        else if (a.WordCount < 300)
        {
            score -= 5;
            issues.Add(new("Minor", $"Short content: {a.WordCount} words (aim for 300+)"));
        }

        // Images (10 pts)
        int missingAlt = a.Images.Count(i => !i.HasAlt);
        if (a.Images.Count > 0 && missingAlt > a.Images.Count / 2)
        {
            score -= 8;
            issues.Add(new("Major", $"{missingAlt}/{a.Images.Count} images missing alt text"));
        }
        else if (missingAlt > 0)
        {
            score -= 3;
            issues.Add(new("Minor", $"{missingAlt} image(s) missing alt text"));
        }

        // Structured data (10 pts)
        if (a.StructuredDataBlocks.Count == 0)
        {
            score -= 8;
            issues.Add(new("Major", "No structured data (Schema.org) found"));
        }

        // Open Graph (5 pts)
        if (!a.OgTags.ContainsKey("title") || !a.OgTags.ContainsKey("image"))
        {
            score -= 5;
            issues.Add(new("Major", "Missing og:title or og:image"));
        }

        // Canonical (5 pts)
        if (string.IsNullOrEmpty(a.Canonical))
        {
            score -= 5;
            issues.Add(new("Major", "Missing canonical URL"));
        }

        a.SeoScore = Math.Max(0, score);
    }

    private static int CalculateGeoScore(string html)
    {
        int score = 100;

        // 1. Structured data presence & quality (25 pts)
        var sdBlocks = StructuredDataRegex().Matches(html);
        if (sdBlocks.Count == 0)
        {
            score -= 25;
        }
        else
        {
            // Check for rich types (FAQ, HowTo, Article, Product)
            string sdText = string.Join(" ", sdBlocks.Select(m => m.Groups[1].Value));
            bool hasRichType = sdText.Contains("FAQPage", StringComparison.OrdinalIgnoreCase)
                || sdText.Contains("HowTo", StringComparison.OrdinalIgnoreCase)
                || sdText.Contains("Article", StringComparison.OrdinalIgnoreCase)
                || sdText.Contains("Product", StringComparison.OrdinalIgnoreCase)
                || sdText.Contains("LocalBusiness", StringComparison.OrdinalIgnoreCase);

            if (!hasRichType)
            {
                score -= 10;
            }
        }

        // 2. Direct answer patterns (20 pts) — AI systems prefer concise answers
        string bodyText = ExtractBodyText(html);
        bool hasDefinitions = bodyText.Contains(" is ", StringComparison.OrdinalIgnoreCase)
            && bodyText.Contains(" means ", StringComparison.OrdinalIgnoreCase);
        bool hasLists = html.Contains("<ol", StringComparison.OrdinalIgnoreCase)
            || html.Contains("<ul", StringComparison.OrdinalIgnoreCase);

        if (!hasDefinitions && !hasLists)
        {
            score -= 20;
        }
        else if (!hasDefinitions || !hasLists)
        {
            score -= 10;
        }

        // 3. FAQ structure (15 pts)
        bool hasFaqContent = html.Contains("faq", StringComparison.OrdinalIgnoreCase)
            || (html.Contains("<details", StringComparison.OrdinalIgnoreCase)
                && html.Contains("<summary", StringComparison.OrdinalIgnoreCase));

        if (!hasFaqContent)
        {
            score -= 15;
        }

        // 4. Content clarity & readability (15 pts) — shorter sentences, clear structure
        var sentences = bodyText.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length > 0)
        {
            double avgSentenceLength = sentences.Average(s =>
                s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

            if (avgSentenceLength > 25)
            {
                score -= 15; // Complex, hard for AI to parse
            }
            else if (avgSentenceLength > 20)
            {
                score -= 5;
            }
        }

        // 5. Entity mentions & authority signals (10 pts)
        bool hasCitations = html.Contains("cite", StringComparison.OrdinalIgnoreCase)
            || html.Contains("blockquote", StringComparison.OrdinalIgnoreCase)
            || html.Contains("source", StringComparison.OrdinalIgnoreCase);

        if (!hasCitations)
        {
            score -= 10;
        }

        // 6. Open Graph / social metadata (10 pts) — AI uses these for context
        bool hasOg = html.Contains("og:title", StringComparison.OrdinalIgnoreCase)
            && html.Contains("og:description", StringComparison.OrdinalIgnoreCase);

        if (!hasOg)
        {
            score -= 10;
        }

        // 7. Table/data structure (5 pts) — AI loves structured data tables
        bool hasTables = html.Contains("<table", StringComparison.OrdinalIgnoreCase);
        if (!hasTables)
        {
            score -= 5;
        }

        return Math.Max(0, score);
    }

    private static string ExtractBodyText(string html)
    {
        // Remove script and style blocks
        string noScript = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>",
            "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return HtmlTagRegex().Replace(noScript, " ").Trim();
    }

    private string FormatReport(SeoGeoAnalysis a, string url)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## SEO/GEO Analysis: {TruncateUrl(url)}");
        sb.AppendLine();
        sb.AppendLine($"### SEO Score: **{a.SeoScore}/100** | GEO Score: **{a.GeoScore}/100**");
        sb.AppendLine();

        // Quick stats
        sb.AppendLine("### Page Overview");
        sb.AppendLine($"- **Title**: {a.Title ?? "⚠ MISSING"}");
        sb.AppendLine($"- **Description**: {Truncate(a.MetaDescription, 80) ?? "⚠ MISSING"}");
        sb.AppendLine($"- **Headings**: {a.Headings.Count} ({string.Join(", ", a.Headings.GroupBy(h => h.Level).Select(g => $"{g.Key}×{g.Count()}"))})");
        sb.AppendLine($"- **Word count**: {a.WordCount}");
        sb.AppendLine($"- **Images**: {a.Images.Count} ({a.Images.Count(i => i.HasAlt)} with alt)");
        sb.AppendLine($"- **Structured data**: {a.StructuredDataBlocks.Count} block(s)");
        sb.AppendLine($"- **Canonical**: {a.Canonical ?? "⚠ MISSING"}");
        sb.AppendLine();

        // Issues by severity
        var critical = a.Issues.Where(i => i.Severity == "Critical").ToList();
        var major = a.Issues.Where(i => i.Severity == "Major").ToList();
        var minor = a.Issues.Where(i => i.Severity == "Minor").ToList();

        if (critical.Count > 0)
        {
            sb.AppendLine("### Critical Issues");
            foreach (var issue in critical)
            {
                sb.AppendLine($"- ❌ {issue.Message}");
            }

            sb.AppendLine();
        }

        if (major.Count > 0)
        {
            sb.AppendLine("### Major Issues");
            foreach (var issue in major)
            {
                sb.AppendLine($"- ⚠ {issue.Message}");
            }

            sb.AppendLine();
        }

        if (minor.Count > 0)
        {
            sb.AppendLine("### Minor Issues");
            foreach (var issue in minor)
            {
                sb.AppendLine($"- 💡 {issue.Message}");
            }

            sb.AppendLine();
        }

        // GEO recommendations
        sb.AppendLine("### GEO Recommendations (AI Discoverability)");
        if (a.StructuredDataBlocks.Count == 0)
        {
            sb.AppendLine("- Add **Schema.org** structured data (Article, FAQPage, Product, etc.)");
        }

        if (a.GeoScore < 60)
        {
            sb.AppendLine("- Add **FAQ sections** with clear question/answer patterns");
            sb.AppendLine("- Use **definition patterns** (\"X is...\", \"X means...\")");
            sb.AppendLine("- Add **ordered/unordered lists** for step-by-step content");
            sb.AppendLine("- Include **data tables** for comparative information");
            sb.AppendLine("- Add **citations and sources** for authority signals");
        }

        return sb.ToString();
    }

    private string FormatGeoReport(string html, string url)
    {
        int geoScore = CalculateGeoScore(html);

        var sb = new StringBuilder();
        sb.AppendLine($"## GEO Analysis: {TruncateUrl(url)}");
        sb.AppendLine($"### GEO Score: **{geoScore}/100**");
        sb.AppendLine();

        sb.AppendLine("| Factor | Weight | Status |");
        sb.AppendLine("|--------|--------|--------|");

        // Structured data
        bool hasSd = StructuredDataRegex().IsMatch(html);
        sb.AppendLine($"| Structured data (Schema.org) | 25 pts | {(hasSd ? "✅" : "❌ Missing")} |");

        // Direct answer patterns
        string body = ExtractBodyText(html);
        bool hasDirect = body.Contains(" is ", StringComparison.OrdinalIgnoreCase)
            || body.Contains(" means ", StringComparison.OrdinalIgnoreCase);
        sb.AppendLine($"| Direct answer patterns | 20 pts | {(hasDirect ? "✅" : "❌ No clear definitions")} |");

        // FAQ
        bool hasFaq = html.Contains("faq", StringComparison.OrdinalIgnoreCase);
        sb.AppendLine($"| FAQ / Q&A structure | 15 pts | {(hasFaq ? "✅" : "❌ Missing")} |");

        // Readability
        var sentences = body.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        double avgLen = sentences.Length > 0
            ? sentences.Average(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            : 0;
        sb.AppendLine($"| Content clarity (avg sentence: {avgLen:F0} words) | 15 pts | {(avgLen <= 20 ? "✅" : "⚠ Long sentences")} |");

        // Authority signals
        bool hasCite = html.Contains("cite", StringComparison.OrdinalIgnoreCase)
            || html.Contains("blockquote", StringComparison.OrdinalIgnoreCase);
        sb.AppendLine($"| Authority / citations | 10 pts | {(hasCite ? "✅" : "❌ No citations")} |");

        // OG
        bool hasOg = html.Contains("og:title", StringComparison.OrdinalIgnoreCase);
        sb.AppendLine($"| Open Graph metadata | 10 pts | {(hasOg ? "✅" : "❌ Missing")} |");

        // Tables
        bool tables = html.Contains("<table", StringComparison.OrdinalIgnoreCase);
        sb.AppendLine($"| Data tables | 5 pts | {(tables ? "✅" : "💡 Consider adding")} |");

        sb.AppendLine();
        sb.AppendLine("### Key GEO Strategies");
        sb.AppendLine("1. **Cite sources** — AI models weight content higher when it references authoritative sources");
        sb.AppendLine("2. **Structure for extraction** — Use definition lists, FAQs, and tables that AI can directly cite");
        sb.AppendLine("3. **Provide concise answers** — Lead paragraphs should answer the page's primary question in 1-2 sentences");
        sb.AppendLine("4. **Rich Schema.org** — FAQPage, HowTo, Article schemas help AI understand content purpose");
        sb.AppendLine("5. **Optimize entity coverage** — Mention relevant entities, people, places, and concepts clearly");

        return sb.ToString();
    }

    private static string TruncateUrl(string url) =>
        url.Length > 50 ? string.Concat(url.AsSpan(0, 47), "...") : url;

    private static string? Truncate(string? text, int max) =>
        text is null ? null : text.Length > max ? string.Concat(text.AsSpan(0, max - 3), "...") : text;

    private sealed class SeoGeoAnalysis
    {
        public string Url { get; set; } = "";
        public string? Title { get; set; }
        public string? MetaDescription { get; set; }
        public List<(string Level, string Text)> Headings { get; } = [];
        public List<string> StructuredDataBlocks { get; } = [];
        public List<(string Tag, bool HasAlt)> Images { get; } = [];
        public Dictionary<string, string> OgTags { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? Canonical { get; set; }
        public int WordCount { get; set; }
        public int SeoScore { get; set; }
        public int GeoScore { get; set; }
        public List<SeoIssue> Issues { get; } = [];
    }

    private sealed record SeoIssue(string Severity, string Message);
}
