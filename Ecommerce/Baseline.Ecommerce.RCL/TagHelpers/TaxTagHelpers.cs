using System.Globalization;
using Baseline.Ecommerce;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Baseline.Ecommerce.TagHelpers;

/// <summary>
/// Tag helper for displaying tax amounts with proper formatting.
/// Usage: &lt;tax-amount value="19.99" currency="USD" /&gt;
/// </summary>
[HtmlTargetElement("tax-amount")]
public class TaxAmountTagHelper : TagHelper
{
    /// <summary>
    /// The tax amount to display.
    /// </summary>
    [HtmlAttributeName("value")]
    public decimal Value { get; set; }

    /// <summary>
    /// Currency code (ISO 4217).
    /// </summary>
    [HtmlAttributeName("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Culture code for formatting (e.g., "en-US", "de-DE").
    /// </summary>
    [HtmlAttributeName("culture")]
    public string? Culture { get; set; }

    /// <summary>
    /// Whether to show currency symbol.
    /// </summary>
    [HtmlAttributeName("show-symbol")]
    public bool ShowSymbol { get; set; } = true;

    /// <summary>
    /// CSS class to apply.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;

        var culture = GetCulture();
        var formatted = FormatAmount(culture);

        if (!string.IsNullOrEmpty(CssClass))
        {
            output.Attributes.SetAttribute("class", CssClass);
        }

        output.Attributes.SetAttribute("data-amount", Value.ToString(CultureInfo.InvariantCulture));
        output.Attributes.SetAttribute("data-currency", Currency);

        output.Content.SetContent(formatted);
    }

    private CultureInfo GetCulture()
    {
        if (!string.IsNullOrEmpty(Culture))
        {
            try
            {
                return CultureInfo.GetCultureInfo(Culture);
            }
            catch
            {
                // Fall through to default
            }
        }

        return CurrencyCultureResolver.Resolve(Currency ?? "USD");
    }

    private string FormatAmount(CultureInfo culture)
    {
        if (ShowSymbol)
        {
            return Value.ToString("C", culture);
        }

        return Value.ToString("N2", culture);
    }
}

/// <summary>
/// Tag helper for displaying tax rate as percentage.
/// Usage: &lt;tax-rate value="8.25" /&gt;
/// </summary>
[HtmlTargetElement("tax-rate")]
public class TaxRateTagHelper : TagHelper
{
    /// <summary>
    /// The tax rate percentage (e.g., 8.25 for 8.25%).
    /// </summary>
    [HtmlAttributeName("value")]
    public decimal Value { get; set; }

    /// <summary>
    /// Number of decimal places to show.
    /// </summary>
    [HtmlAttributeName("decimals")]
    public int Decimals { get; set; } = 2;

    /// <summary>
    /// Whether to show the percent symbol.
    /// </summary>
    [HtmlAttributeName("show-symbol")]
    public bool ShowSymbol { get; set; } = true;

    /// <summary>
    /// CSS class to apply.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;

        var format = $"0.{new string('#', Decimals)}";
        var formatted = Value.ToString(format);

        if (ShowSymbol)
        {
            formatted += "%";
        }

        if (!string.IsNullOrEmpty(CssClass))
        {
            output.Attributes.SetAttribute("class", CssClass);
        }

        output.Attributes.SetAttribute("data-rate", Value.ToString(CultureInfo.InvariantCulture));

        output.Content.SetContent(formatted);
    }
}

