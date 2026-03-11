using System.Collections.Frozen;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Core.RCL.TagHelpers;

/// <summary>
/// Tag helper for generating JSON-LD structured data script tags.
/// </summary>
/// <example>
/// <code>
/// &lt;json-ld type="BreadcrumbList" asp-items="@Model.Breadcrumbs" /&gt;
/// &lt;json-ld type="Organization" name="My Company" url="https://example.com" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("json-ld")]
public class JsonLdTagHelper(IStructuredDataService structuredDataService) : TagHelper
{
    /// <summary>
    /// The schema.org type (Article, BreadcrumbList, FAQPage, Organization, WebSite).
    /// </summary>
    [HtmlAttributeName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Name property for Organization/WebSite schemas.
    /// </summary>
    [HtmlAttributeName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// URL property for Organization/WebSite schemas.
    /// </summary>
    [HtmlAttributeName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Description property.
    /// </summary>
    [HtmlAttributeName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Logo URL for Organization schema.
    /// </summary>
    [HtmlAttributeName("logo")]
    public string? Logo { get; set; }

    /// <summary>
    /// Breadcrumb items for BreadcrumbList schema.
    /// </summary>
    [HtmlAttributeName("breadcrumbs")]
    public IEnumerable<BreadcrumbItem>? Breadcrumbs { get; set; }

    /// <summary>
    /// FAQ items for FAQPage schema.
    /// </summary>
    [HtmlAttributeName("faqs")]
    public IEnumerable<FaqItem>? Faqs { get; set; }

    /// <summary>
    /// Search action URL template for WebSite schema.
    /// </summary>
    [HtmlAttributeName("search-action-url")]
    public string? SearchActionUrl { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        string jsonLd;

        switch (Type.ToLowerInvariant())
        {
            case "breadcrumblist":
                if (Breadcrumbs is null)
                {
                    output.SuppressOutput();
                    return;
                }
                jsonLd = await structuredDataService.GenerateBreadcrumbJsonLdAsync(Breadcrumbs);
                break;

            case "faqpage":
                if (Faqs is null)
                {
                    output.SuppressOutput();
                    return;
                }
                jsonLd = await structuredDataService.GenerateFaqJsonLdAsync(Faqs);
                break;

            case "organization":
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Url))
                {
                    output.SuppressOutput();
                    return;
                }
                jsonLd = await structuredDataService.GenerateOrganizationJsonLdAsync(
                    new OrganizationData(Name, Url, Logo, Description));
                break;

            case "website":
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Url))
                {
                    output.SuppressOutput();
                    return;
                }
                jsonLd = await structuredDataService.GenerateWebSiteJsonLdAsync(
                    new WebSiteData(Name, Url, SearchActionUrl, Description));
                break;

            default:
                output.SuppressOutput();
                return;
        }

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(jsonLd);
    }
}

