using CMS.Commerce;
using CMS.ContentEngine;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Baseline.Ecommerce.Promotions;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.DigitalCommerce;
using Microsoft.Extensions.Logging;

using CmsPriceRequest = CMS.Commerce.PriceCalculationRequest;
using CmsPriceResult = CMS.Commerce.PriceCalculationResult;

[assembly: RegisterPromotionRule<BaselineOrderPromotionRule>(
    identifier: BaselineOrderPromotionRule.IDENTIFIER,
    promotionType: PromotionType.Order,
    name: "Baseline order discount")]

namespace Baseline.Ecommerce.Promotions;

/// <summary>
/// Properties for configuring a Baseline order discount.
/// Extends OrderPromotionRuleProperties for built-in discount and minimum requirement fields.
/// </summary>
public class BaselineOrderPromotionProperties : OrderPromotionRuleProperties
{
    // Base class includes: DiscountValueType, DiscountValue,
    // MinimumRequirementValueType, MinimumRequirementValue

    /// <summary>
    /// Whether this promotion requires membership.
    /// </summary>
    [CheckBoxComponent(
        Label = "Members only",
        ExplanationText = "Only apply to registered members",
        Order = 1)]
    public bool MembersOnly { get; set; }

    /// <summary>
    /// Whether this is for first-time buyers only.
    /// </summary>
    [CheckBoxComponent(
        Label = "First-time buyers only",
        ExplanationText = "Only apply to customers with no previous orders",
        Order = 2)]
    public bool FirstTimeBuyersOnly { get; set; }

    /// <summary>
    /// Specific products required in cart for this promotion.
    /// When selected, overrides category-based requirements.
    /// </summary>
    [ContentItemSelectorComponent(
        typeof(ProductFieldsSchemaFilter),
        Label = "Required products in cart",
        ExplanationText = "Cart must contain at least one of these products. Leave empty for category-based requirements.",
        AllowContentItemCreation = false,
        Order = 3)]
    public IEnumerable<ContentItemReference> RequiredProducts { get; set; } = [];

    /// <summary>
    /// Required product categories in cart.
    /// Hidden when specific products are selected.
    /// </summary>
    [TagSelectorComponent(
        "ProductCategories",
        Label = "Required categories in cart",
        ExplanationText = "Cart must contain at least one item from these categories",
        Order = 4)]
    [VisibleIfEmpty(nameof(RequiredProducts))]
    public IEnumerable<TagReference> RequiredCategories { get; set; } = [];
}

