using CMS.Commerce;
using Microsoft.Extensions.Logging;

using KenticoRequest = CMS.Commerce.PriceCalculationRequest;
using KenticoResult = CMS.Commerce.PriceCalculationResult;
using KenticoRequestItem = CMS.Commerce.PriceCalculationRequestItem;

namespace Baseline.Ecommerce;

/// <summary>
/// Shared Kentico Digital Commerce promotion evaluation helper.
/// Consolidates duplicated DC request-building/result-parsing from
/// PricingService and PriceCalculationService into a single reusable service.
/// </summary>
public sealed class KenticoDcPromotionHelper(
    IPriceCalculationService<KenticoRequest, KenticoResult> kenticoPriceCalculationService,
    ILogger<KenticoDcPromotionHelper> logger)
{
    /// <summary>
    /// Describes one line-item for Kentico DC evaluation.
    /// Abstracts both <c>ShoppingCartDataModel.Items</c> and <c>PriceCalculationRequestItem</c>.
    /// </summary>
    public readonly record struct LineItem(int ProductId, int Quantity);

    /// <summary>
    /// Aggregate catalog discount result.
    /// </summary>
    public readonly record struct CatalogDiscountResult(
        string Name,
        decimal TotalDiscount,
        IReadOnlyList<ItemCatalogDiscount> PerItemDiscounts);

    /// <summary>
    /// Per-item catalog discount info (used by PricingService for itemized results).
    /// </summary>
    public readonly record struct ItemCatalogDiscount(
        int ProductId,
        string Label,
        decimal UnitDiscount,
        decimal LineDiscount);

    /// <summary>
    /// Evaluates Kentico DC catalog promotions for a set of line items.
    /// Returns <c>null</c> when no catalog promotions apply.
    /// </summary>
    public async Task<CatalogDiscountResult?> GetCatalogPromotionsAsync(
        IReadOnlyList<LineItem> items,
        ICollection<string>? couponCodes,
        int customerId,
        string languageName,
        CancellationToken cancellationToken)
    {
        try
        {
            var kenticoRequest = BuildRequest(
                items, couponCodes, customerId, languageName,
                CMS.Commerce.PriceCalculationMode.Catalog);

            if (kenticoRequest is null)
            {
                return null;
            }

            var kenticoResult = await kenticoPriceCalculationService
                .Calculate(kenticoRequest, cancellationToken);

            decimal totalDiscount = 0;
            var promoNames = new List<string>();
            var perItem = new List<ItemCatalogDiscount>();

            foreach (var item in kenticoResult.Items)
            {
                var applied = item.PromotionData?.CatalogPromotionCandidates?
                    .FirstOrDefault(p => p.Applied);

                if (applied is null)
                {
                    continue;
                }

                var candidate = applied.PromotionCandidate;
                string label = ResolveCatalogLabel(candidate);
                decimal lineDiscount = candidate.UnitPriceDiscountAmount * item.Quantity;
                totalDiscount += lineDiscount;

                if (!promoNames.Contains(label))
                {
                    promoNames.Add(label);
                }

                perItem.Add(new ItemCatalogDiscount(
                    item.ProductIdentifier?.Identifier ?? 0,
                    label,
                    candidate.UnitPriceDiscountAmount,
                    lineDiscount));

                logger.LogDebug(
                    "Kentico DC catalog promotion: product {ProductId}, {Label} = {Amount:C}",
                    item.ProductIdentifier?.Identifier, label, lineDiscount);
            }

            if (totalDiscount > 0)
            {
                return new CatalogDiscountResult(
                    string.Join(", ", promoNames),
                    totalDiscount,
                    perItem);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to evaluate Kentico DC catalog promotions");
        }

        return null;
    }

    /// <summary>
    /// Evaluates Kentico DC order promotions for a set of line items.
    /// Returns <c>null</c> when no order promotions apply.
    /// </summary>
    public async Task<(string Name, decimal Amount)?> GetOrderPromotionAsync(
        IReadOnlyList<LineItem> items,
        ICollection<string>? couponCodes,
        int customerId,
        string languageName,
        CancellationToken cancellationToken)
    {
        try
        {
            var kenticoRequest = BuildRequest(
                items, couponCodes, customerId, languageName,
                CMS.Commerce.PriceCalculationMode.ShoppingCart);

            if (kenticoRequest is null)
            {
                return null;
            }

            var kenticoResult = await kenticoPriceCalculationService
                .Calculate(kenticoRequest, cancellationToken);

            var applied = kenticoResult.PromotionData?.OrderPromotionCandidates?
                .FirstOrDefault(p => p.Applied);

            if (applied is not null)
            {
                var candidate = applied.PromotionCandidate;
                string label = ResolveOrderLabel(candidate);

                logger.LogDebug(
                    "Kentico DC order promotion: {Label} = {Amount:C}",
                    label, candidate.OrderDiscountAmount);

                return (label, candidate.OrderDiscountAmount);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to evaluate Kentico DC order promotions");
        }

        return null;
    }

    #region Private Helpers

    private static KenticoRequest? BuildRequest(
        IReadOnlyList<LineItem> items,
        ICollection<string>? couponCodes,
        int customerId,
        string languageName,
        CMS.Commerce.PriceCalculationMode mode)
    {
        // Filter out items with non-positive quantities — Kentico DC throws
        // ArgumentOutOfRangeException when quantity is zero or negative.
        var validItems = items
            .Where(i => i.Quantity > 0)
            .Select(i => new KenticoRequestItem
            {
                ProductIdentifier = new ProductIdentifier { Identifier = i.ProductId },
                Quantity = i.Quantity
            })
            .ToList();

        if (validItems.Count == 0)
        {
            return null;
        }

        return new KenticoRequest
        {
            Mode = mode,
            LanguageName = languageName,
            BuyerIdentifier = BuyerIdentifier.FromCustomerId(customerId),
            CouponCodes = couponCodes?.ToList() ?? [],
            Items = validItems
        };
    }

    private static string ResolveCatalogLabel(CatalogPromotionCandidate candidate)
    {
        if (candidate is Promotions.BaselineCatalogPromotionCandidate bc
            && !string.IsNullOrEmpty(bc.DisplayLabel))
        {
            return bc.DisplayLabel;
        }
        return "Catalog Discount";
    }

    private static string ResolveOrderLabel(OrderPromotionCandidate candidate)
    {
        if (candidate is Promotions.BaselineOrderPromotionCandidate bo
            && !string.IsNullOrEmpty(bo.DisplayLabel))
        {
            return bo.DisplayLabel;
        }
        return "Order Discount";
    }

    #endregion
}
