namespace Ecommerce.Models;

/// <summary>
/// ShoppingCartDataModel for Kentico Commerce.
/// Uses Kentico-specific ContentItemId instead of generic ProductId.
/// </summary>
public class ShoppingCartDataModel
{
    /// <summary>
    /// Items inside the shopping cart.
    /// </summary>
    public ICollection<ShoppingCartDataItem> Items { get; init; } = [];

    /// <summary>
    /// Coupon codes entered by the customer.
    /// </summary>
    public ICollection<string> CouponCodes { get; init; } = [];

    /// <summary>
    /// Applied discounts (gift cards, validated coupons) with their calculated amounts.
    /// </summary>
    public ICollection<AppliedDiscountData> AppliedDiscounts { get; init; } = [];
}

/// <summary>
/// Serializable discount data for cart persistence.
/// </summary>
public class AppliedDiscountData
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DiscountType { get; set; } = "FixedAmount";
}

/// <summary>
/// ShoppingCartDataItem for Kentico Commerce.
/// Uses ContentItemId/VariantId for Kentico content item references.
/// </summary>
public class ShoppingCartDataItem
{
    /// <summary>
    /// Identifier of the content item representing a product.
    /// Use -1 for virtual products like gift cards.
    /// </summary>
    public int ContentItemId { get; set; }

    /// <summary>
    /// Quantity of the item in shopping cart.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Identifier of the variant representing specific variant of a product.
    /// </summary>
    public int? VariantId { get; set; }

    /// <summary>
    /// Optional key-value pairs for additional item metadata.
    /// Used for gift cards (amount, recipient info), custom products, etc.
    /// </summary>
    public Dictionary<string, string>? Options { get; set; }
}
