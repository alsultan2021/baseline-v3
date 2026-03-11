using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helper for generating resource hint link elements (preload, prefetch, preconnect, dns-prefetch).
/// </summary>
/// <example>
/// <code>
/// &lt;resource-preload href="/fonts/main.woff2" as="font" crossorigin /&gt;
/// &lt;resource-prefetch href="/images/hero.webp" as="image" /&gt;
/// &lt;resource-preconnect href="https://cdn.example.com" /&gt;
/// &lt;resource-dns-prefetch href="https://analytics.example.com" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("resource-preload", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("resource-prefetch", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("resource-preconnect", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("resource-dns-prefetch", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ResourceHintsTagHelper : TagHelper
{
    /// <summary>
    /// The resource URL.
    /// </summary>
    [HtmlAttributeName("href")]
    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// The resource type (font, image, script, style, fetch, etc.).
    /// Required for preload.
    /// </summary>
    [HtmlAttributeName("as")]
    public string? As { get; set; }

    /// <summary>
    /// MIME type of the resource.
    /// </summary>
    [HtmlAttributeName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether the resource requires CORS.
    /// </summary>
    [HtmlAttributeName("crossorigin")]
    public bool Crossorigin { get; set; }

    /// <summary>
    /// Media query for conditional loading.
    /// </summary>
    [HtmlAttributeName("media")]
    public string? Media { get; set; }

    /// <summary>
    /// Fetch priority (high, low, auto).
    /// </summary>
    [HtmlAttributeName("fetchpriority")]
    public string? FetchPriority { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Href))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "link";
        output.TagMode = TagMode.SelfClosing;

        var rel = context.TagName switch
        {
            "resource-preload" => "preload",
            "resource-prefetch" => "prefetch",
            "resource-preconnect" => "preconnect",
            "resource-dns-prefetch" => "dns-prefetch",
            _ => "preload"
        };

        output.Attributes.SetAttribute("rel", rel);
        output.Attributes.SetAttribute("href", Href);

        if (!string.IsNullOrWhiteSpace(As))
        {
            output.Attributes.SetAttribute("as", As);
        }

        if (!string.IsNullOrWhiteSpace(Type))
        {
            output.Attributes.SetAttribute("type", Type);
        }

        if (Crossorigin)
        {
            output.Attributes.SetAttribute("crossorigin", "anonymous");
        }

        if (!string.IsNullOrWhiteSpace(Media))
        {
            output.Attributes.SetAttribute("media", Media);
        }

        if (!string.IsNullOrWhiteSpace(FetchPriority))
        {
            output.Attributes.SetAttribute("fetchpriority", FetchPriority);
        }
    }
}

/// <summary>
/// Tag helper for generating multiple resource hints from a collection.
/// </summary>
/// <example>
/// <code>
/// &lt;resource-hints&gt;
///     &lt;preload href="/fonts/main.woff2" as="font" /&gt;
///     &lt;preconnect href="https://cdn.example.com" /&gt;
/// &lt;/resource-hints&gt;
/// </code>
/// </example>
[HtmlTargetElement("resource-hints")]
public sealed class ResourceHintsContainerTagHelper : TagHelper
{
    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Remove wrapper, keep children
    }
}

