using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helpers for SEO-related meta tags and elements.
/// </summary>

#region OpenSearch Tag Helper

/// <summary>
/// Renders an OpenSearch link tag for browser search integration.
/// </summary>
/// <example>
/// <code>
/// &lt;open-search-link title="Search My Site" path="/opensearch.xml" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("open-search-link", TagStructure = TagStructure.WithoutEndTag)]
public sealed class OpenSearchLinkTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    /// <summary>
    /// The search provider title.
    /// </summary>
    [HtmlAttributeName("title")]
    public string Title { get; set; } = "Search";

    /// <summary>
    /// Path to the OpenSearch XML file.
    /// </summary>
    [HtmlAttributeName("path")]
    public string Path { get; set; } = "/opensearch.xml";

    /// <summary>
    /// Whether the OpenSearch link is enabled.
    /// </summary>
    [HtmlAttributeName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(Title))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "link";
        output.TagMode = TagMode.SelfClosing;

        var request = httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is not null ? $"{request.Scheme}://{request.Host}" : string.Empty;

        output.Attributes.SetAttribute("rel", "search");
        output.Attributes.SetAttribute("type", "application/opensearchdescription+xml");
        output.Attributes.SetAttribute("title", Title);
        output.Attributes.SetAttribute("href", $"{baseUrl}{Path}");
    }
}

#endregion

#region Theme Color Tag Helper

/// <summary>
/// Renders a theme-color meta tag for browser UI styling.
/// </summary>
/// <example>
/// <code>
/// &lt;theme-color color="#007bff" /&gt;
/// &lt;theme-color color="#1a1a2e" media="(prefers-color-scheme: dark)" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("theme-color", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ThemeColorTagHelper : TagHelper
{
    /// <summary>
    /// The theme color value (hex, rgb, or named color).
    /// </summary>
    [HtmlAttributeName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Optional media query for color scheme.
    /// </summary>
    [HtmlAttributeName("media")]
    public string? Media { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Color))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "meta";
        output.TagMode = TagMode.SelfClosing;

        output.Attributes.SetAttribute("name", "theme-color");
        output.Attributes.SetAttribute("content", Color);

        if (!string.IsNullOrWhiteSpace(Media))
        {
            output.Attributes.SetAttribute("media", Media);
        }
    }
}

#endregion

#region Viewport Meta Tag Helper

/// <summary>
/// Renders a viewport meta tag with common presets.
/// </summary>
/// <example>
/// <code>
/// &lt;viewport-meta /&gt;
/// &lt;viewport-meta user-scalable="false" maximum-scale="1" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("viewport-meta", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ViewportMetaTagHelper : TagHelper
{
    /// <summary>
    /// Use responsive viewport (width=device-width, initial-scale=1).
    /// </summary>
    [HtmlAttributeName("responsive")]
    public bool Responsive { get; set; } = true;

    /// <summary>
    /// Allow user to scale the page.
    /// </summary>
    [HtmlAttributeName("user-scalable")]
    public bool UserScalable { get; set; } = true;

    /// <summary>
    /// Minimum scale factor.
    /// </summary>
    [HtmlAttributeName("minimum-scale")]
    public double MinimumScale { get; set; } = 1.0;

    /// <summary>
    /// Maximum scale factor.
    /// </summary>
    [HtmlAttributeName("maximum-scale")]
    public double MaximumScale { get; set; } = 5.0;

    /// <summary>
    /// Custom viewport content (overrides other settings).
    /// </summary>
    [HtmlAttributeName("content")]
    public string? CustomContent { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "meta";
        output.TagMode = TagMode.SelfClosing;

        output.Attributes.SetAttribute("name", "viewport");

        if (!string.IsNullOrWhiteSpace(CustomContent))
        {
            output.Attributes.SetAttribute("content", CustomContent);
            return;
        }

        var parts = new List<string>();

        if (Responsive)
        {
            parts.Add("width=device-width");
            parts.Add("initial-scale=1");
        }

        if (!UserScalable)
        {
            parts.Add("user-scalable=no");
        }

        if (MinimumScale != 1.0)
        {
            parts.Add($"minimum-scale={MinimumScale:F1}");
        }

        if (MaximumScale != 5.0)
        {
            parts.Add($"maximum-scale={MaximumScale:F1}");
        }

        output.Attributes.SetAttribute("content", string.Join(", ", parts));
    }
}

#endregion

#region Apple Web App Tag Helper

/// <summary>
/// Renders Apple-specific meta tags for web app capabilities.
/// </summary>
/// <example>
/// <code>
/// &lt;apple-web-app title="My App" status-bar-style="black-translucent" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("apple-web-app", TagStructure = TagStructure.WithoutEndTag)]
public sealed class AppleWebAppTagHelper : TagHelper
{
    /// <summary>
    /// The app title.
    /// </summary>
    [HtmlAttributeName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Status bar style (default, black, black-translucent).
    /// </summary>
    [HtmlAttributeName("status-bar-style")]
    public string StatusBarStyle { get; set; } = "default";

    /// <summary>
    /// Whether the app supports standalone mode.
    /// </summary>
    [HtmlAttributeName("capable")]
    public bool Capable { get; set; } = true;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        if (Capable)
        {
            output.Content.AppendHtml("""<meta name="apple-mobile-web-app-capable" content="yes" />""");
            output.Content.AppendHtml(Environment.NewLine);
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            output.Content.AppendHtml($"""<meta name="apple-mobile-web-app-title" content="{Title}" />""");
            output.Content.AppendHtml(Environment.NewLine);
        }

        output.Content.AppendHtml($"""<meta name="apple-mobile-web-app-status-bar-style" content="{StatusBarStyle}" />""");
    }
}

