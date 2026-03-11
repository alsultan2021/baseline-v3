using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helper for baseline-enhanced anchor links with tracking and external link handling.
/// </summary>
/// <example>
/// <code>
/// &lt;a bl-link href="https://external.com"&gt;External Link&lt;/a&gt;
/// &lt;a bl-link bl-track="cta-button" href="/contact"&gt;Contact&lt;/a&gt;
/// </code>
/// </example>
[HtmlTargetElement("a", Attributes = LinkAttributeName)]
public sealed class LinkTagHelper : TagHelper
{
    private const string LinkAttributeName = "bl-link";
    private const string TrackAttributeName = "bl-track";
    private const string ExternalBehaviorAttributeName = "bl-external";

    private static readonly FrozenSet<string> ExternalProtocols =
        FrozenSet.ToFrozenSet(["http://", "https://", "//"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Enable baseline link enhancements.
    /// </summary>
    [HtmlAttributeName(LinkAttributeName)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tracking identifier for analytics.
    /// </summary>
    [HtmlAttributeName(TrackAttributeName)]
    public string? TrackingId { get; set; }

    /// <summary>
    /// Behavior for external links: "newtab", "noopener", "noreferrer", or "all".
    /// Default is "all" which applies target="_blank" rel="noopener noreferrer".
    /// </summary>
    [HtmlAttributeName(ExternalBehaviorAttributeName)]
    public string ExternalBehavior { get; set; } = "all";

    /// <summary>
    /// Whether to automatically detect and handle external links.
    /// </summary>
    [HtmlAttributeName("bl-auto-external")]
    public bool AutoDetectExternal { get; set; } = true;

    /// <summary>
    /// Current site hostname for external link detection.
    /// </summary>
    [HtmlAttributeName("bl-site-host")]
    public string? SiteHost { get; set; }

    /// <inheritdoc/>
    public override int Order => 100;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled)
        {
            return;
        }

        var href = output.Attributes["href"]?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(href))
        {
            return;
        }

        // Handle external links
        if (AutoDetectExternal && IsExternalLink(href))
        {
            ApplyExternalLinkAttributes(output);
        }

        // Add tracking data attribute
        if (!string.IsNullOrWhiteSpace(TrackingId))
        {
            output.Attributes.SetAttribute("data-track", TrackingId);
        }
    }

    private bool IsExternalLink(string href)
    {
        // Check if it starts with external protocol
        if (!ExternalProtocols.Any(p => href.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // If site host is specified, check if href points to different domain
        if (!string.IsNullOrWhiteSpace(SiteHost))
        {
            try
            {
                var uri = new Uri(href, UriKind.Absolute);
                return !uri.Host.Equals(SiteHost, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }

        return true;
    }

    private void ApplyExternalLinkAttributes(TagHelperOutput output)
    {
        var behavior = ExternalBehavior.ToLowerInvariant();

        if (behavior is "newtab" or "all")
        {
            if (!output.Attributes.ContainsName("target"))
            {
                output.Attributes.SetAttribute("target", "_blank");
            }
        }

        var relParts = new List<string>(2);

        if (behavior is "noopener" or "all")
        {
            relParts.Add("noopener");
        }

        if (behavior is "noreferrer" or "all")
        {
            relParts.Add("noreferrer");
        }

        if (relParts.Count > 0)
        {
            var existingRel = output.Attributes["rel"]?.Value?.ToString() ?? string.Empty;
            var combinedRel = string.Join(" ", relParts.Concat(existingRel.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct());
            output.Attributes.SetAttribute("rel", combinedRel);
        }
    }
}

