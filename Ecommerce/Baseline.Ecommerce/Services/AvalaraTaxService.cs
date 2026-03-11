using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Configuration options for Avalara tax service.
/// </summary>
public class AvalaraOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Ecommerce:Tax:Avalara";

    /// <summary>
    /// Avalara account ID.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Avalara license key.
    /// </summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Company code in Avalara.
    /// </summary>
    public string CompanyCode { get; set; } = "DEFAULT";

    /// <summary>
    /// Whether to use the Avalara sandbox environment.
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Service URL (auto-configured based on UseSandbox if not specified).
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets whether the service is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(AccountId) && !string.IsNullOrEmpty(LicenseKey);

    /// <summary>
    /// Gets the effective service URL.
    /// </summary>
    public string EffectiveServiceUrl => ServiceUrl
        ?? (UseSandbox
            ? "https://sandbox-rest.avatax.com"
            : "https://rest.avatax.com");
}

/// <summary>
/// Avalara AvaTax implementation of external tax service.
/// This is a stub implementation - integrate with Avalara.AvaTax NuGet package for production.
/// </summary>
public class AvalaraTaxService : IExternalTaxService
{
    private readonly AvalaraOptions options;
    private readonly ILogger<AvalaraTaxService> logger;

    /// <summary>
    /// Creates a new instance of the Avalara tax service.
    /// </summary>
    public AvalaraTaxService(
        IOptions<AvalaraOptions> options,
        ILogger<AvalaraTaxService> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public string ProviderName => "Avalara AvaTax";

    /// <inheritdoc/>
    public bool IsAvailable => options.IsConfigured;

    /// <inheritdoc/>
    public async Task<TaxCalculationResult> CalculateTaxAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return TaxCalculationResult.Failed("Avalara is not configured");
        }

        try
        {
            logger.LogInformation(
                "Calculating tax with Avalara for {LineCount} line items to {City}, {State}",
                request.LineItems.Count,
                request.DestinationAddress.City,
                request.DestinationAddress.StateProvince);

            // TODO: Implement actual Avalara API call using Avalara.AvaTax NuGet package
            // Example:
            // var client = new AvaTaxClient("MyApp", "1.0", Environment.MachineName, 
            //     options.UseSandbox ? AvaTaxEnvironment.Sandbox : AvaTaxEnvironment.Production);
            // client.WithSecurity(options.AccountId, options.LicenseKey);
            // var result = await client.CreateTransactionAsync(null, transaction);

            // Stub implementation - returns estimated 8% tax
            var currency = request.CurrencyCode;
            var lineResults = new List<TaxCalculationLineResult>();
            var totalTaxable = 0m;
            var totalTax = 0m;

            foreach (var line in request.LineItems)
            {
                var lineTotal = line.UnitPrice.Amount * line.Quantity;
                var lineTax = line.IsExempt ? 0m : lineTotal * 0.08m;
                totalTaxable += lineTotal;
                totalTax += lineTax;

                lineResults.Add(new TaxCalculationLineResult
                {
                    LineId = line.LineId,
                    TaxableAmount = new Money { Amount = lineTotal, Currency = currency },
                    TaxAmount = new Money { Amount = lineTax, Currency = currency },
                    EffectiveRate = line.IsExempt ? 0m : 0.08m,
                    IsExempt = line.IsExempt,
                    JurisdictionDetails =
                    [
                        new TaxJurisdictionResult
                        {
                            JurisdictionType = "State",
                            JurisdictionName = request.DestinationAddress.StateProvince ?? "Unknown",
                            Rate = 0.0625m,
                            TaxAmount = new Money { Amount = lineTax * 0.78m, Currency = currency }
                        },
                        new TaxJurisdictionResult
                        {
                            JurisdictionType = "County",
                            JurisdictionName = "Local",
                            Rate = 0.0175m,
                            TaxAmount = new Money { Amount = lineTax * 0.22m, Currency = currency }
                        }
                    ]
                });
            }

            var shippingTax = 0m;
            if (request.ShippingAmount != null && request.ShippingAmount.Amount > 0)
            {
                shippingTax = request.ShippingAmount.Amount * 0.08m;
                totalTax += shippingTax;
            }

            var result = new TaxCalculationResult
            {
                Success = true,
                ProviderTransactionId = Guid.NewGuid().ToString(),
                TotalTax = new Money { Amount = totalTax, Currency = currency },
                TaxableAmount = new Money { Amount = totalTaxable, Currency = currency },
                ShippingTax = new Money { Amount = shippingTax, Currency = currency },
                EffectiveRate = 0.08m,
                LineResults = lineResults,
                JurisdictionBreakdown =
                [
                    new TaxJurisdictionResult
                    {
                        JurisdictionType = "State",
                        JurisdictionName = request.DestinationAddress.StateProvince ?? "Unknown",
                        Rate = 0.0625m,
                        TaxAmount = new Money { Amount = totalTax * 0.78m, Currency = currency },
                        TaxableAmount = new Money { Amount = totalTaxable, Currency = currency }
                    },
                    new TaxJurisdictionResult
                    {
                        JurisdictionType = "County",
                        JurisdictionName = "Local",
                        Rate = 0.0175m,
                        TaxAmount = new Money { Amount = totalTax * 0.22m, Currency = currency },
                        TaxableAmount = new Money { Amount = totalTaxable, Currency = currency }
                    }
                ],
                CalculatedAt = DateTimeOffset.UtcNow
            };

            logger.LogInformation(
                "Avalara tax calculation complete: Total tax ${TotalTax} for transaction {TransactionId}",
                result.TotalTax.Amount,
                result.ProviderTransactionId);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating tax with Avalara");
            return TaxCalculationResult.Failed($"Avalara error: {ex.Message}");
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
                "Committing Avalara transaction {TransactionId} for order {OrderNumber}",
                providerTransactionId,
                orderNumber);

            // TODO: Implement actual Avalara commit
            // var result = await client.CommitTransactionAsync(options.CompanyCode, transactionCode, null, model);

            await Task.Delay(10, cancellationToken); // Simulate API call
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error committing Avalara transaction {TransactionId}", providerTransactionId);
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
                "Voiding Avalara transaction {TransactionId}: {Reason}",
                providerTransactionId,
                reason ?? "No reason provided");

            // TODO: Implement actual Avalara void
            // await client.VoidTransactionAsync(options.CompanyCode, transactionCode, null, model);

            await Task.Delay(10, cancellationToken); // Simulate API call
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error voiding Avalara transaction {TransactionId}", providerTransactionId);
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
            return AddressValidationResult.Invalid("Avalara is not configured");
        }

        try
        {
            logger.LogDebug("Validating address with Avalara: {City}, {State}", address.City, address.StateProvince);

            // TODO: Implement actual Avalara address validation
            // var result = await client.ResolveAddressPostAsync(addressInfo);

            await Task.Delay(10, cancellationToken); // Simulate API call

            // For stub, assume address is valid
            return AddressValidationResult.Valid();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating address with Avalara");
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
            // TODO: Implement actual Avalara rate lookup
            // var result = await client.TaxRatesByAddressAsync(address.AddressLine1, ...);

            await Task.Delay(10, cancellationToken); // Simulate API call
            return 0.08m; // Stub: 8% combined rate
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tax rate from Avalara");
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
            logger.LogDebug("Testing Avalara connection");

            // TODO: Implement actual Avalara ping
            // var result = await client.PingAsync();
            // return result.authenticated ?? false;

            await Task.Delay(10, cancellationToken); // Simulate API call
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Avalara connection test failed");
            return false;
        }
    }
}
