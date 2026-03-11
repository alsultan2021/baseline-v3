using CMS.Commerce;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce;

// TODO P3-2: Replace the flat-rate TaxCalculationOptions dictionary with
// an external tax-provider abstraction (e.g. Avalara, TaxJar) so rates are
// fetched dynamically per address. Keep TaxCalculationOptions as the
// fallback/offline provider behind the same ITaxProvider interface.

/// <summary>
/// Configuration options for tax calculation.
/// </summary>
public sealed class TaxCalculationOptions
{
    /// <summary>
    /// Whether catalog prices already include tax.
    /// When true, tax should be extracted from prices rather than added.
    /// </summary>
    public bool PricesIncludeTax { get; set; }

    /// <summary>
    /// The default tax rate to apply when no category-specific rate is found.
    /// Expressed as a decimal (e.g., 0.1 for 10%).
    /// </summary>
    public decimal DefaultTaxRate { get; set; } = 0m;

    /// <summary>
    /// Tax rates by category name. Key is the tax category, value is the rate as a decimal.
    /// </summary>
    public Dictionary<string, decimal> TaxRatesByCategory { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tax rates by state/region code. Key is the region code (e.g., "CA", "NY"), value is the rate.
    /// Only used if shipping address is provided during checkout.
    /// </summary>
    public Dictionary<string, decimal> TaxRatesByRegion { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether digital products are taxable.
    /// </summary>
    public bool TaxDigitalProducts { get; set; } = true;

    /// <summary>
    /// Whether to use database tax classes when available.
    /// When true, the system will first check for tax rates from TaxClassInfo records,
    /// then fall back to configured options.
    /// </summary>
    public bool UseDatabaseTaxClasses { get; set; } = true;
}

/// <summary>
/// Default tax calculation step that uses configured rates and database tax classes.
/// Per Kentico Commerce documentation, the default tax calculation step does not calculate
/// any taxes. Projects must provide a custom implementation for prices to include taxes
/// in ShoppingCart and Checkout modes.
/// </summary>
/// <remarks>
/// For production use, consider implementing a custom step that integrates
/// with tax calculation services like Avalara, TaxJar, or Vertex.
/// </remarks>
/// <typeparam name="TRequest">The price calculation request type implementing IPriceCalculationRequest.</typeparam>
/// <typeparam name="TResult">The price calculation result type implementing IPriceCalculationResult.</typeparam>
public class DefaultTaxPriceCalculationStep<TRequest, TResult>(
    IOptions<TaxCalculationOptions> options,
    ITaxClassService taxClassService,
    ILogger<DefaultTaxPriceCalculationStep<TRequest, TResult>> logger) : ITaxPriceCalculationStep<TRequest, TResult>
    where TRequest : class, IPriceCalculationRequest
    where TResult : class, IPriceCalculationResult
{
    /// <summary>
    /// Gets the tax calculation options.
    /// </summary>
    protected TaxCalculationOptions Options => options.Value;

    /// <summary>
    /// Gets the tax class service for database lookups.
    /// </summary>
    protected ITaxClassService TaxClassService => taxClassService;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => logger;

    /// <inheritdoc/>
    public virtual async Task Execute(
        IPriceCalculationData<TRequest, TResult> calculationData,
        CancellationToken cancellationToken)
    {
        calculationData.Result.TotalTax = 0m;

        foreach (var resultItem in calculationData.Result.Items)
        {
            var taxRate = await GetTaxRateForItemAsync(resultItem, calculationData);
            var taxAmount = CalculateTaxAmount(resultItem, taxRate);

            // Round to 2 decimal places
            resultItem.LineTaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
            calculationData.Result.TotalTax += resultItem.LineTaxAmount;

            Logger.LogDebug(
                "Applied tax rate {TaxRate:P2} to item, tax amount: {TaxAmount:C}",
                taxRate, resultItem.LineTaxAmount);
        }

        Logger.LogDebug("Total tax calculated: {TotalTax:C}", calculationData.Result.TotalTax);
    }

    /// <summary>
    /// Gets the tax rate for a specific line item.
    /// Override this method to implement custom tax rate logic based on product data,
    /// customer location, or external tax services.
    /// </summary>
    /// <param name="resultItem">The calculation result item.</param>
    /// <param name="calculationData">The full calculation data including request and result.</param>
    /// <returns>The tax rate as a decimal (e.g., 0.1 for 10%).</returns>
    protected virtual async Task<decimal> GetTaxRateForItemAsync(
        dynamic resultItem,
        IPriceCalculationData<TRequest, TResult> calculationData)
    {
        // Check if product is tax exempt using extended product data
        var productData = resultItem.ProductData;
        string? taxCategory = null;

        if (productData is ExtendedProductData extendedData)
        {
            if (extendedData.IsTaxExempt)
            {
                Logger.LogDebug("Product is tax exempt");
                return 0m;
            }

            // Check for digital product tax exemption
            if (extendedData.IsDigital && !Options.TaxDigitalProducts)
            {
                Logger.LogDebug("Digital product exempt from tax");
                return 0m;
            }

            taxCategory = extendedData.TaxCategory;

            // First, try to get rate from database tax classes if enabled
            if (Options.UseDatabaseTaxClasses && !string.IsNullOrEmpty(taxCategory))
            {
                var dbRate = await GetTaxRateFromDatabaseAsync(taxCategory);
                if (dbRate.HasValue)
                {
                    Logger.LogDebug("Using database tax class rate for category: {Category}", taxCategory);
                    return dbRate.Value;
                }
            }

            // Try to get rate by tax category from config
            if (!string.IsNullOrEmpty(taxCategory) &&
                Options.TaxRatesByCategory.TryGetValue(taxCategory, out var categoryRate))
            {
                Logger.LogDebug("Using category-based tax rate for category: {Category}", taxCategory);
                return categoryRate;
            }
        }

        // Try to get region-based rate from shipping address
        var regionRate = GetRegionTaxRate(calculationData.Request);
        if (regionRate.HasValue)
        {
            return regionRate.Value;
        }

        // Try default tax class from database
        if (Options.UseDatabaseTaxClasses)
        {
            var defaultDbRate = await GetDefaultTaxRateFromDatabaseAsync();
            if (defaultDbRate.HasValue)
            {
                Logger.LogDebug("Using default database tax class rate");
                return defaultDbRate.Value;
            }
        }

        // Fall back to default rate from config
        return Options.DefaultTaxRate;
    }

    /// <summary>
    /// Gets the tax rate from database tax class by code name.
    /// </summary>
    /// <param name="taxClassCodeName">The tax class code name.</param>
    /// <returns>The tax rate if found, otherwise null.</returns>
    protected virtual async Task<decimal?> GetTaxRateFromDatabaseAsync(string taxClassCodeName)
    {
        try
        {
            var taxClasses = await TaxClassService.GetTaxClassesAsync();
            var taxClass = taxClasses.FirstOrDefault(tc =>
                string.Equals(tc.Code, taxClassCodeName, StringComparison.OrdinalIgnoreCase));

            return taxClass?.DefaultRate;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get tax rate from database for class: {TaxClass}", taxClassCodeName);
            return null;
        }
    }

    /// <summary>
    /// Gets the default tax rate from database.
    /// </summary>
    /// <returns>The default tax rate if available, otherwise null.</returns>
    protected virtual async Task<decimal?> GetDefaultTaxRateFromDatabaseAsync()
    {
        try
        {
            var defaultTaxClass = await TaxClassService.GetDefaultTaxClassAsync();
            return defaultTaxClass?.DefaultRate;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get default tax class from database");
            return null;
        }
    }

    /// <summary>
    /// Gets the tax rate based on the shipping region from the request.
    /// Override to implement custom region-based tax logic.
    /// </summary>
    /// <param name="request">The price calculation request.</param>
    /// <returns>The region-based tax rate, or null if not applicable.</returns>
    protected virtual decimal? GetRegionTaxRate(TRequest request)
    {
        // Default implementation doesn't have access to shipping address
        // Override in site implementation to access address from checkout data
        return null;
    }

    /// <summary>
    /// Calculates the tax amount for a line item.
    /// Handles both tax-inclusive and tax-exclusive pricing.
    /// </summary>
    /// <param name="resultItem">The calculation result item.</param>
    /// <param name="taxRate">The tax rate to apply.</param>
    /// <returns>The calculated tax amount.</returns>
    protected virtual decimal CalculateTaxAmount(
        dynamic resultItem,
        decimal taxRate)
    {
        if (taxRate == 0m)
        {
            return 0m;
        }

        decimal lineSubtotal = resultItem.LineSubtotalAfterAllDiscounts;

        if (Options.PricesIncludeTax)
        {
            // Extract tax from the price (price already includes tax)
            // Tax = Price * (Rate / (1 + Rate))
            return lineSubtotal * (taxRate / (1 + taxRate));
        }
        else
        {
            // Add tax on top of the price
            return lineSubtotal * taxRate;
        }
    }
}

/// <summary>
/// No-op tax calculation step that calculates zero tax.
/// Use this when tax calculation is not required or handled externally.
/// </summary>
/// <typeparam name="TRequest">The price calculation request type implementing IPriceCalculationRequest.</typeparam>
/// <typeparam name="TResult">The price calculation result type implementing IPriceCalculationResult.</typeparam>
public sealed class NoOpTaxPriceCalculationStep<TRequest, TResult>(
    ILogger<NoOpTaxPriceCalculationStep<TRequest, TResult>> logger) : ITaxPriceCalculationStep<TRequest, TResult>
    where TRequest : class, IPriceCalculationRequest
    where TResult : class, IPriceCalculationResult
{
    /// <inheritdoc/>
    public Task Execute(
        IPriceCalculationData<TRequest, TResult> calculationData,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("NoOpTaxPriceCalculationStep: No tax calculated. Configure a real tax step for production.");

        calculationData.Result.TotalTax = 0m;

        foreach (var resultItem in calculationData.Result.Items)
        {
            resultItem.LineTaxAmount = 0m;
        }

        return Task.CompletedTask;
    }
}
