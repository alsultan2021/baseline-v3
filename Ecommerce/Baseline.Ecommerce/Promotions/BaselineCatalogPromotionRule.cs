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

[assembly: RegisterPromotionRule<BaselineCatalogPromotionRule>(
    identifier: BaselineCatalogPromotionRule.IDENTIFIER,
    promotionType: PromotionType.Catalog,
    name: "Baseline catalog discount")]

namespace Baseline.Ecommerce.Promotions;

/// <summary>
/// Properties for configuring a Baseline catalog discount.
/// Extends CatalogPromotionRuleProperties for built-in discount value/type fields.
/// </summary>
public class BaselineCatalogPromotionProperties : CatalogPromotionRuleProperties
{
    /// <summary>
    /// Specific target products to apply discount to.
    /// When selected, overrides category-based targeting.
    /// </summary>
    [ContentItemSelectorComponent(
        typeof(ProductFieldsSchemaFilter),
        Label = "Target products",
        ExplanationText = "Select specific products. Leave empty for category-based targeting.",
        AllowContentItemCreation = false,
        Order = 1)]
    public IEnumerable<ContentItemReference> TargetProducts { get; set; } = [];

    /// <summary>
    /// Product categories eligible for the discount.
    /// Hidden when specific products are selected.
    /// </summary>
    [TagSelectorComponent(
        "ProductCategories",
        Label = "Product categories",
        ExplanationText = "Leave empty to apply to all products",
        Order = 2)]
    [VisibleIfEmpty(nameof(TargetProducts))]
    public IEnumerable<TagReference> ProductCategories { get; set; } = [];

    /// <summary>
    /// Whether this promotion requires membership.
    /// </summary>
    [CheckBoxComponent(
        Label = "Members only",
        ExplanationText = "Only apply to registered members",
        Order = 3)]
    public bool MembersOnly { get; set; }
}

/// <summary>
/// Filter for content item selector to only show content types with ProductFields reusable schema.
/// </summary>
public class ProductFieldsSchemaFilter : IReusableFieldSchemasFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> AllowedSchemaNames => ["ProductFields"];
}

/// <summary>
/// Baseline catalog promotion rule that applies discounts to products.
/// Supports category-based targeting, specific product targeting, and member-only pricing.
/// </summary>
public class BaselineCatalogPromotionRule
    : CatalogPromotionRule<BaselineCatalogPromotionProperties, ProductIdentifier,
        CmsPriceRequest, CmsPriceResult>
{
    /// <summary>
    /// Unique identifier for this promotion rule.
    /// </summary>
    public const string IDENTIFIER = "Baseline.CatalogDiscount";

    private readonly IInfoProvider<CustomerInfo> _customerProvider;
    private readonly ILogger<BaselineCatalogPromotionRule> _logger;

    /// <summary>
    /// Initializes a new instance of the promotion rule.
    /// </summary>
    public BaselineCatalogPromotionRule(
        IInfoProvider<CustomerInfo> customerProvider,
        ILogger<BaselineCatalogPromotionRule> logger)
    {
        _customerProvider = customerProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<bool> IsApplicable(
        IPriceCalculationData<CmsPriceRequest, CmsPriceResult> calculationData,
        CancellationToken cancellationToken)
    {
        // Check member-only requirement
        if (Properties.MembersOnly)
        {
            var customer = await _customerProvider.GetAsync(
                calculationData.Request.BuyerIdentifier?.Value ?? 0, cancellationToken);

            if (customer?.CustomerMemberID is null or 0)
            {
                _logger.LogDebug("Promotion {PromoId} skipped - requires membership", IDENTIFIER);
                return false;
            }
        }

        return await base.IsApplicable(calculationData, cancellationToken);
    }

    /// <inheritdoc/>
    public override CatalogPromotionCandidate? GetPromotionCandidate(
        ProductIdentifier productIdentifier,
        IPriceCalculationData<CmsPriceRequest, CmsPriceResult> calculationData)
    {
        // Get the result item for this product
        var resultItem = calculationData.Result.Items
            .FirstOrDefault(i => i.ProductIdentifier == productIdentifier);

        if (resultItem?.ProductData is null)
        {
            _logger.LogDebug("No product data for {ProductId}", productIdentifier.Identifier);
            return null;
        }

        var productData = resultItem.ProductData;

        // Check if targeting specific products by ContentItemReference
        if (Properties.TargetProducts.Any())
        {
            // ContentItemReference.Identifier is a GUID
            var targetProductGuids = Properties.TargetProducts
                .Select(p => p.Identifier)
                .ToHashSet();

            // Get product's ContentItemGUID from result data
            if (resultItem.ProductData is BaselineProductData baselineData &&
                baselineData.ContentItemGUID != Guid.Empty)
            {
                if (!targetProductGuids.Contains(baselineData.ContentItemGUID))
                {
                    return null;
                }
            }
            else
            {
                // No BaselineProductData - cannot match by GUID, skip targeting
                _logger.LogDebug("Product {ProductId} lacks ContentItemGUID, skipping target check", productIdentifier.Identifier);
            }
        }

        // Check category targeting if categories are specified
        if (Properties.ProductCategories.Any())
        {
            // Try to get categories from extended product data
            var productCategories = GetProductCategories(productData);

            if (!productCategories.Any())
            {
                return null;
            }

            // Check if product is in any eligible category
            var isInEligibleCategory = productCategories
                .Intersect(Properties.ProductCategories)
                .Any();

            if (!isInEligibleCategory)
            {
                return null;
            }
        }

        // Calculate discount using built-in helper (handles % vs fixed)
        var discountAmount = GetDiscountAmount(productData.UnitPrice);

        _logger.LogDebug(
            "Catalog discount {Amount:C} applied to product {ProductId}",
            discountAmount,
            productIdentifier.Identifier);

        return new BaselineCatalogPromotionCandidate
        {
            UnitPriceDiscountAmount = discountAmount,
            DisplayLabel = GetDiscountValueLabel()
        };
    }

    /// <summary>
    /// Extracts product categories from ProductData.
    /// Override in site-specific implementation if using custom ProductData.
    /// </summary>
    protected virtual IEnumerable<TagReference> GetProductCategories(ProductData productData)
    {
        // Check if this is an extended product data with categories
        if (productData is BaselineProductData baselineProduct)
        {
            return baselineProduct.Categories;
        }

        return [];
    }
}

/// <summary>
/// Extended catalog promotion candidate with display label.
/// </summary>
public class BaselineCatalogPromotionCandidate : CatalogPromotionCandidate
{
    /// <summary>
    /// Display label for the discount (e.g., "10% off" or "$5.00 off").
    /// </summary>
    public string DisplayLabel { get; set; } = string.Empty;
}

/// <summary>
/// Extended ProductData that includes category information for promotion evaluation and tax calculation.
/// Extends ExtendedProductData so DefaultTaxPriceCalculationStep can access TaxCategory and IsDigital.
/// </summary>
public record BaselineProductData : ExtendedProductData
{
    /// <summary>
    /// Content item GUID for matching against ContentItemSelector selections.
    /// </summary>
    public Guid ContentItemGUID { get; init; }

    /// <summary>
    /// Product categories for promotion targeting.
    /// </summary>
    public IEnumerable<TagReference> Categories { get; init; } = [];

    /// <summary>
    /// Product tags for additional targeting.
    /// </summary>
    public IEnumerable<TagReference> Tags { get; init; } = [];
}
