namespace Baseline.Ecommerce;

/// <summary>
/// Represents a monetary value.
/// </summary>
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";

    public static Money Zero(string currency = "USD") => new() { Amount = 0, Currency = currency };

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        return new Money { Amount = a.Amount + b.Amount, Currency = a.Currency };
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        return new Money { Amount = a.Amount - b.Amount, Currency = a.Currency };
    }

    public static Money operator *(Money a, decimal multiplier)
        => new() { Amount = a.Amount * multiplier, Currency = a.Currency };
}
