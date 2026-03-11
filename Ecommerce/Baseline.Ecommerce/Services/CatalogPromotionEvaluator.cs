using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using CMS.Websites.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation of <see cref="ICatalogPromotionEvaluator"/> with a complex rule engine.
/// Supports product targeting, customer targeting, time-based rules, quantity-based discounts,
/// and custom rule types via extensible rule handlers.
/// </summary>
public class CatalogPromotionEvaluator : ICatalogPromotionEvaluator
{
    private readonly IInfoProvider<PromotionInfo> promotionProvider;
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly IMemoryCache cache;
    private readonly ILogger<CatalogPromotionEvaluator> logger;

    private readonly ConcurrentDictionary<string, IPromotionRuleHandler> ruleHandlers = new(StringComparer.OrdinalIgnoreCase);

    private const string ActivePromotionsCacheKey = "baseline.ecommerce.catalogpromotions.evaluator";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Creates a new instance of <see cref="CatalogPromotionEvaluator"/>.
    /// </summary>
    public CatalogPromotionEvaluator(
        IInfoProvider<PromotionInfo> promotionProvider,
        IWebsiteChannelContext websiteChannelContext,
        IMemoryCache cache,
        ILogger<CatalogPromotionEvaluator> logger)
    {
        this.promotionProvider = promotionProvider;
        this.websiteChannelContext = websiteChannelContext;
        this.cache = cache;
        this.logger = logger;

        // Register built-in rule handlers
        RegisterBuiltInHandlers();
    }

    /// <inheritdoc/>
    public async Task<PromotionEvaluationResult> EvaluateAsync(
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var allMatches = await EvaluateAllAsync(context, cancellationToken);
        var rejectionReasons = new List<string>();

        if (allMatches.Count == 0)
        {
            return new PromotionEvaluationResult(
                false,
                context.Product.UnitPrice,
                0m,
                context.Product.UnitPrice,
                null,
                [],
                rejectionReasons);
        }

        // Find best non-stackable match (highest priority, then highest discount)
        var bestMatch = allMatches
            .OrderByDescending(m => m.Priority)
            .ThenByDescending(m => m.CalculatedDiscount)
            .First();

        // Calculate total discount considering stacking rules
        var totalDiscount = CalculateTotalDiscount(allMatches, bestMatch, context.Product.UnitPrice);
        var discountedPrice = Math.Max(0, context.Product.UnitPrice - totalDiscount);

        return new PromotionEvaluationResult(
            true,
            context.Product.UnitPrice,
            totalDiscount,
            discountedPrice,
            bestMatch,
            allMatches,
            rejectionReasons);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PromotionMatch>> EvaluateAllAsync(
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var promotions = await GetActiveCatalogPromotionsAsync(cancellationToken);
        var matches = new List<PromotionMatch>();

        foreach (var promotion in promotions)
        {
            var match = await EvaluatePromotionAsync(promotion, context, cancellationToken);
            if (match is not null)
            {
                matches.Add(match);
            }
        }

        return matches.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, PromotionEvaluationResult>> EvaluateCartAsync(
        IEnumerable<CartItemContext> items,
        CustomerPromotionContext? customerContext = null,
        CancellationToken cancellationToken = default)
    {
        var itemsList = items.ToList();
        var results = new Dictionary<Guid, PromotionEvaluationResult>();

        // Build cart context
        var cartContext = new CartPromotionContext
        {
            Subtotal = itemsList.Sum(i => i.UnitPrice * i.Quantity),
            TotalItemCount = itemsList.Sum(i => (int)i.Quantity),
            Items = itemsList
        };

        foreach (var item in itemsList)
        {
            if (item.ProductGuid is null)
            {
                continue;
            }

            var productContext = new ProductPromotionContext
            {
                ContentItemId = item.ContentItemId,
                ProductGuid = item.ProductGuid,
                SKU = item.SKU,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Categories = item.Categories,
                Tags = item.Tags
            };

            var itemCartContext = new CartPromotionContext
            {
                Subtotal = cartContext.Subtotal,
                TotalItemCount = cartContext.TotalItemCount,
                ProductQuantityInCart = item.Quantity,
                Items = cartContext.Items
            };

            var evalContext = new PromotionEvaluationContext
            {
                Product = productContext,
                Customer = customerContext,
                Cart = itemCartContext
            };

            var result = await EvaluateAsync(evalContext, cancellationToken);
            results[item.ProductGuid.Value] = result;
        }

        return results;
    }

    /// <inheritdoc/>
    public void RegisterRuleHandler(string ruleType, IPromotionRuleHandler handler)
    {
        ruleHandlers[ruleType] = handler;
        logger.LogInformation("Registered custom rule handler for type: {RuleType}", ruleType);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetRegisteredRuleTypes() => ruleHandlers.Keys;

    private async Task<IReadOnlyList<PromotionInfo>> GetActiveCatalogPromotionsAsync(CancellationToken cancellationToken)
    {
        // Skip cache in preview mode
        if (websiteChannelContext.IsPreview)
        {
            return await LoadActivePromotionsAsync(cancellationToken);
        }

        return await cache.GetOrCreateAsync(ActivePromotionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;
            return await LoadActivePromotionsAsync(cancellationToken);
        }) ?? [];
    }

    private async Task<List<PromotionInfo>> LoadActivePromotionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var promotions = await promotionProvider
            .Get()
            .WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.Catalog)
            .WhereEquals(nameof(PromotionInfo.PromotionEnabled), true)
            .WhereLessOrEquals(nameof(PromotionInfo.PromotionActiveFrom), now)
            .Where(w => w
                .WhereNull(nameof(PromotionInfo.PromotionActiveTo))
                .Or()
                .WhereGreaterOrEquals(nameof(PromotionInfo.PromotionActiveTo), now))
            .OrderByDescending(nameof(PromotionInfo.PromotionOrder))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return promotions.ToList();
    }

