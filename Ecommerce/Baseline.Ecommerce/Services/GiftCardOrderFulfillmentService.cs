using CMS.Commerce;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Interface for gift card order fulfillment.
/// Handles automatic gift card creation when gift card products are purchased.
/// </summary>
public interface IGiftCardOrderFulfillmentService
{
    /// <summary>
    /// Processes an order to fulfill any gift card purchases.
    /// Creates gift cards for each gift card item in the order.
    /// </summary>
    /// <param name="orderId">The order ID to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with created gift cards or errors.</returns>
    Task<GiftCardFulfillmentResult> FulfillGiftCardOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an order using the order GUID.
    /// </summary>
    Task<GiftCardFulfillmentResult> FulfillGiftCardOrderAsync(Guid orderGuid, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for gift card fulfillment.
/// </summary>
public class GiftCardFulfillmentOptions
{
    /// <summary>
    /// The content type name that represents purchasable gift cards.
    /// Items with this content type in orders will trigger gift card creation.
    /// </summary>
    public string GiftCardContentTypeName { get; set; } = "Generic.GiftCardProduct";

    /// <summary>
    /// Whether to automatically send email to recipients when gift cards are fulfilled.
    /// </summary>
    public bool SendEmailOnFulfillment { get; set; } = true;

    /// <summary>
    /// Default expiration period for purchased gift cards (in days).
    /// Null means no expiration.
    /// </summary>
    public int? DefaultExpirationDays { get; set; } = 365;

    /// <summary>
    /// Order statuses that trigger gift card fulfillment.
    /// Typically "PaymentReceived" or "Completed".
    /// </summary>
    public string[] TriggerStatuses { get; set; } = ["PaymentReceived", "Completed", "Paid"];
}

/// <summary>
/// Result of gift card order fulfillment.
/// </summary>
public record GiftCardFulfillmentResult
{
    /// <summary>
    /// Whether all gift cards were fulfilled successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Number of gift cards created.
    /// </summary>
    public int GiftCardsCreated { get; init; }

    /// <summary>
    /// The gift card codes that were created.
    /// </summary>
    public IReadOnlyList<string> GiftCardCodes { get; init; } = [];

    /// <summary>
    /// Error messages if any fulfillment failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GiftCardFulfillmentResult Succeeded(IEnumerable<string> codes) => new()
    {
        Success = true,
        GiftCardsCreated = codes.Count(),
        GiftCardCodes = codes.ToList()
    };

    /// <summary>
    /// Creates a partial success result.
    /// </summary>
    public static GiftCardFulfillmentResult Partial(IEnumerable<string> codes, IEnumerable<string> errors) => new()
    {
        Success = false,
        GiftCardsCreated = codes.Count(),
        GiftCardCodes = codes.ToList(),
        Errors = errors.ToList()
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GiftCardFulfillmentResult Failed(string error) => new()
    {
        Success = false,
        GiftCardsCreated = 0,
        Errors = [error]
    };

    /// <summary>
    /// Creates a no-op result when no gift cards need to be created.
    /// </summary>
    public static GiftCardFulfillmentResult NoGiftCards => new()
    {
        Success = true,
        GiftCardsCreated = 0
    };
}

/// <summary>
/// Default implementation of gift card order fulfillment.
/// Monitors orders for gift card product purchases and creates corresponding gift cards.
/// </summary>
public class GiftCardOrderFulfillmentService(
    IGiftCardService giftCardService,
    IGiftCardEmailService giftCardEmailService,
    IInfoProvider<OrderInfo> orderInfoProvider,
    IInfoProvider<OrderItemInfo> orderItemInfoProvider,
    IInfoProvider<OrderAddressInfo> orderAddressInfoProvider,
    ICurrencyService currencyService,
    IOptions<GiftCardFulfillmentOptions> options,
    ILogger<GiftCardOrderFulfillmentService> logger) : IGiftCardOrderFulfillmentService
{
    private readonly GiftCardFulfillmentOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<GiftCardFulfillmentResult> FulfillGiftCardOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await orderInfoProvider.GetAsync(orderId, cancellationToken);
            if (order == null)
            {
                return GiftCardFulfillmentResult.Failed($"Order {orderId} not found.");
            }

            return await ProcessOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fulfill gift cards for order {OrderId}", orderId);
            return GiftCardFulfillmentResult.Failed($"Failed to process order: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<GiftCardFulfillmentResult> FulfillGiftCardOrderAsync(Guid orderGuid, CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderGUID), orderGuid)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var order = orders.FirstOrDefault();
            if (order == null)
            {
                return GiftCardFulfillmentResult.Failed($"Order with GUID {orderGuid} not found.");
            }

            return await ProcessOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fulfill gift cards for order {OrderGuid}", orderGuid);
            return GiftCardFulfillmentResult.Failed($"Failed to process order: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes an order to create gift cards for gift card items.
    /// </summary>
    private async Task<GiftCardFulfillmentResult> ProcessOrderAsync(OrderInfo order, CancellationToken cancellationToken)
    {
        // Get order items
        var orderItems = await orderItemInfoProvider.Get()
            .WhereEquals(nameof(OrderItemInfo.OrderItemOrderID), order.OrderID)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        // Filter to gift card items
        var giftCardItems = orderItems
            .Where(IsGiftCardItem)
            .ToList();

        if (giftCardItems.Count == 0)
        {
            logger.LogDebug("Order {OrderId} has no gift card items to fulfill.", order.OrderID);
            return GiftCardFulfillmentResult.NoGiftCards;
        }

        // Get currency for the order
        var currencyId = await GetOrderCurrencyIdAsync(order, cancellationToken);

        var createdCodes = new List<string>();
        var errors = new List<string>();

        foreach (var item in giftCardItems)
        {
            // Create one gift card per unit ordered
            var quantity = item.OrderItemQuantity;
            var unitPrice = item.OrderItemUnitPrice;

            for (int i = 0; i < quantity; i++)
            {
                var result = await CreateGiftCardFromOrderItemAsync(order, item, unitPrice, currencyId, cancellationToken);

                if (result.Success && result.GiftCard != null)
                {
                    createdCodes.Add(result.GiftCard.GiftCardCode);

                    // Send email notification if enabled
                    if (_options.SendEmailOnFulfillment)
                    {
                        await SendGiftCardEmailAsync(order, item, result.GiftCard, cancellationToken);
                    }
                }
                else
                {
                    errors.Add(result.ErrorMessage ?? "Unknown error creating gift card.");
                }
            }
        }

        if (errors.Count > 0)
        {
            logger.LogWarning(
                "Order {OrderId}: Created {Created} gift cards with {Errors} errors.",
                order.OrderID,
                createdCodes.Count,
                errors.Count);

            return GiftCardFulfillmentResult.Partial(createdCodes, errors);
        }

        logger.LogDebug(
            "Order {OrderId}: Successfully created {Count} gift cards.",
            order.OrderID,
            createdCodes.Count);

        return GiftCardFulfillmentResult.Succeeded(createdCodes);
    }

    /// <summary>
    /// Determines if an order item is a gift card product.
    /// </summary>
    private bool IsGiftCardItem(OrderItemInfo item)
    {
        // Check by SKU pattern - gift card SKUs start with "GIFT-CARD"
        var sku = item.OrderItemSKU ?? string.Empty;
        if (sku.StartsWith("GIFT-CARD", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check by name pattern (fallback)
        var itemName = item.OrderItemName ?? string.Empty;
        if (itemName.Contains("Gift Card", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a gift card from an order item.
    /// </summary>
    private async Task<GiftCardResult> CreateGiftCardFromOrderItemAsync(
        OrderInfo order,
        OrderItemInfo item,
        decimal amount,
        int currencyId,
        CancellationToken cancellationToken)
    {
        // Try to extract recipient info from order item data
        var recipientEmail = ExtractRecipientEmail(item);
        var recipientName = ExtractRecipientName(item);
        var personalMessage = ExtractPersonalMessage(item);

        // Calculate expiration date if configured
        DateTime? expiresAt = _options.DefaultExpirationDays.HasValue
            ? DateTime.UtcNow.AddDays(_options.DefaultExpirationDays.Value)
            : null;

        var request = new CreateGiftCardRequest
        {
            Amount = amount,
            CurrencyId = currencyId,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            PersonalMessage = personalMessage,
            ExpiresAt = expiresAt,
            SourceOrderId = order.OrderID,
            SourceOrderItemId = item.OrderItemID,
            Notes = $"Purchased in order #{order.OrderNumber}"
        };

        return await giftCardService.CreateGiftCardAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets the currency ID for an order.
    /// </summary>
    private async Task<int> GetOrderCurrencyIdAsync(OrderInfo order, CancellationToken cancellationToken)
    {
        // Orders in this system use a default currency - get it from currency service
        // The order doesn't store currency directly, so we use the site's default currency
        var defaultCurrency = await currencyService.GetDefaultCurrencyAsync(cancellationToken);
        return defaultCurrency?.Id ?? 1;
    }

    /// <summary>
    /// Sends the gift card email to the recipient.
    /// </summary>
    private async Task SendGiftCardEmailAsync(
        OrderInfo order,
        OrderItemInfo item,
        Models.GiftCardInfo giftCard,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipientEmail = ExtractRecipientEmail(item);

            // If no recipient email specified, try to use the customer email from billing address
            if (string.IsNullOrEmpty(recipientEmail))
            {
                recipientEmail = await GetOrderCustomerEmailAsync(order.OrderID, cancellationToken);
            }

            if (!string.IsNullOrEmpty(recipientEmail))
            {
                await giftCardEmailService.SendGiftCardEmailAsync(
                    giftCard,
                    recipientEmail,
                    ExtractRecipientName(item),
                    ExtractPersonalMessage(item),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the entire fulfillment if email fails
            logger.LogWarning(ex, "Failed to send gift card email for {Code}", giftCard.GiftCardCode);
        }
    }

    /// <summary>
    /// Gets the customer email from the order's billing address.
    /// </summary>
    private async Task<string?> GetOrderCustomerEmailAsync(int orderId, CancellationToken cancellationToken)
    {
        var addresses = await orderAddressInfoProvider.Get()
            .WhereEquals(nameof(OrderAddressInfo.OrderAddressOrderID), orderId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        // Try billing address first, then shipping
        var billingAddress = addresses.FirstOrDefault(a => a.OrderAddressType == OrderAddressType.Billing);
        if (!string.IsNullOrEmpty(billingAddress?.OrderAddressEmail))
        {
            return billingAddress.OrderAddressEmail;
        }

        var shippingAddress = addresses.FirstOrDefault(a => a.OrderAddressType == OrderAddressType.Shipping);
        return shippingAddress?.OrderAddressEmail;
    }

    /// <summary>
    /// Extracts recipient email from order item.
    /// Gift card recipient info is encoded in SKU field as: GIFT-CARD|emailB64|nameB64|messageB64
    /// </summary>
    private static string? ExtractRecipientEmail(OrderItemInfo item)
    {
        var sku = item.OrderItemSKU ?? string.Empty;
        var parts = sku.Split('|');
        if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
        {
            try
            {
                var email = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                return string.IsNullOrWhiteSpace(email) ? null : email;
            }
            catch { /* Invalid base64, return null */ }
        }
        return null;
    }

    /// <summary>
    /// Extracts recipient name from order item.
    /// </summary>
    private static string? ExtractRecipientName(OrderItemInfo item)
    {
        var sku = item.OrderItemSKU ?? string.Empty;
        var parts = sku.Split('|');
        if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
        {
            try
            {
                var name = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch { /* Invalid base64, return null */ }
        }
        return null;
    }

    /// <summary>
    /// Extracts personal message from order item.
    /// </summary>
    private static string? ExtractPersonalMessage(OrderItemInfo item)
    {
        var sku = item.OrderItemSKU ?? string.Empty;
        var parts = sku.Split('|');
        if (parts.Length >= 4 && !string.IsNullOrEmpty(parts[3]))
        {
            try
            {
                var message = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));
                return string.IsNullOrWhiteSpace(message) ? null : message;
            }
            catch { /* Invalid base64, return null */ }
        }
        return null;
    }
}
