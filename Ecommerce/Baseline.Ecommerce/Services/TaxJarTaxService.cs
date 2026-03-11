using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Configuration options for TaxJar tax service.
/// </summary>
public class TaxJarOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Ecommerce:Tax:TaxJar";

    /// <summary>
    /// TaxJar API token.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use the TaxJar sandbox environment.
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// API version.
    /// </summary>
    public string ApiVersion { get; set; } = "v2";

    /// <summary>
    /// Gets whether the service is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(ApiToken);

    /// <summary>
    /// Gets the API base URL.
    /// </summary>
    public string BaseUrl => UseSandbox
        ? "https://api.sandbox.taxjar.com"
        : "https://api.taxjar.com";
}

/// <summary>
/// TaxJar implementation of external tax service.
/// This is a stub implementation - integrate with Taxjar NuGet package for production.
/// </summary>
public class TaxJarTaxService : IExternalTaxService
{
    private readonly TaxJarOptions options;
    private readonly ILogger<TaxJarTaxService> logger;

    /// <summary>
    /// Creates a new instance of the TaxJar tax service.
    /// </summary>
    public TaxJarTaxService(
        IOptions<TaxJarOptions> options,
        ILogger<TaxJarTaxService> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public string ProviderName => "TaxJar";

    /// <inheritdoc/>
    public bool IsAvailable => options.IsConfigured;

    /// <inheritdoc/>
    public async Task<TaxCalculationResult> CalculateTaxAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return TaxCalculationResult.Failed("TaxJar is not configured");
        }

