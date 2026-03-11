namespace Baseline.Ecommerce;

/// <summary>
/// Represents a shopping cart.
/// </summary>
public class Cart
{
    public Guid Id { get; set; }
    public IList<CartItem> Items { get; set; } = [];
    public IList<AppliedDiscount> Discounts { get; set; } = [];
    public CartTotals Totals { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UserId { get; set; }

    public int ItemCount => Items.Sum(i => i.Quantity);
    public bool IsEmpty => Items.Count == 0;
}

/// <summary>
/// Represents an item in the cart.
/// </summary>
public class CartItem
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = new();
    public Money LineTotal { get; set; } = new();
    public Dictionary<string, string> Options { get; set; } = [];
}

/// <summary>
/// Cart totals breakdown.
/// </summary>
public class CartTotals
{
    public Money Subtotal { get; set; } = Money.Zero();
    public Money Discount { get; set; } = Money.Zero();
    public Money Shipping { get; set; } = Money.Zero();
    public Money Tax { get; set; } = Money.Zero();
    public Money Total { get; set; } = Money.Zero();
}

/// <summary>
/// Applied discount information.
/// </summary>
public class AppliedDiscount
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Money Amount { get; set; } = Money.Zero();
    public DiscountType Type { get; set; }
}

/// <summary>
/// Types of discounts.
/// </summary>
public enum DiscountType
{
    Percentage,
    FixedAmount,
    FreeShipping,
    BuyXGetY,
    GiftCard,
    /// <summary>
    /// Discount from a Kentico DC promotion activated via coupon code.
    /// </summary>
    KenticoDcCoupon
}

/// <summary>
/// Request to add an item to cart.
/// </summary>
public record AddToCartRequest
{
    public required int ProductId { get; init; }
    public int Quantity { get; init; } = 1;
    public Dictionary<string, string>? Options { get; init; }
}

/// <summary>
/// Result of a cart operation.
/// </summary>
public record CartResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Cart? Cart { get; init; }

    public static CartResult Succeeded(Cart cart) => new() { Success = true, Cart = cart };
    public static CartResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of applying a discount.
/// </summary>
public record DiscountResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public AppliedDiscount? Discount { get; init; }
    public Cart? Cart { get; init; }

    public static DiscountResult Succeeded(AppliedDiscount discount, Cart cart) =>
        new() { Success = true, Discount = discount, Cart = cart };
    public static DiscountResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of cart validation.
/// </summary>
public record CartValidationResult
{
    public bool IsValid { get; init; }
    public IEnumerable<CartValidationError> Errors { get; init; } = [];

    public static CartValidationResult Valid() => new() { IsValid = true };
    public static CartValidationResult Invalid(IEnumerable<CartValidationError> errors) =>
        new() { IsValid = false, Errors = errors };
}

/// <summary>
/// Cart validation error.
/// </summary>
public record CartValidationError(Guid? CartItemId, string ErrorCode, string Message);