#endregion

#region Canonical Link Tag Helper

/// <summary>
/// Renders a canonical link element.
/// </summary>
/// <example>
/// <code>
/// &lt;canonical-link url="https://example.com/page" /&gt;
/// &lt;canonical-link /&gt; &lt;!-- Uses current URL --&gt;
/// </code>
/// </example>
[HtmlTargetElement("canonical-link", TagStructure = TagStructure.WithoutEndTag)]
public sealed class CanonicalLinkTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    /// <summary>
    /// The canonical URL. If not specified, uses the current request URL.
    /// </summary>
    [HtmlAttributeName("url")]
    public string? Url { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var canonicalUrl = Url;

        if (string.IsNullOrWhiteSpace(canonicalUrl))
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null)
            {
                canonicalUrl = $"{request.Scheme}://{request.Host}{request.Path}";
            }
        }

        if (string.IsNullOrWhiteSpace(canonicalUrl))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "link";
        output.TagMode = TagMode.SelfClosing;

        output.Attributes.SetAttribute("rel", "canonical");
        output.Attributes.SetAttribute("href", canonicalUrl);
    }
}

#endregion

#region Robots Meta Tag Helper

/// <summary>
/// Renders a robots meta tag.
/// </summary>
/// <example>
/// <code>
/// &lt;robots-meta index="true" follow="true" /&gt;
/// &lt;robots-meta no-index no-follow /&gt;
/// </code>
/// </example>
[HtmlTargetElement("robots-meta", TagStructure = TagStructure.WithoutEndTag)]
public sealed class RobotsMetaTagHelper : TagHelper
{
    /// <summary>
    /// Allow indexing of this page.
    /// </summary>
    [HtmlAttributeName("index")]
    public bool Index { get; set; } = true;

    /// <summary>
    /// Convenience attribute to set noindex.
    /// </summary>
    [HtmlAttributeName("no-index")]
    public bool NoIndex { get; set; }

    /// <summary>
    /// Allow following links on this page.
    /// </summary>
    [HtmlAttributeName("follow")]
    public bool Follow { get; set; } = true;

    /// <summary>
    /// Convenience attribute to set nofollow.
    /// </summary>
    [HtmlAttributeName("no-follow")]
    public bool NoFollow { get; set; }

    /// <summary>
    /// Prevent caching.
    /// </summary>
    [HtmlAttributeName("no-cache")]
    public bool NoCache { get; set; }

    /// <summary>
    /// Prevent archiving.
    /// </summary>
    [HtmlAttributeName("no-archive")]
    public bool NoArchive { get; set; }

    /// <summary>
    /// Prevent snippet display.
    /// </summary>
    [HtmlAttributeName("no-snippet")]
    public bool NoSnippet { get; set; }

    /// <summary>
    /// Maximum snippet length (-1 for unlimited).
    /// </summary>
    [HtmlAttributeName("max-snippet")]
    public int? MaxSnippet { get; set; }

    /// <summary>
    /// Maximum image preview size (none, standard, large).
    /// </summary>
    [HtmlAttributeName("max-image-preview")]
    public string? MaxImagePreview { get; set; }

    /// <summary>
    /// Maximum video preview duration in seconds.
    /// </summary>
    [HtmlAttributeName("max-video-preview")]
    public int? MaxVideoPreview { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "meta";
        output.TagMode = TagMode.SelfClosing;

        var directives = new List<string>();

        // Handle index/noindex
        if (NoIndex || !Index)
        {
            directives.Add("noindex");
        }
        else
        {
            directives.Add("index");
        }

        // Handle follow/nofollow
        if (NoFollow || !Follow)
        {
            directives.Add("nofollow");
        }
        else
        {
            directives.Add("follow");
        }

        if (NoCache) directives.Add("noarchive");
        if (NoArchive) directives.Add("noarchive");
        if (NoSnippet) directives.Add("nosnippet");

        if (MaxSnippet.HasValue)
        {
            directives.Add($"max-snippet:{MaxSnippet.Value}");
        }

        if (!string.IsNullOrWhiteSpace(MaxImagePreview))
        {
            directives.Add($"max-image-preview:{MaxImagePreview}");
        }

        if (MaxVideoPreview.HasValue)
        {
            directives.Add($"max-video-preview:{MaxVideoPreview.Value}");
        }

        output.Attributes.SetAttribute("name", "robots");
        output.Attributes.SetAttribute("content", string.Join(", ", directives));
    }
}

#endregion

