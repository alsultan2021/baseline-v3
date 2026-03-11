namespace Ecommerce.Models;

/// <summary>
/// Money class with FromAmount factory method.
/// Wraps the Baseline.Ecommerce.Money record for .
/// </summary>
public static class Money
{
    /// <summary>
    /// Creates a Money value from a decimal amount with default USD currency.
    /// </summary>
    public static Baseline.Ecommerce.Money FromAmount(decimal amount, string currency = "USD")
        => new() { Amount = amount, Currency = currency };

    /// <summary>
    /// Creates a zero Money value.
    /// </summary>
    public static Baseline.Ecommerce.Money Zero(string currency = "USD")
        => Baseline.Ecommerce.Money.Zero(currency);
}
