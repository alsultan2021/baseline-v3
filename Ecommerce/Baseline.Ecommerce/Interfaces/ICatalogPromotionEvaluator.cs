namespace Baseline.Ecommerce;

/// <summary>
/// Service for evaluating catalog promotions with a complex rule engine.
/// Supports product targeting, customer targeting, time-based rules, quantity-based discounts,
/// and custom rule types via extensible rule handlers.
/// </summary>
public interface ICatalogPromotionEvaluator
{
    /// <summary>
    /// Evaluates all applicable catalog promotions for a product and returns the best discount.
    /// </summary>
    /// <param name="context">The evaluation context containing product and customer information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result with best applicable promotion.</returns>
    Task<PromotionEvaluationResult> EvaluateAsync(
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates all applicable catalog promotions and returns all matching promotions (not just the best).
    /// Useful for displaying "You could save..." messages or applying stackable discounts.
    /// </summary>
    /// <param name="context">The evaluation context containing product and customer information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching promotions with their discount amounts.</returns>
    Task<IReadOnlyList<PromotionMatch>> EvaluateAllAsync(
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates promotions for multiple products in a cart context.
    /// Supports quantity-based rules like BOGO and tiered discounts.
    /// </summary>
    /// <param name="items">The cart items to evaluate.</param>
    /// <param name="customerContext">Optional customer context for customer-specific promotions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discount results for each item.</returns>
    Task<IReadOnlyDictionary<Guid, PromotionEvaluationResult>> EvaluateCartAsync(
        IEnumerable<CartItemContext> items,
        CustomerPromotionContext? customerContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a custom rule handler for a specific rule type.
    /// </summary>
    /// <param name="ruleType">The rule type identifier (matches PromotionRuleType field).</param>
    /// <param name="handler">The rule handler function.</param>
    void RegisterRuleHandler(string ruleType, IPromotionRuleHandler handler);

    /// <summary>
    /// Gets all registered rule type identifiers.
    /// </summary>
    /// <returns>Collection of registered rule types.</returns>
    IEnumerable<string> GetRegisteredRuleTypes();
}

/// <summary>
/// Handler for evaluating custom promotion rules.
/// </summary>
public interface IPromotionRuleHandler
{
    /// <summary>
    /// Evaluates whether the rule conditions are met.
    /// </summary>
    /// <param name="ruleProperties">JSON properties for the rule configuration.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the rule conditions are met, false otherwise.</returns>
    Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for evaluating catalog promotions.
/// </summary>
public class PromotionEvaluationContext
{
    /// <summary>
    /// Product information for evaluation.
    /// </summary>
    public required ProductPromotionContext Product { get; init; }

    /// <summary>
    /// Optional customer information for customer-specific promotions.
    /// </summary>
    public CustomerPromotionContext? Customer { get; init; }

    /// <summary>
    /// Optional cart context for quantity-based evaluations.
    /// </summary>
    public CartPromotionContext? Cart { get; init; }

    /// <summary>
    /// Evaluation timestamp (defaults to UTC now).
    /// </summary>
    public DateTime EvaluationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Applied coupon codes to consider.
    /// </summary>
    public IReadOnlyList<string> AppliedCoupons { get; init; } = [];

    /// <summary>
    /// Channel context for channel-specific promotions.
    /// </summary>
    public string? ChannelName { get; init; }

    /// <summary>
    /// Additional custom properties for rule evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomProperties { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// Product information for promotion evaluation.
/// </summary>
public class ProductPromotionContext
{
    /// <summary>
    /// Content item ID of the product.
    /// </summary>
    public required int ContentItemId { get; init; }

    /// <summary>
    /// Product GUID for stock/variant lookups.
    /// </summary>
    public Guid? ProductGuid { get; init; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string? SKU { get; init; }

    /// <summary>
    /// Unit price before discounts.
    /// </summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>
    /// Quantity for quantity-based discount calculations.
    /// </summary>
    public decimal Quantity { get; init; } = 1;

    /// <summary>
    /// Product categories (taxonomy codes).
    /// </summary>
    public IReadOnlyList<string> Categories { get; init; } = [];

    /// <summary>
    /// Product tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Tax category for tax-related promotion rules.
    /// </summary>
    public string? TaxCategory { get; init; }

    /// <summary>
    /// Brand/manufacturer name.
    /// </summary>
    public string? Brand { get; init; }

    /// <summary>
    /// Product type code.
    /// </summary>
    public string? ProductType { get; init; }
}

/// <summary>
/// Customer information for promotion evaluation.
/// </summary>
public class CustomerPromotionContext
{
    /// <summary>
    /// Customer/member ID.
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// Kentico member ID.
    /// </summary>
    public int? MemberId { get; init; }

    /// <summary>
    /// Customer email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Customer groups/segments.
    /// </summary>
    public IReadOnlyList<string> CustomerGroups { get; init; } = [];

    /// <summary>
    /// Loyalty tier (e.g., "Gold", "Platinum").
    /// </summary>
    public string? LoyaltyTier { get; init; }

    /// <summary>
    /// Date when customer first registered.
    /// </summary>
    public DateTime? MemberSince { get; init; }

    /// <summary>
    /// Total lifetime purchase amount.
    /// </summary>
    public decimal? LifetimePurchaseAmount { get; init; }

    /// <summary>
    /// Total order count.
    /// </summary>
    public int? TotalOrderCount { get; init; }

    /// <summary>
    /// Whether this is the customer's first purchase.
    /// </summary>
    public bool IsFirstPurchase { get; init; }

    /// <summary>
    /// Customer's preferred currency.
    /// </summary>
    public string? Currency { get; init; }
}

/// <summary>
/// Cart context for quantity-based promotion evaluation.
/// </summary>
public class CartPromotionContext
{
    /// <summary>
    /// Cart subtotal before discounts.
    /// </summary>
    public decimal Subtotal { get; init; }

    /// <summary>
    /// Total item count in cart.
    /// </summary>
    public int TotalItemCount { get; init; }

    /// <summary>
    /// Total quantity of the specific product in cart.
    /// </summary>
    public decimal ProductQuantityInCart { get; init; }

    /// <summary>
    /// All items in the cart for cross-product rules.
    /// </summary>
    public IReadOnlyList<CartItemContext> Items { get; init; } = [];
}

/// <summary>
/// Individual cart item for cart-level promotion evaluation.
/// </summary>
public class CartItemContext
{
    /// <summary>
    /// Content item ID of the product.
    /// </summary>
    public required int ContentItemId { get; init; }

    /// <summary>
    /// Product GUID.
    /// </summary>
    public Guid? ProductGuid { get; init; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string? SKU { get; init; }

    /// <summary>
    /// Unit price before discounts.
    /// </summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>
    /// Quantity in cart.
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Product categories.
    /// </summary>
    public IReadOnlyList<string> Categories { get; init; } = [];

    /// <summary>
    /// Product tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Result of promotion evaluation.
/// </summary>
public record PromotionEvaluationResult(
    bool HasDiscount,
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal DiscountedPrice,
    PromotionMatch? BestMatch,
    IReadOnlyList<PromotionMatch> AllMatches,
    IReadOnlyList<string> RejectionReasons);

/// <summary>
/// A matched promotion with calculated discount.
/// </summary>
public record PromotionMatch(
    int PromotionId,
    Guid PromotionGuid,
    string PromotionName,
    string PromotionDisplayName,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    decimal CalculatedDiscount,
    string DiscountLabel,
    PromotionMatchReason MatchReason,
    bool IsStackable,
    int Priority);

/// <summary>
/// Reason why a promotion matched.
/// </summary>
public enum PromotionMatchReason
{
    /// <summary>Product directly targeted by ID.</summary>
    ProductTarget = 0,

    /// <summary>Product matched by category.</summary>
    CategoryTarget = 1,

    /// <summary>Product matched by tag.</summary>
    TagTarget = 2,

    /// <summary>Product matched by brand.</summary>
    BrandTarget = 3,

    /// <summary>Promotion applies to all products (no targeting).</summary>
    UniversalPromotion = 4,

    /// <summary>Customer-specific promotion.</summary>
    CustomerTarget = 5,

    /// <summary>Quantity-based promotion (BOGO, tiered).</summary>
    QuantityBased = 6,

    /// <summary>Time-based promotion (flash sale).</summary>
    TimeBased = 7,

    /// <summary>Coupon code applied.</summary>
    CouponApplied = 8,

    /// <summary>Custom rule matched.</summary>
    CustomRule = 9
}

/// <summary>
/// Result of custom rule evaluation.
/// </summary>
public record RuleEvaluationResult(
    bool IsMatch,
    string? RejectionReason = null,
    decimal? ModifiedDiscount = null,
    PromotionMatchReason MatchReason = PromotionMatchReason.CustomRule);

/// <summary>
/// Built-in rule types supported by the evaluator.
/// </summary>
public static class PromotionRuleTypes
{
    /// <summary>Buy One Get One rules.</summary>
    public const string BuyOneGetOne = "BOGO";

    /// <summary>Tiered quantity discounts.</summary>
    public const string TieredQuantity = "TIERED_QTY";

    /// <summary>Customer tier targeting.</summary>
    public const string CustomerTier = "CUSTOMER_TIER";

    /// <summary>Customer group targeting.</summary>
    public const string CustomerGroup = "CUSTOMER_GROUP";

    /// <summary>First-time customer discount.</summary>
    public const string FirstPurchase = "FIRST_PURCHASE";

    /// <summary>Time-of-day restrictions.</summary>
    public const string TimeOfDay = "TIME_OF_DAY";

    /// <summary>Day-of-week restrictions.</summary>
    public const string DayOfWeek = "DAY_OF_WEEK";

    /// <summary>SKU pattern matching.</summary>
    public const string SkuPattern = "SKU_PATTERN";

    /// <summary>Minimum spend requirement.</summary>
    public const string MinimumSpend = "MIN_SPEND";

    /// <summary>Bundle discount (requires specific product combinations).</summary>
    public const string BundleDiscount = "BUNDLE";

    /// <summary>Exclusive promotion (cannot be combined).</summary>
    public const string Exclusive = "EXCLUSIVE";
}