/// <summary>
/// Baseline order promotion rule that applies discounts to entire orders.
/// Supports minimum requirements, member-only, first-time buyer, and category requirements.
/// </summary>
public class BaselineOrderPromotionRule
    : OrderPromotionRule<BaselineOrderPromotionProperties, CmsPriceRequest, CmsPriceResult>
{
    /// <summary>
    /// Unique identifier for this promotion rule.
    /// </summary>
    public const string IDENTIFIER = "Baseline.OrderDiscount";

    private readonly IInfoProvider<CustomerInfo> _customerProvider;
    private readonly IInfoProvider<OrderInfo> _orderProvider;
    private readonly ILogger<BaselineOrderPromotionRule> _logger;

    /// <summary>
    /// Initializes a new instance of the promotion rule.
    /// </summary>
    public BaselineOrderPromotionRule(
        IInfoProvider<CustomerInfo> customerProvider,
        IInfoProvider<OrderInfo> orderProvider,
        ILogger<BaselineOrderPromotionRule> logger)
    {
        _customerProvider = customerProvider;
        _orderProvider = orderProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> IsApplicable(
        IPriceCalculationData<CmsPriceRequest, CmsPriceResult> calculationData,
        CancellationToken cancellationToken)
    {
        var customerId = calculationData.Request.BuyerIdentifier?.Value ?? 0;

        // Check member-only requirement
        if (Properties.MembersOnly)
        {
            var customer = await _customerProvider.GetAsync(customerId, cancellationToken);

            if (customer?.CustomerMemberID is null or 0)
            {
                _logger.LogDebug("Order promotion {PromoId} skipped - requires membership", IDENTIFIER);
                return false;
            }
        }

        // Check first-time buyer requirement
        if (Properties.FirstTimeBuyersOnly)
        {
            if (!await IsFirstTimeBuyerAsync(customerId, cancellationToken))
            {
                _logger.LogDebug("Order promotion {PromoId} skipped - not first-time buyer", IDENTIFIER);
                return false;
            }
        }

        // Check required products in cart (takes priority over categories)
        if (Properties.RequiredProducts.Any())
        {
            if (!HasRequiredProducts(calculationData.Result))
            {
                _logger.LogDebug("Order promotion {PromoId} skipped - missing required products", IDENTIFIER);
                return false;
            }
        }
        // Check required categories in cart (only if no specific products selected)
        else if (Properties.RequiredCategories.Any())
        {
            if (!HasRequiredCategories(calculationData.Result))
            {
                _logger.LogDebug("Order promotion {PromoId} skipped - missing required categories", IDENTIFIER);
                return false;
            }
        }

        // Base class handles minimum purchase/quantity requirements
        return await base.IsApplicable(calculationData, cancellationToken);
    }

    /// <inheritdoc/>
    public override OrderPromotionCandidate GetPromotionCandidate(
        IPriceCalculationData<CmsPriceRequest, CmsPriceResult> calculationData)
    {
        // Calculate order subtotal after catalog discounts
        var orderSubtotal = calculationData.Result.Items
            .Sum(i => i.LineSubtotalAfterLineDiscount);

        // Calculate discount using built-in helper (handles % vs fixed)
        var discountAmount = GetDiscountAmount(orderSubtotal);

        _logger.LogDebug(
            "Order discount {Amount:C} calculated on subtotal {Subtotal:C}",
            discountAmount,
            orderSubtotal);

        return new BaselineOrderPromotionCandidate
        {
            OrderDiscountAmount = discountAmount,
            DisplayLabel = $"{GetDiscountValueLabel()} off"
        };
    }

    /// <summary>
    /// Checks if the customer is a first-time buyer (no previous orders).
    /// </summary>
    private async Task<bool> IsFirstTimeBuyerAsync(int customerId, CancellationToken cancellationToken)
    {
        if (customerId == 0)
        {
            // New customer with no record yet - qualifies as first-time
            return true;
        }

        var existingOrders = await _orderProvider.Get()
            .WhereEquals(nameof(OrderInfo.OrderCustomerID), customerId)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return !existingOrders.Any();
    }

    /// <summary>
    /// Checks if the cart contains items from required categories.
    /// </summary>
    private bool HasRequiredCategories(CmsPriceResult result)
    {
        foreach (var item in result.Items)
        {
            if (item.ProductData is BaselineProductData baselineProduct)
            {
                if (baselineProduct.Categories.Intersect(Properties.RequiredCategories).Any())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the cart contains any of the required products.
    /// </summary>
    private bool HasRequiredProducts(CmsPriceResult result)
    {
        // ContentItemReference.Identifier is a GUID
        var requiredProductGuids = Properties.RequiredProducts
            .Select(p => p.Identifier)
            .ToHashSet();

        foreach (var item in result.Items)
        {
            if (item.ProductData is BaselineProductData baselineProduct)
            {
                if (requiredProductGuids.Contains(baselineProduct.ContentItemGUID))
                {
                    return true;
                }
            }
            // No fallback - cannot match by GUID without BaselineProductData
        }

        return false;
    }
}

/// <summary>
/// Extended order promotion candidate with display label.
/// </summary>
public class BaselineOrderPromotionCandidate : OrderPromotionCandidate
{
    /// <summary>
    /// Display label for the discount (e.g., "10% off" or "$15.00 off").
    /// </summary>
    public string DisplayLabel { get; set; } = string.Empty;
}
