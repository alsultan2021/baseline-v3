namespace Baseline.Ecommerce;

/// <summary>
/// Service for managing currency operations.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Gets all enabled currencies.
    /// </summary>
    Task<IEnumerable<Currency>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currencies including disabled ones.
    /// </summary>
    Task<IEnumerable<Currency>> GetAllCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default currency.
    /// </summary>
    Task<Currency?> GetDefaultCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a currency by its code (e.g., "USD", "EUR", "CAD").
    /// </summary>
    Task<Currency?> GetCurrencyByCodeAsync(string currencyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a currency by its ID.
    /// </summary>
    Task<Currency?> GetCurrencyByIdAsync(int currencyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats an amount using the currency's format pattern.
    /// Note: This synchronous method cannot read channel settings.
    /// Use FormatAmountWithSettingsAsync for channel-aware formatting.
    /// </summary>
    string FormatAmount(decimal amount, Currency currency);

    /// <summary>
    /// Formats an amount using the default currency with channel settings.
    /// </summary>
    Task<string> FormatAmountAsync(decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats an amount using the specified currency with channel settings.
    /// Uses channel settings for symbol position, decimal places, and separators.
    /// </summary>
    Task<string> FormatAmountWithSettingsAsync(decimal amount, Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rounds an amount according to the currency's decimal places.
    /// </summary>
    decimal RoundAmount(decimal amount, Currency currency);
}

/// <summary>
/// Service for managing currency exchange rate operations.
/// </summary>
public interface ICurrencyExchangeService
{
    /// <summary>
    /// Gets the exchange rate between two currencies.
    /// </summary>
    Task<CurrencyExchangeRate?> GetExchangeRateAsync(
        string fromCurrencyCode,
        string toCurrencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all exchange rates for a source currency.
    /// </summary>
    Task<IEnumerable<CurrencyExchangeRate>> GetExchangeRatesFromAsync(
        string fromCurrencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    Task<Money> ConvertAsync(
        Money amount,
        string toCurrencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount from one currency to another using a specific exchange rate.
    /// </summary>
    Money Convert(Money amount, CurrencyExchangeRate exchangeRate);

    /// <summary>
    /// Gets all exchange rates.
    /// </summary>
    Task<IEnumerable<CurrencyExchangeRate>> GetAllExchangeRatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount to the default currency.
    /// </summary>
    Task<Money> ConvertToDefaultAsync(Money amount, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing tax class operations.
/// </summary>
public interface ITaxClassService
{
    /// <summary>
    /// Gets all enabled tax classes.
    /// </summary>
    Task<IEnumerable<TaxClass>> GetTaxClassesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tax classes including disabled ones.
    /// </summary>
    Task<IEnumerable<TaxClass>> GetAllTaxClassesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default tax class.
    /// </summary>
    Task<TaxClass?> GetDefaultTaxClassAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tax class by its code name.
    /// </summary>
    Task<TaxClass?> GetTaxClassByCodeAsync(string taxClassCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tax class by its ID.
    /// </summary>
    Task<TaxClass?> GetTaxClassByIdAsync(int taxClassId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tax class by its GUID.
    /// </summary>
    Task<TaxClass?> GetTaxClassByGuidAsync(Guid taxClassGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates tax amount for a given subtotal using the tax class.
    /// </summary>
    Task<Money> CalculateTaxAsync(Money subtotal, int taxClassId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates tax amount using the default tax class.
    /// </summary>
    Task<Money> CalculateTaxAsync(Money subtotal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective tax rate for a tax class, considering any regional overrides.
    /// </summary>
    Task<decimal> GetEffectiveTaxRateAsync(
        int taxClassId,
        string? regionCode = null,
        CancellationToken cancellationToken = default);
}

#region DTOs

/// <summary>
/// Represents a currency for use in business logic.
/// </summary>
public class Currency
{
    /// <summary>
    /// Currency ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Currency GUID.
    /// </summary>
    public Guid Guid { get; init; }

    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR", "CAD").
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "US Dollar", "Euro").
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Currency symbol (e.g., "$", "€", "£").
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Number of decimal places for this currency.
    /// </summary>
    public int DecimalPlaces { get; init; } = 2;

    /// <summary>
    /// Format pattern for displaying amounts (e.g., "${0:N2}").
    /// </summary>
    public string FormatPattern { get; init; } = "${0:N2}";

    /// <summary>
    /// Whether this is the default currency.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Whether this currency is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    public int Order { get; init; }
}

/// <summary>
/// Represents a currency exchange rate for use in business logic.
/// </summary>
public class CurrencyExchangeRate
{
    /// <summary>
    /// Exchange rate ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Exchange rate GUID.
    /// </summary>
    public Guid Guid { get; init; }

    /// <summary>
    /// Source currency code (convert FROM).
    /// </summary>
    public string FromCurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// Target currency code (convert TO).
    /// </summary>
    public string ToCurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// Source currency.
    /// </summary>
    public Currency? FromCurrency { get; init; }

    /// <summary>
    /// Target currency.
    /// </summary>
    public Currency? ToCurrency { get; init; }

    /// <summary>
    /// Exchange rate multiplier. To convert: targetAmount = sourceAmount * Rate.
    /// </summary>
    public decimal Rate { get; init; } = 1m;

    /// <summary>
    /// Date when this exchange rate becomes effective.
    /// </summary>
    public DateTime? EffectiveFrom { get; init; }

    /// <summary>
    /// Date when this exchange rate expires (null = no expiry).
    /// </summary>
    public DateTime? EffectiveTo { get; init; }

    /// <summary>
    /// Whether this exchange rate is currently active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    public int Order { get; init; }
}

/// <summary>
/// Represents a tax class for use in business logic.
/// </summary>
public class TaxClass
{
    /// <summary>
    /// Tax class ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Tax class GUID.
    /// </summary>
    public Guid Guid { get; init; }

    /// <summary>
    /// Tax class code name.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Default tax rate for this class (as a decimal, e.g., 0.10 for 10%).
    /// </summary>
    public decimal DefaultRate { get; init; }

    /// <summary>
    /// Whether this is the default tax class.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Whether this tax class is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets the tax rate formatted as a percentage string.
    /// </summary>
    public string FormattedRate => $"{DefaultRate * 100:0.##}%";
}

#endregion
