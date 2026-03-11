using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// ViewComponent for displaying tax summary and breakdown.
/// </summary>
public class TaxSummaryViewComponent(
    ICartService cartService,
    IPricingService pricingService) : ViewComponent
{
    /// <summary>
    /// Renders the tax summary.
    /// </summary>
    /// <param name="showDetails">Whether to show detailed breakdown.</param>
    /// <param name="taxLabel">Custom label for tax (e.g., "VAT", "GST").</param>
    /// <param name="cssClass">Optional CSS class for styling.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        bool showDetails = false,
        string taxLabel = "Tax",
        string? cssClass = null)
    {
        var cart = await cartService.GetCartAsync();
        if (cart == null || cart.IsEmpty)
        {
            return Content(string.Empty);
        }

        var model = new TaxSummaryComponentViewModel
        {
            TaxAmount = cart.Totals.Tax.Amount,
            Subtotal = cart.Totals.Subtotal.Amount,
            CurrencyCode = cart.Totals.Tax.Currency,
            TaxLabel = taxLabel,
            ShowDetails = showDetails,
            CssClass = cssClass
        };

        // Try to get tax breakdown from pricing service
        try
        {
            var totals = await pricingService.CalculateCartTotalsAsync(cart);
            if (totals != null)
            {
                model.TaxRate = CalculateEffectiveRate(totals.Tax.Amount, cart.Totals.Subtotal.Amount);
            }
        }
        catch
        {
            // Use simple calculation if pricing service fails
            model.TaxRate = CalculateEffectiveRate(cart.Totals.Tax.Amount, cart.Totals.Subtotal.Amount);
        }

        return View(model);
    }

    private static decimal CalculateEffectiveRate(decimal tax, decimal subtotal)
    {
        if (subtotal <= 0) return 0;
        return Math.Round((tax / subtotal) * 100, 2);
    }
}

/// <summary>
/// ViewModel for the TaxSummary component.
/// </summary>
public class TaxSummaryComponentViewModel
{
    /// <summary>
    /// Total tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Taxable subtotal.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Effective tax rate percentage.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Currency code for formatting.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Label to display (e.g., "Sales Tax", "VAT").
    /// </summary>
    public string TaxLabel { get; set; } = "Tax";

    /// <summary>
    /// Whether to show detailed breakdown.
    /// </summary>
    public bool ShowDetails { get; set; }

    /// <summary>
    /// CSS class for styling.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Whether tax is included in prices.
    /// </summary>
    public bool TaxIncluded { get; set; }

    /// <summary>
    /// Individual tax line items (for multi-jurisdiction).
    /// </summary>
    public IList<TaxLineItemViewModel> LineItems { get; set; } = [];

    /// <summary>
    /// Gets whether there are multiple tax jurisdictions.
    /// </summary>
    public bool HasMultipleTaxes => LineItems.Count > 1;

    /// <summary>
    /// Gets the formatted tax rate.
    /// </summary>
    public string FormattedRate => $"{TaxRate:0.##}%";
}

/// <summary>
/// ViewModel for individual tax line item.
/// </summary>
public class TaxLineItemViewModel
{
    /// <summary>
    /// Tax jurisdiction or name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tax rate percentage.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets the formatted rate.
    /// </summary>
    public string FormattedRate => $"{Rate:0.##}%";
}
