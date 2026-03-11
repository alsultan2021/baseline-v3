using Microsoft.AspNetCore.Html;
using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Web;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helper that enhances img elements with baseline optimizations.
/// Converts standard img tags to picture elements with WebP and optimized sources.
/// </summary>
/// <example>
/// <code>
/// &lt;img bl-optimize src="/images/source/photo.jpg" alt="Photo" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("img", Attributes = OptimizeAttributeName)]
public sealed class ImageTagHelper : TagHelper
{
    private const string OptimizeAttributeName = "bl-optimize";

    private static readonly FrozenSet<string> OptimizedImageExtensions =
        FrozenSet.ToFrozenSet(["jpg", "jpeg", "png"], StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> WebpConvertibleExtensions =
        FrozenSet.ToFrozenSet(["jpg", "jpeg", "png"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether to enable optimization. Default is true.
    /// </summary>
    [HtmlAttributeName(OptimizeAttributeName)]
    public bool Optimize { get; set; } = true;

    /// <summary>
    /// The source folder path that triggers optimization.
    /// </summary>
    [HtmlAttributeName("bl-source-path")]
    public string SourcePath { get; set; } = "/images/source";

    /// <summary>
    /// Enable lazy loading. Default is true for below-the-fold images.
    /// </summary>
    [HtmlAttributeName("bl-lazy")]
    public bool? LazyLoad { get; set; }

    /// <inheritdoc/>
    public override int Order => 50;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Optimize)
        {
            base.Process(context, output);
            return;
        }

        if (!output.Attributes.TryGetAttribute("src", out var srcAttribute) || srcAttribute.Value is null)
        {
            base.Process(context, output);
            return;
        }

        var srcValue = srcAttribute.Value switch
        {
            HtmlString htmlString => htmlString.Value,
            string str => str,
            _ => srcAttribute.Value.ToString()
        };

        if (string.IsNullOrWhiteSpace(srcValue))
        {
            base.Process(context, output);
            return;
        }

        var imgSrc = "/" + srcValue.Trim('/');

        if (!imgSrc.StartsWith(SourcePath, StringComparison.OrdinalIgnoreCase))
        {
            base.Process(context, output);
            return;
        }

        var extension = Path.GetExtension(imgSrc).TrimStart('.');
        var hasOptimized = OptimizedImageExtensions.Contains(extension);
        var hasWebp = WebpConvertibleExtensions.Contains(extension);

        if (!hasOptimized && !hasWebp)
        {
            base.Process(context, output);
            return;
        }

        // Build picture element
        output.PreElement.AppendHtml("<picture>");

        var altAttr = GetEncodedAttribute(output, "alt");
        var heightAttr = GetEncodedAttribute(output, "height");
        var widthAttr = GetEncodedAttribute(output, "width");

        // WebP source
        if (hasWebp)
        {
            var webpSrc = imgSrc
                .Replace("/source/", "/webp/", StringComparison.OrdinalIgnoreCase)
                .Replace($".{extension}", ".webp", StringComparison.OrdinalIgnoreCase);

            output.PreElement.AppendHtml(
                $"""<source srcset="{webpSrc}" type="image/webp"{altAttr}{heightAttr}{widthAttr} />""");
        }

        // Optimized source
        if (hasOptimized)
        {
            var optimizedSrc = imgSrc.Replace("/source/", "/optimized/", StringComparison.OrdinalIgnoreCase);
            var mimeType = extension.Equals("jpg", StringComparison.OrdinalIgnoreCase) ? "jpeg" : extension;

            output.PreElement.AppendHtml(
                $"""<source srcset="{optimizedSrc}" type="image/{mimeType}"{altAttr}{heightAttr}{widthAttr} />""");
        }

        output.PostElement.AppendHtml("</picture>");

        // Add lazy loading if specified
        if (LazyLoad == true && !output.Attributes.ContainsName("loading"))
        {
            output.Attributes.SetAttribute("loading", "lazy");
            output.Attributes.SetAttribute("decoding", "async");
        }

        base.Process(context, output);
    }

    private static string GetEncodedAttribute(TagHelperOutput output, string name)
    {
        if (!output.Attributes.ContainsName(name))
        {
            return string.Empty;
        }

        var value = output.Attributes[name].Value?.ToString() ?? string.Empty;
        return $""" {name}="{HttpUtility.HtmlAttributeEncode(value)}" """.Trim();
    }
}

