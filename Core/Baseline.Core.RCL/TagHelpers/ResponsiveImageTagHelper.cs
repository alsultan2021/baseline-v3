using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helper for generating responsive images with srcset.
/// </summary>
/// <example>
/// <code>
/// &lt;responsive-image src="/getmedia/..." alt="Description" /&gt;
/// &lt;responsive-image src="/getmedia/..." alt="Description" widths="320,640,1024" sizes="(max-width: 768px) 100vw, 50vw" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("responsive-image")]
public class ResponsiveImageTagHelper(IResponsiveImageService imageService) : TagHelper
{
    /// <summary>
    /// The image source URL.
    /// </summary>
    [HtmlAttributeName("src")]
    public string Src { get; set; } = string.Empty;

    /// <summary>
    /// Alt text for the image. Required for accessibility.
    /// </summary>
    [HtmlAttributeName("alt")]
    public string Alt { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of widths for srcset.
    /// </summary>
    [HtmlAttributeName("widths")]
    public string? Widths { get; set; }

    /// <summary>
    /// Sizes attribute for responsive behavior.
    /// </summary>
    [HtmlAttributeName("sizes")]
    public string? Sizes { get; set; }

    /// <summary>
    /// CSS class names.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    /// <summary>
    /// Loading behavior (lazy, eager).
    /// </summary>
    [HtmlAttributeName("loading")]
    public string? Loading { get; set; }

    /// <summary>
    /// Fetch priority (high, low, auto).
    /// </summary>
    [HtmlAttributeName("fetchpriority")]
    public string? FetchPriority { get; set; }

    /// <summary>
    /// Image quality (1-100).
    /// </summary>
    [HtmlAttributeName("quality")]
    public int? Quality { get; set; }

    /// <summary>
    /// Image format (webp, jpg, png).
    /// </summary>
    [HtmlAttributeName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// Aspect ratio (e.g., "16/9").
    /// </summary>
    [HtmlAttributeName("aspect-ratio")]
    public string? AspectRatio { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(Src))
        {
            output.SuppressOutput();
            return;
        }

        var options = new ResponsiveImageTagOptions
        {
            Sizes = Sizes,
            CssClass = CssClass,
            Loading = Loading,
            FetchPriority = FetchPriority,
            Quality = Quality,
            Format = Format,
            AspectRatio = AspectRatio
        };

        if (!string.IsNullOrEmpty(Widths))
        {
            options.Widths = Widths.Split(',')
                .Select(w => int.TryParse(w.Trim(), out var width) ? width : 0)
                .Where(w => w > 0);
        }

        var html = imageService.GenerateResponsiveImage(Src, Alt, options);

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(html);
    }
}