    private async Task<PromotionMatch?> EvaluatePromotionAsync(
        PromotionInfo promotion,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Check basic product targeting
            var targetingResult = EvaluateProductTargeting(promotion, context.Product);
            if (!targetingResult.IsMatch)
            {
                return null;
            }

            // Step 2: Evaluate custom rule if specified
            if (!string.IsNullOrEmpty(promotion.PromotionRuleType))
            {
                var ruleResult = await EvaluateCustomRuleAsync(promotion, context, cancellationToken);
                if (!ruleResult.IsMatch)
                {
                    return null;
                }

                // Allow rule to modify the match reason
                if (ruleResult.MatchReason != PromotionMatchReason.CustomRule)
                {
                    targetingResult = targetingResult with { MatchReason = ruleResult.MatchReason };
                }
            }

            // Step 3: Calculate discount
            var discount = CalculateDiscount(
                promotion,
                context.Product.UnitPrice,
                context.Product.Quantity,
                context.Cart?.ProductQuantityInCart ?? context.Product.Quantity);

            if (discount <= 0)
            {
                return null;
            }

            // Step 4: Build discount label
            var discountLabel = BuildDiscountLabel(promotion);

            // Step 5: Determine stacking and priority
            var isStackable = DetermineStackability(promotion);
            var priority = promotion.PromotionOrder;

            return new PromotionMatch(
                promotion.PromotionID,
                promotion.PromotionGuid,
                promotion.PromotionName,
                promotion.PromotionDisplayName,
                (PromotionDiscountType)promotion.PromotionDiscountType,
                promotion.PromotionDiscountValue,
                discount,
                discountLabel,
                targetingResult.MatchReason,
                isStackable,
                priority);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating promotion {PromotionId}: {Message}",
                promotion.PromotionID, ex.Message);
            return null;
        }
    }

    private (bool IsMatch, PromotionMatchReason MatchReason) EvaluateProductTargeting(
        PromotionInfo promotion,
        ProductPromotionContext product)
    {
        // Parse target product IDs
        var targetProductIds = ParseIntJsonArray(promotion.PromotionTargetProducts);
        var targetCategories = ParseJsonArray(promotion.PromotionTargetCategories);

        // Check specific product targeting first
        if (targetProductIds.Any())
        {
            if (targetProductIds.Contains(product.ContentItemId))
            {
                return (true, PromotionMatchReason.ProductTarget);
            }
            // If specific products are targeted but this isn't one, no match
            return (false, PromotionMatchReason.ProductTarget);
        }

        // Check category targeting
        if (targetCategories.Any())
        {
            var productCategories = product.Categories ?? [];
            if (targetCategories.Intersect(productCategories, StringComparer.OrdinalIgnoreCase).Any())
            {
                return (true, PromotionMatchReason.CategoryTarget);
            }
            // If categories are targeted but product doesn't match, no match
            return (false, PromotionMatchReason.CategoryTarget);
        }

        // No targeting means universal promotion
        return (true, PromotionMatchReason.UniversalPromotion);
    }

    private async Task<RuleEvaluationResult> EvaluateCustomRuleAsync(
        PromotionInfo promotion,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        var ruleType = promotion.PromotionRuleType;

        if (!ruleHandlers.TryGetValue(ruleType, out var handler))
        {
            logger.LogWarning("No handler registered for rule type: {RuleType}", ruleType);
            return new RuleEvaluationResult(true); // Default to match if handler not found
        }

        return await handler.EvaluateAsync(promotion.PromotionRuleProperties, context, cancellationToken);
    }

    private static decimal CalculateDiscount(
        PromotionInfo promotion,
        decimal unitPrice,
        decimal quantity,
        decimal totalQuantityInCart)
    {
        var baseDiscount = (PromotionDiscountType)promotion.PromotionDiscountType switch
        {
            PromotionDiscountType.Percentage => Math.Round(unitPrice * (promotion.PromotionDiscountValue / 100m), 2),
            PromotionDiscountType.FixedAmount => Math.Min(promotion.PromotionDiscountValue, unitPrice),
            _ => 0m
        };

        // Check for quantity-based rule modifications
        if (!string.IsNullOrEmpty(promotion.PromotionRuleType))
        {
            var ruleType = promotion.PromotionRuleType;
            var ruleProps = promotion.PromotionRuleProperties;

            if (ruleType.Equals(PromotionRuleTypes.TieredQuantity, StringComparison.OrdinalIgnoreCase))
            {
                baseDiscount = CalculateTieredDiscount(ruleProps, unitPrice, totalQuantityInCart) ?? baseDiscount;
            }
            else if (ruleType.Equals(PromotionRuleTypes.BuyOneGetOne, StringComparison.OrdinalIgnoreCase))
            {
                baseDiscount = CalculateBogoDiscount(ruleProps, unitPrice, totalQuantityInCart) ?? baseDiscount;
            }
        }

        return baseDiscount;
    }

    private static decimal? CalculateTieredDiscount(string? rulePropsJson, decimal unitPrice, decimal quantity)
    {
        if (string.IsNullOrEmpty(rulePropsJson))
        {
            return null;
        }

        try
        {
            var props = JsonSerializer.Deserialize<TieredQuantityRuleProperties>(rulePropsJson);
            if (props?.Tiers is null || props.Tiers.Count == 0)
            {
                return null;
            }

            // Find highest applicable tier
            var applicableTier = props.Tiers
                .Where(t => quantity >= t.MinQuantity)
                .OrderByDescending(t => t.MinQuantity)
                .FirstOrDefault();

            if (applicableTier is null)
            {
                return null;
            }

            return applicableTier.DiscountType == "percentage"
                ? Math.Round(unitPrice * (applicableTier.DiscountValue / 100m), 2)
                : Math.Min(applicableTier.DiscountValue, unitPrice);
        }
        catch
        {
            return null;
        }
    }

    private static decimal? CalculateBogoDiscount(string? rulePropsJson, decimal unitPrice, decimal quantity)
    {
        if (string.IsNullOrEmpty(rulePropsJson))
        {
            return null;
        }

        try
        {
            var props = JsonSerializer.Deserialize<BogoRuleProperties>(rulePropsJson);
            if (props is null)
            {
                return null;
            }

            // Calculate how many free items based on quantity
            // e.g., Buy 2 Get 1 Free: for every 3 items, 1 is free
            var buyQty = props.BuyQuantity;
            var getQty = props.GetQuantity;
            var discountPercentage = props.GetDiscountPercentage ?? 100m; // Default 100% = free

            if (buyQty <= 0 || getQty <= 0)
            {
                return null;
            }

            var totalInGroup = buyQty + getQty;
            var completeGroups = (int)(quantity / totalInGroup);
            var freeItems = completeGroups * getQty;

            if (freeItems <= 0)
            {
                return null;
            }

            // Discount per item for free items
            var discountPerItem = unitPrice * (discountPercentage / 100m);

            // Average discount per item in cart
            return Math.Round((freeItems * discountPerItem) / quantity, 2);
        }
        catch
        {
            return null;
        }
    }

    private static decimal CalculateTotalDiscount(
        IReadOnlyList<PromotionMatch> matches,
        PromotionMatch bestMatch,
        decimal originalPrice)
    {
        // Check if best match is exclusive (not stackable)
        if (!bestMatch.IsStackable)
        {
            return bestMatch.CalculatedDiscount;
        }

        // Stack all stackable discounts
        var stackableMatches = matches.Where(m => m.IsStackable).ToList();
        var totalDiscount = stackableMatches.Sum(m => m.CalculatedDiscount);

        // Don't exceed original price
        return Math.Min(totalDiscount, originalPrice);
    }

    private static string BuildDiscountLabel(PromotionInfo promotion)
    {
        var discountType = (PromotionDiscountType)promotion.PromotionDiscountType;

        return discountType switch
        {
            PromotionDiscountType.Percentage => $"{promotion.PromotionDiscountValue}% off",
            PromotionDiscountType.FixedAmount => $"${promotion.PromotionDiscountValue:F2} off",
            _ => "Discount applied"
        };
    }

    private static bool DetermineStackability(PromotionInfo promotion)
    {
        // Check rule properties for exclusivity
        if (!string.IsNullOrEmpty(promotion.PromotionRuleType) &&
            promotion.PromotionRuleType.Equals(PromotionRuleTypes.Exclusive, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check if rule properties specify stacking
        if (!string.IsNullOrEmpty(promotion.PromotionRuleProperties))
        {
            try
            {
                using var doc = JsonDocument.Parse(promotion.PromotionRuleProperties);
                if (doc.RootElement.TryGetProperty("isStackable", out var stackProp))
                {
                    return stackProp.GetBoolean();
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        // Default to stackable
        return true;
    }

    private void RegisterBuiltInHandlers()
    {
        ruleHandlers[PromotionRuleTypes.BuyOneGetOne] = new BogoRuleHandler();
        ruleHandlers[PromotionRuleTypes.TieredQuantity] = new TieredQuantityRuleHandler();
        ruleHandlers[PromotionRuleTypes.CustomerTier] = new CustomerTierRuleHandler();
        ruleHandlers[PromotionRuleTypes.CustomerGroup] = new CustomerGroupRuleHandler();
        ruleHandlers[PromotionRuleTypes.FirstPurchase] = new FirstPurchaseRuleHandler();
        ruleHandlers[PromotionRuleTypes.TimeOfDay] = new TimeOfDayRuleHandler();
        ruleHandlers[PromotionRuleTypes.DayOfWeek] = new DayOfWeekRuleHandler();
        ruleHandlers[PromotionRuleTypes.SkuPattern] = new SkuPatternRuleHandler();
        ruleHandlers[PromotionRuleTypes.MinimumSpend] = new MinimumSpendRuleHandler();
        ruleHandlers[PromotionRuleTypes.BundleDiscount] = new BundleDiscountRuleHandler();
        ruleHandlers[PromotionRuleTypes.Exclusive] = new ExclusiveRuleHandler();
    }

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<int> ParseIntJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

#region Rule Properties Models

internal class TieredQuantityRuleProperties
{
    public List<TieredQuantityTier>? Tiers { get; set; }
}

internal class TieredQuantityTier
{
    public decimal MinQuantity { get; set; }
    public string DiscountType { get; set; } = "percentage";
    public decimal DiscountValue { get; set; }
}

internal class BogoRuleProperties
{
    public int BuyQuantity { get; set; } = 1;
    public int GetQuantity { get; set; } = 1;
    public decimal? GetDiscountPercentage { get; set; } = 100m;
}

internal class CustomerTierRuleProperties
{
    public List<string>? AllowedTiers { get; set; }
}

internal class CustomerGroupRuleProperties
{
    public List<string>? AllowedGroups { get; set; }
}

internal class TimeOfDayRuleProperties
{
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? TimeZone { get; set; }
}

internal class DayOfWeekRuleProperties
{
    public List<DayOfWeek>? AllowedDays { get; set; }
}

internal class SkuPatternRuleProperties
{
    public string? Pattern { get; set; }
    public bool IsRegex { get; set; }
}

internal class MinimumSpendRuleProperties
{
    public decimal MinimumAmount { get; set; }
    public bool UseCartTotal { get; set; } = true;
}

internal class BundleDiscountRuleProperties
{
    public List<int>? RequiredProductIds { get; set; }
    public List<string>? RequiredCategories { get; set; }
    public int MinimumBundleSize { get; set; } = 2;
}

#endregion

#region Built-in Rule Handlers

internal class BogoRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.QuantityBased));
        }

        try
        {
            var props = JsonSerializer.Deserialize<BogoRuleProperties>(ruleProperties);
            if (props is null)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var quantity = context.Cart?.ProductQuantityInCart ?? context.Product.Quantity;
            var totalRequired = props.BuyQuantity + props.GetQuantity;

            if (quantity < totalRequired)
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Need {totalRequired} items for BOGO, have {quantity}"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.QuantityBased));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class TieredQuantityRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<TieredQuantityRuleProperties>(ruleProperties);
            if (props?.Tiers is null || props.Tiers.Count == 0)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var quantity = context.Cart?.ProductQuantityInCart ?? context.Product.Quantity;
            var minTier = props.Tiers.Min(t => t.MinQuantity);

            if (quantity < minTier)
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Need at least {minTier} items for tiered discount"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.QuantityBased));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class CustomerTierRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (context.Customer is null)
        {
            return Task.FromResult(new RuleEvaluationResult(false, "Customer context required"));
        }

        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<CustomerTierRuleProperties>(ruleProperties);
            if (props?.AllowedTiers is null || props.AllowedTiers.Count == 0)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var customerTier = context.Customer.LoyaltyTier;
            if (string.IsNullOrEmpty(customerTier))
            {
                return Task.FromResult(new RuleEvaluationResult(false, "Customer has no loyalty tier"));
            }

            if (!props.AllowedTiers.Contains(customerTier, StringComparer.OrdinalIgnoreCase))
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Customer tier '{customerTier}' not eligible"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.CustomerTarget));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class CustomerGroupRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (context.Customer is null)
        {
            return Task.FromResult(new RuleEvaluationResult(false, "Customer context required"));
        }

        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<CustomerGroupRuleProperties>(ruleProperties);
            if (props?.AllowedGroups is null || props.AllowedGroups.Count == 0)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var customerGroups = context.Customer.CustomerGroups ?? [];
            if (!customerGroups.Any())
            {
                return Task.FromResult(new RuleEvaluationResult(false, "Customer has no groups"));
            }

            if (!props.AllowedGroups.Intersect(customerGroups, StringComparer.OrdinalIgnoreCase).Any())
            {
                return Task.FromResult(new RuleEvaluationResult(false, "Customer not in eligible group"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.CustomerTarget));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class FirstPurchaseRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (context.Customer is null)
        {
            // Allow guest checkout as first purchase
            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.CustomerTarget));
        }

        if (!context.Customer.IsFirstPurchase)
        {
            return Task.FromResult(new RuleEvaluationResult(false, "Not a first-time customer"));
        }

        return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.CustomerTarget));
    }
}

