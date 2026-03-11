using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helper for generating optimized picture elements with multiple sources.
/// Supports WebP, AVIF, and responsive srcset generation.
/// </summary>
/// <example>
/// <code>
/// &lt;optimized-picture src="/images/hero.jpg" alt="Hero Image" /&gt;
/// &lt;optimized-picture src="/images/hero.jpg" alt="Hero" widths="320,640,1024,1920" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("optimized-picture")]
public sealed class OptimizedPictureTagHelper : TagHelper
{
    private static readonly FrozenSet<string> SupportedFormats =
        FrozenSet.ToFrozenSet(["jpg", "jpeg", "png", "gif", "webp"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The image source URL.
    /// </summary>
    [HtmlAttributeName("src")]
    public string Src { get; set; } = string.Empty;

    /// <summary>
    /// Alt text for accessibility. Required.
    /// </summary>
    [HtmlAttributeName("alt")]
    public string Alt { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated widths for srcset generation.
    /// </summary>
    [HtmlAttributeName("widths")]
    public string? Widths { get; set; }

    /// <summary>
    /// Sizes attribute for responsive behavior.
    /// </summary>
    [HtmlAttributeName("sizes")]
    public string? Sizes { get; set; }

    /// <summary>
    /// CSS class for the img element.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    /// <summary>
    /// Loading behavior: lazy (default) or eager.
    /// </summary>
    [HtmlAttributeName("loading")]
    public string Loading { get; set; } = "lazy";

    /// <summary>
    /// Decoding hint: async (default), sync, or auto.
    /// </summary>
    [HtmlAttributeName("decoding")]
    public string Decoding { get; set; } = "async";

    /// <summary>
    /// Fetch priority: high, low, or auto.
    /// </summary>
    [HtmlAttributeName("fetchpriority")]
    public string? FetchPriority { get; set; }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    [HtmlAttributeName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    [HtmlAttributeName("height")]
    public int? Height { get; set; }

    /// <summary>
    /// Enable AVIF format (modern browsers).
    /// </summary>
    [HtmlAttributeName("avif")]
    public bool EnableAvif { get; set; } = false;

    /// <summary>
    /// Enable WebP format. Default is true.
    /// </summary>
    [HtmlAttributeName("webp")]
    public bool EnableWebp { get; set; } = true;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Src))
        {
            output.SuppressOutput();
            return;
        }

        var extension = Path.GetExtension(Src).TrimStart('.').ToLowerInvariant();
        if (!SupportedFormats.Contains(extension))
        {
            // Just render a simple img
            RenderSimpleImage(output);
            return;
        }

        output.TagName = "picture";
        output.TagMode = TagMode.StartTagAndEndTag;

        var widthValues = ParseWidths();

        // AVIF source (best compression, limited support)
        if (EnableAvif && extension is "jpg" or "jpeg" or "png")
        {
            var avifSrcset = GenerateSrcset(Src, extension, "avif", widthValues);
            output.Content.AppendHtml($"""<source type="image/avif" srcset="{avifSrcset}"{GetSizesAttr()} />""");
            output.Content.AppendHtml(Environment.NewLine);
        }

        // WebP source (good compression, wide support)
        if (EnableWebp && extension is "jpg" or "jpeg" or "png")
        {
            var webpSrcset = GenerateSrcset(Src, extension, "webp", widthValues);
            output.Content.AppendHtml($"""<source type="image/webp" srcset="{webpSrcset}"{GetSizesAttr()} />""");
            output.Content.AppendHtml(Environment.NewLine);
        }

        // Original format as fallback
        if (widthValues.Length > 0)
        {
            var originalSrcset = GenerateSrcset(Src, extension, extension, widthValues);
            output.Content.AppendHtml($"""<source type="image/{GetMimeType(extension)}" srcset="{originalSrcset}"{GetSizesAttr()} />""");
            output.Content.AppendHtml(Environment.NewLine);
        }

        // Fallback img element
        output.Content.AppendHtml(BuildImgElement());
    }

    private void RenderSimpleImage(TagHelperOutput output)
    {
        output.TagName = "img";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("src", Src);
        output.Attributes.SetAttribute("alt", Alt);

        if (!string.IsNullOrWhiteSpace(CssClass))
            output.Attributes.SetAttribute("class", CssClass);

        output.Attributes.SetAttribute("loading", Loading);
        output.Attributes.SetAttribute("decoding", Decoding);

        if (Width.HasValue)
            output.Attributes.SetAttribute("width", Width.Value);
        if (Height.HasValue)
            output.Attributes.SetAttribute("height", Height.Value);
    }

    private int[] ParseWidths()
    {
        if (string.IsNullOrWhiteSpace(Widths))
        {
            return [];
        }

        return Widths
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => int.TryParse(w, out var val) ? val : 0)
            .Where(w => w > 0)
            .OrderBy(w => w)
            .ToArray();
    }

    private static string GenerateSrcset(string src, string originalExt, string targetExt, int[] widths)
    {
        if (widths.Length == 0)
        {
            var converted = src.Replace($".{originalExt}", $".{targetExt}", StringComparison.OrdinalIgnoreCase);
            return converted;
        }

        var srcsetParts = widths.Select(w =>
        {
            var basePath = Path.ChangeExtension(src, null);
            return $"{basePath}-{w}w.{targetExt} {w}w";
        });

        return string.Join(", ", srcsetParts);
    }

    private string GetSizesAttr() =>
        string.IsNullOrWhiteSpace(Sizes) ? string.Empty : $""" sizes="{Sizes}" """.TrimEnd();

    private static string GetMimeType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            "jpg" => "jpeg",
            "svg" => "svg+xml",
            _ => extension
        };

    private string BuildImgElement()
    {
        var parts = new List<string>
        {
            $"""src="{Src}" """,
            $"""alt="{Alt}" """,
            $"""loading="{Loading}" """,
            $"""decoding="{Decoding}" """
        };

        if (!string.IsNullOrWhiteSpace(CssClass))
            parts.Add($"""class="{CssClass}" """);

        if (Width.HasValue)
            parts.Add($"""width="{Width}" """);

        if (Height.HasValue)
            parts.Add($"""height="{Height}" """);

        if (!string.IsNullOrWhiteSpace(FetchPriority))
            parts.Add($"""fetchpriority="{FetchPriority}" """);

        return $"<img {string.Join("", parts).TrimEnd()} />";
    }
}