        try
        {
            logger.LogInformation(
                "Calculating tax with TaxJar for {LineCount} line items to {City}, {State} {Zip}",
                request.LineItems.Count,
                request.DestinationAddress.City,
                request.DestinationAddress.StateProvince,
                request.DestinationAddress.PostalCode);

            // TODO: Implement actual TaxJar API call using Taxjar NuGet package
            // Example:
            // var client = new TaxjarApi(options.ApiToken, new { apiUrl = options.BaseUrl });
            // var result = await client.TaxForOrderAsync(order);

            // Stub implementation - returns state-based tax
            var currency = request.CurrencyCode;
            var stateTaxRate = GetStateTaxRate(request.DestinationAddress.StateProvince ?? string.Empty);
            
            var lineResults = new List<TaxCalculationLineResult>();
            var totalTaxable = 0m;
            var totalTax = 0m;

            foreach (var line in request.LineItems)
            {
                var lineTotal = line.UnitPrice.Amount * line.Quantity;
                var lineTax = line.IsExempt ? 0m : lineTotal * stateTaxRate;
                totalTaxable += lineTotal;
                totalTax += lineTax;

                lineResults.Add(new TaxCalculationLineResult
                {
                    LineId = line.LineId,
                    TaxableAmount = new Money { Amount = lineTotal, Currency = currency },
                    TaxAmount = new Money { Amount = lineTax, Currency = currency },
                    EffectiveRate = line.IsExempt ? 0m : stateTaxRate,
                    IsExempt = line.IsExempt
                });
            }

            var shippingTax = 0m;
            if (request.ShippingAmount != null && request.ShippingAmount.Amount > 0 && IsShippingTaxable(request.DestinationAddress.StateProvince))
            {
                shippingTax = request.ShippingAmount.Amount * stateTaxRate;
                totalTax += shippingTax;
            }

            var result = new TaxCalculationResult
            {
                Success = true,
                ProviderTransactionId = $"tj_{Guid.NewGuid():N}",
                TotalTax = new Money { Amount = Math.Round(totalTax, 2), Currency = currency },
                TaxableAmount = new Money { Amount = totalTaxable, Currency = currency },
                ShippingTax = new Money { Amount = Math.Round(shippingTax, 2), Currency = currency },
                EffectiveRate = stateTaxRate,
                LineResults = lineResults,
                JurisdictionBreakdown =
                [
                    new TaxJurisdictionResult
                    {
                        JurisdictionType = "State",
                        JurisdictionName = request.DestinationAddress.StateProvince ?? "Unknown",
                        Rate = stateTaxRate,
                        TaxAmount = new Money { Amount = Math.Round(totalTax, 2), Currency = currency },
                        TaxableAmount = new Money { Amount = totalTaxable, Currency = currency }
                    }
                ],
                CalculatedAt = DateTimeOffset.UtcNow
            };

            logger.LogInformation(
                "TaxJar tax calculation complete: Total tax ${TotalTax} ({Rate:P2}) for {State}",
                result.TotalTax.Amount,
                stateTaxRate,
                request.DestinationAddress.StateProvince);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating tax with TaxJar");
            return TaxCalculationResult.Failed($"TaxJar error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CommitTransactionAsync(
        string providerTransactionId,
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            logger.LogInformation(
                "Creating TaxJar transaction for order {OrderNumber} (ref: {TransactionId})",
                orderNumber,
                providerTransactionId);

            // TODO: Implement actual TaxJar order creation
            // await client.CreateOrderAsync(order);

            await Task.Delay(10, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating TaxJar transaction for order {OrderNumber}", orderNumber);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VoidTransactionAsync(
        string providerTransactionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            logger.LogInformation(
                "Deleting TaxJar transaction {TransactionId}: {Reason}",
                providerTransactionId,
                reason ?? "No reason provided");

            // TODO: Implement actual TaxJar order deletion
            // await client.DeleteOrderAsync(transactionId);

            await Task.Delay(10, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting TaxJar transaction {TransactionId}", providerTransactionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<AddressValidationResult> ValidateAddressAsync(
        Address address,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return AddressValidationResult.Invalid("TaxJar is not configured");
        }

        try
        {
            logger.LogDebug("Validating address with TaxJar: {City}, {State} {Zip}",
                address.City, address.StateProvince, address.PostalCode);

            // TODO: Implement actual TaxJar address validation
            // var result = await client.ValidateAddressAsync(address);

            await Task.Delay(10, cancellationToken);

            // Basic validation for US addresses
            if (address.CountryCode?.ToUpperInvariant() == "US" || address.CountryCode?.ToUpperInvariant() == "USA")
            {
                if (string.IsNullOrWhiteSpace(address.PostalCode))
                {
                    return AddressValidationResult.Invalid("ZIP code is required for US addresses");
                }
                if (string.IsNullOrWhiteSpace(address.StateProvince))
                {
                    return AddressValidationResult.Invalid("State is required for US addresses");
                }
            }

            return AddressValidationResult.Valid();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating address with TaxJar");
            return AddressValidationResult.Invalid($"Validation error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> GetTaxRateAsync(
        Address address,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return 0m;
        }

        try
        {
            // TODO: Implement actual TaxJar rate lookup
            // var result = await client.RatesForLocationAsync(postalCode);

            await Task.Delay(10, cancellationToken);
            return GetStateTaxRate(address.StateProvince ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tax rate from TaxJar");
            return 0m;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            logger.LogDebug("Testing TaxJar connection");

            // TODO: Implement actual TaxJar categories call (simple API test)
            // var categories = await client.CategoriesAsync();

            await Task.Delay(10, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TaxJar connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Gets an approximate state tax rate for stub purposes.
    /// In production, this would come from TaxJar API.
    /// </summary>
    private static decimal GetStateTaxRate(string stateCode)
    {
        return stateCode.ToUpperInvariant() switch
        {
            "AL" => 0.04m,
            "AZ" => 0.056m,
            "AR" => 0.065m,
            "CA" => 0.0725m,
            "CO" => 0.029m,
            "CT" => 0.0635m,
            "FL" => 0.06m,
            "GA" => 0.04m,
            "HI" => 0.04m,
            "ID" => 0.06m,
            "IL" => 0.0625m,
            "IN" => 0.07m,
            "IA" => 0.06m,
            "KS" => 0.065m,
            "KY" => 0.06m,
            "LA" => 0.0445m,
            "ME" => 0.055m,
            "MD" => 0.06m,
            "MA" => 0.0625m,
            "MI" => 0.06m,
            "MN" => 0.06875m,
            "MS" => 0.07m,
            "MO" => 0.04225m,
            "NE" => 0.055m,
            "NV" => 0.0685m,
            "NJ" => 0.06625m,
            "NM" => 0.05125m,
            "NY" => 0.04m,
            "NC" => 0.0475m,
            "ND" => 0.05m,
            "OH" => 0.0575m,
            "OK" => 0.045m,
            "PA" => 0.06m,
            "RI" => 0.07m,
            "SC" => 0.06m,
            "SD" => 0.045m,
            "TN" => 0.07m,
            "TX" => 0.0625m,
            "UT" => 0.0485m,
            "VT" => 0.06m,
            "VA" => 0.053m,
            "WA" => 0.065m,
            "WV" => 0.06m,
            "WI" => 0.05m,
            "WY" => 0.04m,
            // Canadian provinces
            "AB" => 0.05m,  // GST only
            "BC" => 0.12m,  // GST + PST
            "MB" => 0.12m,  // GST + PST
            "NB" => 0.15m,  // HST
            "NL" => 0.15m,  // HST
            "NS" => 0.15m,  // HST
            "ON" => 0.13m,  // HST
            "PE" => 0.15m,  // HST
            "QC" => 0.14975m, // GST + QST
            "SK" => 0.11m,  // GST + PST
            // States with no sales tax
            "AK" or "DE" or "MT" or "NH" or "OR" => 0m,
            // Territories
            "NT" or "NU" or "YT" => 0.05m, // GST only
            _ => 0.05m // Default fallback
        };
    }

    /// <summary>
    /// Checks if shipping is taxable in the given state.
    /// </summary>
    private static bool IsShippingTaxable(string? stateCode)
    {
        if (string.IsNullOrEmpty(stateCode))
        {
            return false;
        }

        // States where shipping is generally taxable
        return stateCode.ToUpperInvariant() switch
        {
            "AR" or "CT" or "GA" or "HI" or "KS" or "KY" or "MI" or "MN" or
            "MS" or "NE" or "NJ" or "NM" or "NY" or "NC" or "ND" or "OH" or
            "PA" or "SC" or "SD" or "TN" or "TX" or "VT" or "WA" or "WV" or "WI" => true,
            _ => false
        };
    }
}