internal class TimeOfDayRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<TimeOfDayRuleProperties>(ruleProperties);
            if (props is null || (!props.StartTime.HasValue && !props.EndTime.HasValue))
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var now = context.EvaluationTime;

            // Apply timezone if specified
            if (!string.IsNullOrEmpty(props.TimeZone))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(props.TimeZone);
                    now = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
                }
                catch
                {
                    // Fall back to UTC
                }
            }

            var currentTime = now.TimeOfDay;

            if (props.StartTime.HasValue && currentTime < props.StartTime.Value)
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Promotion active from {props.StartTime.Value}"));
            }

            if (props.EndTime.HasValue && currentTime > props.EndTime.Value)
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Promotion ended at {props.EndTime.Value}"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.TimeBased));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class DayOfWeekRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<DayOfWeekRuleProperties>(ruleProperties);
            if (props?.AllowedDays is null || props.AllowedDays.Count == 0)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var today = context.EvaluationTime.DayOfWeek;

            if (!props.AllowedDays.Contains(today))
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Promotion not active on {today}"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.TimeBased));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class SkuPatternRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<SkuPatternRuleProperties>(ruleProperties);
            if (props is null || string.IsNullOrEmpty(props.Pattern))
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var sku = context.Product.SKU;
            if (string.IsNullOrEmpty(sku))
            {
                return Task.FromResult(new RuleEvaluationResult(false, "Product has no SKU"));
            }

            bool matches;
            if (props.IsRegex)
            {
                try
                {
                    matches = Regex.IsMatch(sku, props.Pattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    matches = false;
                }
            }
            else
            {
                matches = sku.Contains(props.Pattern, StringComparison.OrdinalIgnoreCase);
            }

            if (!matches)
            {
                return Task.FromResult(new RuleEvaluationResult(false, "SKU does not match pattern"));
            }

            return Task.FromResult(new RuleEvaluationResult(true, null, null, PromotionMatchReason.ProductTarget));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class MinimumSpendRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<MinimumSpendRuleProperties>(ruleProperties);
            if (props is null || props.MinimumAmount <= 0)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var spendAmount = props.UseCartTotal && context.Cart is not null
                ? context.Cart.Subtotal
                : context.Product.UnitPrice * context.Product.Quantity;

            if (spendAmount < props.MinimumAmount)
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Minimum spend of ${props.MinimumAmount:F2} required"));
            }

            return Task.FromResult(new RuleEvaluationResult(true));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class BundleDiscountRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (context.Cart is null)
        {
            return Task.FromResult(new RuleEvaluationResult(false, "Cart context required for bundle discounts"));
        }

        if (string.IsNullOrEmpty(ruleProperties))
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }

        try
        {
            var props = JsonSerializer.Deserialize<BundleDiscountRuleProperties>(ruleProperties);
            if (props is null)
            {
                return Task.FromResult(new RuleEvaluationResult(true));
            }

            var cartItems = context.Cart.Items ?? [];

            // Check required product IDs
            if (props.RequiredProductIds is not null && props.RequiredProductIds.Count > 0)
            {
                var cartProductIds = cartItems.Select(i => i.ContentItemId).ToHashSet();
                var missingProducts = props.RequiredProductIds.Except(cartProductIds).ToList();

                if (missingProducts.Any())
                {
                    return Task.FromResult(new RuleEvaluationResult(
                        false,
                        $"Bundle requires {missingProducts.Count} more products"));
                }
            }

            // Check required categories
            if (props.RequiredCategories is not null && props.RequiredCategories.Count > 0)
            {
                var cartCategories = cartItems
                    .SelectMany(i => i.Categories)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingCategories = props.RequiredCategories
                    .Where(c => !cartCategories.Contains(c))
                    .ToList();

                if (missingCategories.Any())
                {
                    return Task.FromResult(new RuleEvaluationResult(
                        false,
                        $"Bundle requires items from: {string.Join(", ", missingCategories)}"));
                }
            }

            // Check minimum bundle size
            if (context.Cart.TotalItemCount < props.MinimumBundleSize)
            {
                return Task.FromResult(new RuleEvaluationResult(
                    false,
                    $"Bundle requires at least {props.MinimumBundleSize} items"));
            }

            return Task.FromResult(new RuleEvaluationResult(true));
        }
        catch
        {
            return Task.FromResult(new RuleEvaluationResult(true));
        }
    }
}

internal class ExclusiveRuleHandler : IPromotionRuleHandler
{
    public Task<RuleEvaluationResult> EvaluateAsync(
        string? ruleProperties,
        PromotionEvaluationContext context,
        CancellationToken cancellationToken)
    {
        // Exclusive promotions always match (the exclusivity is handled in stacking logic)
        return Task.FromResult(new RuleEvaluationResult(true));
    }
}

#endregion