/// <summary>
/// Tag helper for displaying price with tax information.
/// Usage: &lt;price-with-tax amount="99.99" tax="8.00" show-breakdown="true" /&gt;
/// </summary>
[HtmlTargetElement("price-with-tax")]
public class PriceWithTaxTagHelper : TagHelper
{
    /// <summary>
    /// The base price amount.
    /// </summary>
    [HtmlAttributeName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// The tax amount.
    /// </summary>
    [HtmlAttributeName("tax")]
    public decimal Tax { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    [HtmlAttributeName("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether tax is already included in the amount.
    /// </summary>
    [HtmlAttributeName("tax-inclusive")]
    public bool TaxInclusive { get; set; }

    /// <summary>
    /// Whether to show the breakdown.
    /// </summary>
    [HtmlAttributeName("show-breakdown")]
    public bool ShowBreakdown { get; set; }

    /// <summary>
    /// Label for excluding tax.
    /// </summary>
    [HtmlAttributeName("excl-label")]
    public string ExcludingLabel { get; set; } = "excl. tax";

    /// <summary>
    /// Label for including tax.
    /// </summary>
    [HtmlAttributeName("incl-label")]
    public string IncludingLabel { get; set; } = "incl. tax";

    /// <summary>
    /// CSS class for container.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;

        var culture = GetCultureForCurrency();
        var totalWithTax = TaxInclusive ? Amount : Amount + Tax;
        var totalWithoutTax = TaxInclusive ? Amount - Tax : Amount;

        var cssClass = "price-with-tax";
        if (!string.IsNullOrEmpty(CssClass))
        {
            cssClass += $" {CssClass}";
        }
        output.Attributes.SetAttribute("class", cssClass);

        if (ShowBreakdown)
        {
            var content = $@"
                <span class=""price-main"">{totalWithTax.ToString("C", culture)}</span>
                <span class=""price-tax-info"">({IncludingLabel})</span>
                <span class=""price-excl"">{totalWithoutTax.ToString("C", culture)} {ExcludingLabel}</span>
            ";
            output.Content.SetHtmlContent(content.Trim());
        }
        else
        {
            var displayPrice = TaxInclusive ? totalWithTax : totalWithoutTax;
            var label = TaxInclusive ? IncludingLabel : ExcludingLabel;
            output.Content.SetHtmlContent($@"
                <span class=""price-main"">{displayPrice.ToString("C", culture)}</span>
                <span class=""price-tax-info"">({label})</span>
            ".Trim());
        }
    }

    private CultureInfo GetCultureForCurrency()
        => CurrencyCultureResolver.Resolve(Currency ?? "USD");
}

/// <summary>
/// Tag helper for displaying tax-inclusive/exclusive badge.
/// Usage: &lt;tax-badge inclusive="true" /&gt;
/// </summary>
[HtmlTargetElement("tax-badge")]
public class TaxBadgeTagHelper : TagHelper
{
    /// <summary>
    /// Whether tax is inclusive.
    /// </summary>
    [HtmlAttributeName("inclusive")]
    public bool Inclusive { get; set; }

    /// <summary>
    /// Custom label for inclusive.
    /// </summary>
    [HtmlAttributeName("incl-text")]
    public string InclusiveText { get; set; } = "Tax included";

    /// <summary>
    /// Custom label for exclusive.
    /// </summary>
    [HtmlAttributeName("excl-text")]
    public string ExclusiveText { get; set; } = "Plus tax";

    /// <summary>
    /// CSS class to apply.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;

        var baseClass = Inclusive ? "tax-badge tax-inclusive" : "tax-badge tax-exclusive";
        if (!string.IsNullOrEmpty(CssClass))
        {
            baseClass += $" {CssClass}";
        }
        output.Attributes.SetAttribute("class", baseClass);

        output.Content.SetContent(Inclusive ? InclusiveText : ExclusiveText);
    }
}

/// <summary>
/// Tag helper for displaying tax exemption status.
/// Usage: &lt;tax-exempt-status is-exempt="true" reason="Resale" /&gt;
/// </summary>
[HtmlTargetElement("tax-exempt-status")]
public class TaxExemptStatusTagHelper : TagHelper
{
    /// <summary>
    /// Whether the customer is tax-exempt.
    /// </summary>
    [HtmlAttributeName("is-exempt")]
    public bool IsExempt { get; set; }

    /// <summary>
    /// Exemption reason to display.
    /// </summary>
    [HtmlAttributeName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Certificate number to display.
    /// </summary>
    [HtmlAttributeName("certificate")]
    public string? Certificate { get; set; }

    /// <summary>
    /// Whether to show details.
    /// </summary>
    [HtmlAttributeName("show-details")]
    public bool ShowDetails { get; set; }

    /// <summary>
    /// CSS class to apply.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!IsExempt)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        var baseClass = "tax-exempt-status";
        if (!string.IsNullOrEmpty(CssClass))
        {
            baseClass += $" {CssClass}";
        }
        output.Attributes.SetAttribute("class", baseClass);

        var content = "<span class=\"tax-exempt-badge\">Tax Exempt</span>";

        if (ShowDetails)
        {
            if (!string.IsNullOrEmpty(Reason))
            {
                content += $"<span class=\"tax-exempt-reason\">Reason: {Reason}</span>";
            }
            if (!string.IsNullOrEmpty(Certificate))
            {
                content += $"<span class=\"tax-exempt-cert\">Certificate: {Certificate}</span>";
            }
        }

        output.Content.SetHtmlContent(content);
    }
}
