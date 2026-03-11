using Baseline.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.OrderHistory;

#region View Models

/// <summary>
/// View model for order history list.
/// </summary>
public sealed class OrderHistoryViewModel
{
    /// <summary>
    /// Current page number.
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Order history items.
    /// </summary>
    public OrderHistoryResult? OrderHistory { get; set; }

    /// <summary>
    /// Order statistics summary.
    /// </summary>
    public OrderSummaryStats? Stats { get; set; }

    /// <summary>
    /// Error message if loading failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => OrderHistory?.TotalItems > 0
        ? (int)Math.Ceiling((double)OrderHistory.TotalItems / PageSize)
        : 0;
}

/// <summary>
/// View model for single order detail.
/// </summary>
public sealed class OrderDetailViewModel
{
    /// <summary>
    /// The order details.
    /// </summary>
    public OrderDetail? Order { get; set; }

    /// <summary>
    /// Error message if loading failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Order history result with pagination.
/// </summary>
public sealed class OrderHistoryResult
{
    /// <summary>
    /// The orders on this page.
    /// </summary>
    public IReadOnlyList<OrderSummary> Orders { get; set; } = [];

    /// <summary>
    /// Total number of orders.
    /// </summary>
    public int TotalItems { get; set; }
}

/// <summary>
/// Order summary for list display.
/// </summary>
public sealed class OrderSummary
{
    /// <summary>
    /// Order unique identifier.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Order number for display.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order date.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; }

    /// <summary>
    /// Order status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Order total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Number of items in the order.
    /// </summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Detailed order information.
/// </summary>
public sealed class OrderDetail
{
    /// <summary>
    /// Order unique identifier.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Order number for display.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order date.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; }

    /// <summary>
    /// Order status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Subtotal before tax and shipping.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal Tax { get; set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    public decimal Shipping { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Order total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Order items.
    /// </summary>
    public IReadOnlyList<OrderItemDetail> Items { get; set; } = [];

    /// <summary>
    /// Shipping address.
    /// </summary>
    public AddressInfo? ShippingAddress { get; set; }

    /// <summary>
    /// Billing address.
    /// </summary>
    public AddressInfo? BillingAddress { get; set; }

    /// <summary>
    /// Payment method used.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Tracking number if shipped.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gift card amount applied to the order.
    /// </summary>
    public decimal GiftCardAmount { get; set; }

    /// <summary>
    /// Applied promotions/discounts.
    /// </summary>
    public IReadOnlyList<OrderPromotionDetail> Promotions { get; set; } = [];
}

/// <summary>
/// Order promotion/discount details.
/// </summary>
public sealed class OrderPromotionDetail
{
    /// <summary>
    /// Display name of the promotion.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Promotion type (order, item, giftcard).
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Order item details.
/// </summary>
public sealed class OrderItemDetail
{
    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// SKU.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line total.
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Address information.
/// </summary>
public sealed class AddressInfo
{
    public string Name { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Order summary statistics.
/// </summary>
public sealed class OrderSummaryStats
{
    /// <summary>
    /// Total number of orders.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Total amount spent.
    /// </summary>
    public decimal TotalSpent { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Number of pending orders.
    /// </summary>
    public int PendingOrders { get; set; }
}

#endregion

#region Service Interface

/// <summary>
/// Service for customer order operations.
/// </summary>
public interface ICustomerOrderService
{
    /// <summary>
    /// Gets paginated order history for the current user.
    /// </summary>
    Task<OrderHistoryResult> GetOrderHistoryAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order summary statistics for the current user.
    /// </summary>
    Task<OrderSummaryStats> GetOrderSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific order by ID.
    /// </summary>
    Task<OrderDetail?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific order by order number.
    /// </summary>
    Task<OrderDetail?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user can view the specified order.
    /// </summary>
    Task<bool> CanViewOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an order if it's in Created or Pending Payment status.
    /// </summary>
    /// <returns>True if deleted successfully, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> DeleteOrderAsync(string orderNumber, CancellationToken cancellationToken = default);
}

#endregion

/// <summary>
/// Controller for order history pages.
/// </summary>
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class OrderHistoryController(
    ICustomerOrderService orderService,
    ILogger<OrderHistoryController> logger) : Controller
{
    /// <summary>
    /// Route URL for order history.
    /// </summary>
    public const string RouteUrl = "Account/Orders";

    /// <summary>
    /// Displays the order history list.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var model = new OrderHistoryViewModel
        {
            CurrentPage = page,
            PageSize = pageSize,
            IsAuthenticated = true
        };

        try
        {
            model.OrderHistory = await orderService.GetOrderHistoryAsync(page, pageSize);
            model.Stats = await orderService.GetOrderSummaryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading order history");
            model.ErrorMessage = "Unable to load order history. Please try again later.";
        }

        return View("~/Features/OrderHistory/OrderHistoryPage.cshtml", model);
    }

    /// <summary>
    /// Displays a single order detail.
    /// </summary>
    [HttpGet]
    [Route("Account/Orders/{orderId:int}")]
    [Route("{language}/Account/Orders/{orderId:int}")]
    public async Task<IActionResult> Detail(int orderId)
    {
        var model = new OrderDetailViewModel();

        try
        {
            if (!await orderService.CanViewOrderAsync(orderId))
            {
                model.ErrorMessage = "Order not found or you don't have permission to view it.";
                return View("~/Features/OrderHistory/OrderDetail.cshtml", model);
            }

            model.Order = await orderService.GetOrderAsync(orderId);

            if (model.Order is null)
            {
                model.ErrorMessage = "Order not found.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading order {OrderId}", orderId);
            model.ErrorMessage = "Unable to load order details. Please try again later.";
        }

        return View("~/Features/OrderHistory/OrderDetail.cshtml", model);
    }

    /// <summary>
    /// Displays order detail by order number.
    /// </summary>
    [HttpGet]
    [Route("Account/Orders/Number/{orderNumber}")]
    [Route("{language}/Account/Orders/Number/{orderNumber}")]
    public async Task<IActionResult> DetailByNumber(string orderNumber)
    {
        var model = new OrderDetailViewModel();

        try
        {
            model.Order = await orderService.GetOrderByNumberAsync(orderNumber);

            if (model.Order is null)
            {
                model.ErrorMessage = "Order not found or you don't have permission to view it.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading order {OrderNumber}", orderNumber);
            model.ErrorMessage = "Unable to load order details. Please try again later.";
        }

        return View("~/Features/OrderHistory/OrderDetail.cshtml", model);
    }

    /// <summary>
    /// Gets the order history URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
