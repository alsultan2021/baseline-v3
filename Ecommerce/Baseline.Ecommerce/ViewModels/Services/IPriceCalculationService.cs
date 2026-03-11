namespace Ecommerce.Services;

/// <summary>
/// Individual discount entry for display
/// </summary>
public class DiscountEntryResult
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Individual tax entry for display (e.g., GST, QST as separate lines)
/// </summary>
public class TaxEntryResult
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Price calculation result
/// </summary>
public class PriceCalculationResult
{
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Discount { get; set; }
    public string? DiscountDescription { get; set; }
    public IList<DiscountEntryResult> DiscountEntries { get; set; } = [];
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// The tax rate applied to the order (e.g., 0.14975 for 14.975%).
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// The display name of the tax (e.g., "GST + QST", "HST").
    /// </summary>
    public string TaxName { get; set; } = "Tax";

    /// <summary>
    /// Individual tax entries for display (e.g., GST, QST as separate lines).
    /// </summary>
    public IList<TaxEntryResult> TaxEntries { get; set; } = [];

    // Alias for chevalroyal
    public decimal TotalTax => Tax;
}

/// <summary>
/// IPriceCalculationService non-generic interface.
/// </summary>
public interface IPriceCalculationService
{
    Task<decimal> CalculateTotalAsync(Ecommerce.Models.ShoppingCartDataModel cart, CancellationToken cancellationToken = default);
    Task<decimal> CalculateTaxAsync(Ecommerce.Models.ShoppingCartDataModel cart, CancellationToken cancellationToken = default);
    Task<decimal> CalculateShippingAsync(Ecommerce.Models.ShoppingCartDataModel cart, CancellationToken cancellationToken = default);
    Task<PriceCalculationResult> CalculateCartAsync(int? shippingMethodId, int? paymentMethodId, Ecommerce.Models.OrderAddress? shippingAddress, CancellationToken cancellationToken = default);
}
