using System.ComponentModel;
using System.Text;

using Baseline.AI;
using Baseline.Ecommerce.Services;

using CMS.Commerce;
using CMS.DataEngine;
using CMS.DataEngine.Query;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.Ecommerce.Plugins;

/// <summary>
/// AIRA plugin providing e-commerce data access — products, orders, promotions, inventory,
/// and store analytics — directly within the AIRA chat.
/// </summary>
[Description("Provides e-commerce capabilities — product catalog search, order lookup, " +
             "inventory status, promotions, pricing, and store analytics.")]
public sealed class EcommerceAiraPlugin(
    IServiceProvider serviceProvider,
    ILogger<EcommerceAiraPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "Ecommerce";

    // ──────────────────────────────────────────────────────────────
    //  Product Catalog
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("search_products")]
    [Description("Searches the product catalog by keyword, category, price range, or stock status. " +
                 "Returns product name, SKU, price, sale price, stock status, and description.")]
    public async Task<string> SearchProductsAsync(
        [Description("Search keyword (product name, SKU, or description). Leave empty to list all.")] string? query = null,
        [Description("Filter by in-stock products only (true/false)")] bool? inStockOnly = null,
        [Description("Maximum number of results (default: 20)")] int? limit = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetService<IProductService>();

            if (productService is null)
            {
                return "Product service not available. Ensure AddBaselineEcommerce() is registered.";
            }

            var request = new ProductSearchRequest
            {
                Query = query,
                InStockOnly = inStockOnly,
                PageSize = limit ?? 20
            };

            var results = await productService.SearchProductsAsync(request);

            if (results.TotalCount == 0)
            {
                return string.IsNullOrEmpty(query)
                    ? "No products found in the catalog."
                    : $"No products matching '{query}'.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Products ({results.TotalCount} total, showing {results.Items.Count()})");
            sb.AppendLine();

            foreach (var p in results.Items)
            {
                sb.AppendLine($"- **{p.Name}**" + (p.Sku is not null ? $" (SKU: {p.Sku})" : ""));
                sb.AppendLine($"  Price: {p.Price.Amount:C}" +
                    (p.IsOnSale ? $" ~~{p.Price.Amount:C}~~ **{p.SalePrice!.Amount:C}**" : ""));
                sb.AppendLine($"  Stock: {(p.Availability.InStock ? $"In stock ({p.Availability.StockQuantity})" : "Out of stock")}");

                if (!string.IsNullOrEmpty(p.ShortDescription))
                {
                    sb.AppendLine($"  {Truncate(p.ShortDescription, 150)}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to search products");
            return $"Error searching products: {ex.Message}";
        }
    }

    [KernelFunction("get_product_details")]
    [Description("Gets detailed information about a specific product by its SKU code. " +
                 "Returns full product info including price, availability, description, and images.")]
    public async Task<string> GetProductDetailsAsync(
        [Description("The product SKU code")] string sku)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetService<IProductService>();

            if (productService is null)
            {
                return "Product service not available.";
            }

            var product = await productService.GetProductBySkuAsync(sku);

            if (product is null)
            {
                return $"Product with SKU '{sku}' not found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## {product.Name}");
            if (product.Sku is not null) sb.AppendLine($"**SKU**: {product.Sku}");
            sb.AppendLine($"**Price**: {product.Price.Amount:C}");

            if (product.IsOnSale)
            {
                sb.AppendLine($"**Sale Price**: {product.SalePrice!.Amount:C}");
            }

            sb.AppendLine($"**In Stock**: {(product.Availability.InStock ? "Yes" : "No")}");

            if (product.Availability.StockQuantity.HasValue)
            {
                sb.AppendLine($"**Stock Quantity**: {product.Availability.StockQuantity}");
            }

            if (product.Availability.AllowBackorder)
            {
                sb.AppendLine("**Backorders**: Allowed");
            }

            if (!string.IsNullOrEmpty(product.Description))
            {
                sb.AppendLine();
                sb.AppendLine("### Description");
                sb.AppendLine(Truncate(product.Description, 500));
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get product details for {Sku}", sku);
            return $"Error retrieving product: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Orders
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("get_order")]
    [Description("Gets order details by order number. Returns order status, items, totals, " +
                 "shipping/billing addresses, payment method, and dates.")]
    public async Task<string> GetOrderAsync(
        [Description("The order number (e.g., 'ORD-2026-0001')")] string orderNumber)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetService<IOrderService>();

            if (orderService is null)
            {
                return "Order service not available.";
            }

            var order = await orderService.GetOrderByNumberAsync(orderNumber);

            if (order is null)
            {
                return $"Order '{orderNumber}' not found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Order {order.OrderNumber}");
            sb.AppendLine($"- **Status**: {order.Status}");
            sb.AppendLine($"- **Date**: {order.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"- **Subtotal**: {order.Totals.Subtotal.Amount:C}");

            if (order.Totals.Tax.Amount > 0)
            {
                sb.AppendLine($"- **Tax**: {order.Totals.Tax.Amount:C}");
            }

            if (order.Totals.Shipping.Amount > 0)
            {
                sb.AppendLine($"- **Shipping**: {order.Totals.Shipping.Amount:C}");
            }

            if (order.Totals.Discount.Amount > 0)
            {
                sb.AppendLine($"- **Discount**: -{order.Totals.Discount.Amount:C}");
            }

            sb.AppendLine($"- **Grand Total**: {order.Totals.Total.Amount:C}");
            sb.AppendLine();

            if (order.Items.Count > 0)
            {
                sb.AppendLine("### Items");
                foreach (var item in order.Items)
                {
                    sb.AppendLine($"- {item.ProductName} × {item.Quantity} — {item.LineTotal.Amount:C}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("### Shipping Address");
            sb.AppendLine(FormatAddress(order.ShippingAddress));

            if (!string.IsNullOrEmpty(order.TrackingNumber))
            {
                sb.AppendLine($"**Tracking**: {order.TrackingNumber}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get order {OrderNumber}", orderNumber);
            return $"Error retrieving order: {ex.Message}";
        }
    }

    [KernelFunction("get_recent_orders")]
    [Description("Gets the most recent orders with pagination. " +
                 "Returns order number, date, status, items, and total. " +
                 "Useful for getting an overview of recent store activity.")]
    public async Task<string> GetRecentOrdersAsync(
        [Description("Maximum number of orders to return (default: 15)")] int? limit = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var orderProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderInfo>>();
            var statusProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderStatusInfo>>();
            var itemProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderItemInfo>>();

            int pageSize = limit ?? 15;

            // Query orders directly — no customer-scope filter
            var orders = (await orderProvider.Get()
                .OrderByDescending(nameof(OrderInfo.OrderCreatedWhen))
                .TopN(pageSize)
                .GetEnumerableTypedResultAsync())
                .ToList();

            if (orders.Count == 0)
            {
                return "No orders found.";
            }

            // Total count
            int totalCount = await orderProvider.Get()
                .Column(nameof(OrderInfo.OrderID))
                .GetCountAsync();

            // Batch-load statuses
            var statusIds = orders.Select(o => o.OrderOrderStatusID).Distinct().ToList();
            var statuses = (await statusProvider.Get()
                .WhereIn(nameof(OrderStatusInfo.OrderStatusID), statusIds)
                .GetEnumerableTypedResultAsync())
                .ToDictionary(s => s.OrderStatusID, s => s.OrderStatusDisplayName ?? s.OrderStatusName ?? "Unknown");

            // Batch-load item counts
            var orderIds = orders.Select(o => o.OrderID).ToList();
            var items = await itemProvider.Get()
                .WhereIn(nameof(OrderItemInfo.OrderItemOrderID), orderIds)
                .Columns(nameof(OrderItemInfo.OrderItemOrderID))
                .GetEnumerableTypedResultAsync();
            var itemCounts = items.GroupBy(i => i.OrderItemOrderID)
                .ToDictionary(g => g.Key, g => g.Count());

            var sb = new StringBuilder();
            sb.AppendLine($"## Recent Orders ({totalCount} total, showing {orders.Count})");
            sb.AppendLine();
            sb.AppendLine("| Order # | Date | Status | Items | Total |");
            sb.AppendLine("|---------|------|--------|-------|-------|");

            foreach (var o in orders)
            {
                string status = statuses.GetValueOrDefault(o.OrderOrderStatusID, "Unknown");
                int count = itemCounts.GetValueOrDefault(o.OrderID, 0);
                sb.AppendLine($"| {o.OrderNumber} | {o.OrderCreatedWhen:yyyy-MM-dd} | {status} | {count} | {o.OrderGrandTotal:C} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get recent orders");
            return $"Error retrieving orders: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Order Statistics
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("get_order_statistics")]
    [Description("Gets order statistics — total orders, revenue, average order value, " +
                 "orders by status. Queries across all orders in the store.")]
    public async Task<string> GetOrderStatisticsAsync(
        [Description("Number of recent orders to analyze (default: 100)")] int? sampleSize = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var orderProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderInfo>>();
            var statusProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderStatusInfo>>();

            int size = sampleSize ?? 100;

            // Total count across all orders
            int totalCount = await orderProvider.Get()
                .Column(nameof(OrderInfo.OrderID))
                .GetCountAsync();

            // Query recent orders directly — no customer-scope filter
            var orders = (await orderProvider.Get()
                .OrderByDescending(nameof(OrderInfo.OrderCreatedWhen))
                .TopN(size)
                .GetEnumerableTypedResultAsync())
                .ToList();

            if (orders.Count == 0)
            {
                return "No orders found.";
            }

            // Batch-load statuses
            var statusIds = orders.Select(o => o.OrderOrderStatusID).Distinct().ToList();
            var statuses = (await statusProvider.Get()
                .WhereIn(nameof(OrderStatusInfo.OrderStatusID), statusIds)
                .GetEnumerableTypedResultAsync())
                .ToDictionary(s => s.OrderStatusID, s => s.OrderStatusDisplayName ?? s.OrderStatusName ?? "Unknown");

            var totalRevenue = orders.Sum(o => o.OrderGrandTotal);
            var avgOrder = totalRevenue / orders.Count;

            var sb = new StringBuilder();
            sb.AppendLine($"## Order Statistics ({totalCount} total orders, analyzing {orders.Count})");
            sb.AppendLine();
            sb.AppendLine($"- **Total Orders**: {totalCount:N0}");
            sb.AppendLine($"- **Sample Revenue**: {totalRevenue:C}");
            sb.AppendLine($"- **Average Order Value**: {avgOrder:C}");
            sb.AppendLine();

            // By status
            var statusGroups = orders.GroupBy(o => statuses.GetValueOrDefault(o.OrderOrderStatusID, "Unknown"))
                .OrderByDescending(g => g.Count());

            sb.AppendLine("### By Status");

            foreach (var group in statusGroups)
            {
                var groupRevenue = group.Sum(o => o.OrderGrandTotal);
                sb.AppendLine($"- **{group.Key}**: {group.Count()} orders ({groupRevenue:C})");
            }

            // Daily breakdown
            sb.AppendLine();
            sb.AppendLine("### Daily Breakdown (recent)");
            sb.AppendLine("| Date | Orders | Revenue |");
            sb.AppendLine("|------|--------|---------|");

            var dailyGroups = orders
                .GroupBy(o => o.OrderCreatedWhen.Date)
                .OrderByDescending(g => g.Key)
                .Take(14);

            foreach (var day in dailyGroups)
            {
                var dayRevenue = day.Sum(o => o.OrderGrandTotal);
                sb.AppendLine($"| {day.Key:yyyy-MM-dd} | {day.Count()} | {dayRevenue:C} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get order statistics");
            return $"Error retrieving statistics: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Promotions & Coupons
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("get_active_promotions")]
    [Description("Gets currently active promotions (catalog, order, shipping, and buy-x-get-y). " +
                 "Returns promotion name, type, discount amount/percentage, and validity period.")]
    public async Task<string> GetActivePromotionsAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var promotionService = scope.ServiceProvider.GetService<IPromotionService>();

            if (promotionService is null)
            {
                return "Promotion service not available.";
            }

            var catalogPromos = await promotionService.GetActiveCatalogPromotionsAsync();
            var orderPromos = await promotionService.GetActiveOrderPromotionsAsync();
            var shippingPromos = await promotionService.GetActiveShippingPromotionsAsync();
            var bxgyPromos = await promotionService.GetActiveBuyXGetYPromotionsAsync();

            int total = catalogPromos.Count + orderPromos.Count + shippingPromos.Count + bxgyPromos.Count;

            if (total == 0)
            {
                return "No active promotions found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Active Promotions ({total})");
            sb.AppendLine();

            if (catalogPromos.Count > 0)
            {
                sb.AppendLine("### Catalog Promotions");
                foreach (var p in catalogPromos)
                {
                    sb.AppendLine($"- **{p.PromotionDisplayName}**");
                    sb.AppendLine($"  Discount: {FormatDiscount(p.DiscountType, p.DiscountValue)}");
                    sb.AppendLine($"  Valid: {p.ActiveFrom:yyyy-MM-dd} to {p.ActiveTo?.ToString("yyyy-MM-dd") ?? "ongoing"}");
                    if (p.CouponCode is not null) sb.AppendLine($"  Coupon: {p.CouponCode}");
                    sb.AppendLine();
                }
            }

            if (orderPromos.Count > 0)
            {
                sb.AppendLine("### Order Promotions");
                foreach (var p in orderPromos)
                {
                    sb.AppendLine($"- **{p.PromotionDisplayName}**");
                    sb.AppendLine($"  Discount: {FormatDiscount(p.DiscountType, p.DiscountValue)}");

                    if (p.MinimumRequirementType != MinimumRequirementType.None)
                    {
                        sb.AppendLine($"  Minimum: {p.MinimumRequirementType} = {p.MinimumRequirementValue}");
                    }

                    sb.AppendLine($"  Valid: {p.ActiveFrom:yyyy-MM-dd} to {p.ActiveTo?.ToString("yyyy-MM-dd") ?? "ongoing"}");
                    if (p.CouponCode is not null) sb.AppendLine($"  Coupon: {p.CouponCode}");
                    sb.AppendLine();
                }
            }

            if (shippingPromos.Count > 0)
            {
                sb.AppendLine("### Shipping Promotions");
                foreach (var p in shippingPromos)
                {
                    sb.AppendLine($"- **{p.PromotionDisplayName}**");
                    string discount = p.ShippingDiscountType switch
                    {
                        ShippingDiscountType.FreeShipping => "Free Shipping",
                        ShippingDiscountType.ReducedRate => $"{p.DiscountValue}% off shipping",
                        ShippingDiscountType.FlatRate => $"Flat rate ${p.DiscountValue:F2}",
                        _ => $"{p.DiscountValue}"
                    };
                    sb.AppendLine($"  Discount: {discount}");
                    sb.AppendLine($"  Valid: {p.ActiveFrom:yyyy-MM-dd} to {p.ActiveTo?.ToString("yyyy-MM-dd") ?? "ongoing"}");
                    sb.AppendLine();
                }
            }

            if (bxgyPromos.Count > 0)
            {
                sb.AppendLine("### Buy X Get Y Promotions");
                foreach (var p in bxgyPromos)
                {
                    sb.AppendLine($"- **{p.PromotionDisplayName}**");
                    sb.AppendLine($"  Buy {p.BuyQuantity}, Get {p.GetQuantity} at {p.GetDiscountPercentage}% off");
                    sb.AppendLine($"  Valid: {p.ActiveFrom:yyyy-MM-dd} to {p.ActiveTo?.ToString("yyyy-MM-dd") ?? "ongoing"}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get promotions");
            return $"Error retrieving promotions: {ex.Message}";
        }
    }

    [KernelFunction("validate_coupon")]
    [Description("Validates a coupon code and returns its details — discount type, " +
                 "usage limits, expiration, and whether it's currently valid.")]
    public async Task<string> ValidateCouponAsync(
        [Description("The coupon code to validate")] string couponCode)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var couponService = scope.ServiceProvider.GetService<ICouponService>();

            if (couponService is null)
            {
                return "Coupon service not available.";
            }

            var result = await couponService.ValidateAsync(couponCode);

            if (!result.IsValid)
            {
                return $"Coupon '{couponCode}' is not valid: {result.ErrorMessage}";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Coupon: {couponCode}");
            sb.AppendLine("- **Status**: Valid");

            if (result.Coupon is not null)
            {
                sb.AppendLine($"- **Type**: {result.Coupon.CouponType}");
                sb.AppendLine($"- **Uses**: {result.Coupon.UsageCount}" +
                    (result.Coupon.UsageLimit.HasValue ? $"/{result.Coupon.UsageLimit}" : " (unlimited)"));

                if (result.Coupon.ExpirationDate.HasValue)
                {
                    sb.AppendLine($"- **Expires**: {result.Coupon.ExpirationDate:yyyy-MM-dd}");
                }
            }

            if (result.CatalogPromotion is not null)
            {
                sb.AppendLine($"- **Promotion**: {result.CatalogPromotion.PromotionDisplayName}");
                sb.AppendLine($"- **Discount**: {FormatDiscount(result.CatalogPromotion.DiscountType, result.CatalogPromotion.DiscountValue)}");
            }
            else if (result.OrderPromotion is not null)
            {
                sb.AppendLine($"- **Promotion**: {result.OrderPromotion.PromotionDisplayName}");
                sb.AppendLine($"- **Discount**: {FormatDiscount(result.OrderPromotion.DiscountType, result.OrderPromotion.DiscountValue)}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to validate coupon {Code}", couponCode);
            return $"Error validating coupon: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Inventory
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("get_customer_order_history")]
    [Description("Gets order history for a specific customer by email address. " +
                 "Returns their orders with dates, statuses, items, and totals.")]
    public async Task<string> GetCustomerOrderHistoryAsync(
        [Description("Customer email address")] string email,
        [Description("Maximum number of orders to return (default: 10)")] int? limit = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var orderProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderInfo>>();
            var statusProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderStatusInfo>>();
            var itemProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderItemInfo>>();
            var addressProvider = scope.ServiceProvider.GetRequiredService<IInfoProvider<OrderAddressInfo>>();

            int pageSize = limit ?? 10;

            var orders = (await orderProvider.Get()
                .Source(s => s.InnerJoin<OrderAddressInfo>(
                    nameof(OrderInfo.OrderID),
                    nameof(OrderAddressInfo.OrderAddressOrderID)))
                .WhereEquals(nameof(OrderAddressInfo.OrderAddressEmail), email)
                .WhereEquals(nameof(OrderAddressInfo.OrderAddressType), "Billing")
                .OrderByDescending(nameof(OrderInfo.OrderCreatedWhen))
                .TopN(pageSize)
                .GetEnumerableTypedResultAsync())
                .ToList();

            if (orders.Count == 0)
            {
                return $"No orders found for customer '{email}'.";
            }

            // Batch-load statuses
            var statusIds = orders.Select(o => o.OrderOrderStatusID).Distinct().ToList();
            var statuses = (await statusProvider.Get()
                .WhereIn(nameof(OrderStatusInfo.OrderStatusID), statusIds)
                .GetEnumerableTypedResultAsync())
                .ToDictionary(s => s.OrderStatusID, s => s.OrderStatusDisplayName ?? s.OrderStatusName ?? "Unknown");

            // Batch-load item counts
            var orderIds = orders.Select(o => o.OrderID).ToList();
            var items = await itemProvider.Get()
                .WhereIn(nameof(OrderItemInfo.OrderItemOrderID), orderIds)
                .Columns(nameof(OrderItemInfo.OrderItemOrderID))
                .GetEnumerableTypedResultAsync();
            var itemCounts = items.GroupBy(i => i.OrderItemOrderID)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalSpent = orders.Sum(o => o.OrderGrandTotal);

            var sb = new StringBuilder();
            sb.AppendLine($"## Order History for {email}");
            sb.AppendLine($"**Total Orders**: {orders.Count} | **Lifetime Spend**: {totalSpent:C}");
            sb.AppendLine();
            sb.AppendLine("| Order # | Date | Status | Items | Total |");
            sb.AppendLine("|---------|------|--------|-------|-------|");

            foreach (var o in orders)
            {
                string status = statuses.GetValueOrDefault(o.OrderOrderStatusID, "Unknown");
                int count = itemCounts.GetValueOrDefault(o.OrderID, 0);
                sb.AppendLine($"| {o.OrderNumber} | {o.OrderCreatedWhen:yyyy-MM-dd} | {status} | {count} | {o.OrderGrandTotal:C} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get order history for {Email}", email);
            return $"Error retrieving customer order history: {ex.Message}";
        }
    }

    [KernelFunction("check_gift_card_balance")]
    [Description("Checks a gift card's balance and status by redemption code. " +
                 "Returns the remaining balance, initial amount, status, and expiration.")]
    public async Task<string> CheckGiftCardBalanceAsync(
        [Description("The gift card redemption code")] string code)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var giftCardService = scope.ServiceProvider.GetService<IGiftCardService>();

            if (giftCardService is null)
            {
                return "Gift card service not available.";
            }

            var result = await giftCardService.ValidateCodeAsync(code);

            if (result.GiftCard is null)
            {
                return $"Gift card '{code}' not found.";
            }

            var gc = result.GiftCard;
            var sb = new StringBuilder();
            sb.AppendLine($"## Gift Card: {code}");
            sb.AppendLine($"- **Status**: {(result.IsValid ? "Active" : result.ErrorCode?.ToString() ?? "Invalid")}");
            sb.AppendLine($"- **Initial Amount**: {gc.GiftCardInitialAmount:C}");
            sb.AppendLine($"- **Remaining Balance**: {gc.GiftCardRemainingBalance:C}");
            sb.AppendLine($"- **Used**: {gc.GiftCardInitialAmount - gc.GiftCardRemainingBalance:C}");

            if (!result.IsValid && result.ErrorMessage is not null)
            {
                sb.AppendLine($"- **Note**: {result.ErrorMessage}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to check gift card {Code}", code);
            return $"Error checking gift card: {ex.Message}";
        }
    }

    [KernelFunction("get_subscription_status")]
    [Description("Gets subscription status for a customer by customer ID. " +
                 "Returns active subscriptions with plan, status, billing period, and renewal date.")]
    public async Task<string> GetSubscriptionStatusAsync(
        [Description("The customer ID")] int customerId,
        [Description("Include inactive/cancelled subscriptions (default: false)")] bool? includeInactive = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var subscriptionService = scope.ServiceProvider.GetService<IBillingSubscriptionService>();

            if (subscriptionService is null)
            {
                return "Subscription service not available.";
            }

            var subscriptions = await subscriptionService.GetCustomerSubscriptionsAsync(
                customerId, includeInactive ?? false);
            var subList = subscriptions.ToList();

            if (subList.Count == 0)
            {
                return $"No {(includeInactive == true ? "" : "active ")}subscriptions found for customer {customerId}.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Subscriptions for Customer {customerId}");
            sb.AppendLine();

            foreach (var sub in subList)
            {
                sb.AppendLine($"### {sub.Plan?.Name ?? $"Plan #{sub.PlanId}"}");
                sb.AppendLine($"- **Status**: {sub.Status}");
                sb.AppendLine($"- **Started**: {sub.StartDate:yyyy-MM-dd}");
                sb.AppendLine($"- **Current Period Ends**: {sub.CurrentPeriodEnd:yyyy-MM-dd}");

                if (sub.Plan is not null)
                {
                    sb.AppendLine($"- **Price**: {sub.Plan.Price:C}/{sub.Plan.BillingInterval}");
                }

                if (sub.TrialEnd is not null && sub.TrialEnd > DateTimeOffset.UtcNow)
                {
                    sb.AppendLine($"- **Trial Ends**: {sub.TrialEnd:yyyy-MM-dd}");
                }

                if (sub.CancelAtPeriodEnd)
                {
                    sb.AppendLine($"- **Cancels At**: {sub.CancelAt:yyyy-MM-dd}");
                }

                if (!string.IsNullOrEmpty(sub.ExternalSubscriptionId))
                {
                    sb.AppendLine($"- **External ID**: {sub.ExternalSubscriptionId}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get subscriptions for customer {CustomerId}", customerId);
            return $"Error retrieving subscriptions: {ex.Message}";
        }
    }

    [KernelFunction("check_inventory")]
    [Description("Checks inventory/stock levels for a product by SKU. " +
                 "Returns current stock quantity, reservation holds, and availability status.")]
    public async Task<string> CheckInventoryAsync(
        [Description("The product SKU to check")] string sku)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetService<IProductService>();

            if (productService is null)
            {
                return "Product service not available.";
            }

            var product = await productService.GetProductBySkuAsync(sku);

            if (product is null)
            {
                return $"Product with SKU '{sku}' not found.";
            }

            var availability = product.Availability;

            var sb = new StringBuilder();
            sb.AppendLine($"## Inventory: {product.Name} ({sku})");
            sb.AppendLine($"- **In Stock**: {(availability.InStock ? "Yes" : "No")}");

            if (availability.StockQuantity.HasValue)
            {
                sb.AppendLine($"- **Quantity**: {availability.StockQuantity}");
            }

            sb.AppendLine($"- **Backorder**: {(availability.AllowBackorder ? "Allowed" : "Not allowed")}");

            if (!string.IsNullOrEmpty(availability.AvailabilityText))
            {
                sb.AppendLine($"- **Status**: {availability.AvailabilityText}");
            }

            if (availability.ExpectedRestockDate.HasValue)
            {
                sb.AppendLine($"- **Expected Restock**: {availability.ExpectedRestockDate:yyyy-MM-dd}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to check inventory for {Sku}", sku);
            return $"Error checking inventory: {ex.Message}";
        }
    }

    [KernelFunction("get_low_stock_products")]
    [Description("Gets products with stock below the given threshold. " +
                 "Useful for inventory alerts and restock planning.")]
    public async Task<string> GetLowStockProductsAsync(
        [Description("Stock threshold below which products are considered low (default: 10)")] decimal? threshold = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var stockService = scope.ServiceProvider.GetService<IProductStockService>();

            if (stockService is null)
            {
                return "Stock service not available.";
            }

            var lowStock = await stockService.GetLowStockProductsAsync(threshold ?? 10);

            if (lowStock.Count == 0)
            {
                return "No products below the stock threshold.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Low Stock Products ({lowStock.Count})");
            sb.AppendLine();
            sb.AppendLine("| Product GUID | Available | Reserved | Status |");
            sb.AppendLine("|-------------|-----------|----------|--------|");

            foreach (var s in lowStock)
            {
                sb.AppendLine($"| {s.ProductGuid:N} | {s.AvailableQuantity} | {s.ReservedQuantity} | {s.Status} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get low stock products");
            return $"Error retrieving low stock products: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Shipping & Payment
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("get_shipping_methods")]
    [Description("Gets all available shipping methods with their costs and descriptions.")]
    public async Task<string> GetShippingMethodsAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var checkoutService = scope.ServiceProvider.GetService<ICheckoutService>();

            if (checkoutService is null)
            {
                return "Checkout service not available.";
            }

            var methods = await checkoutService.GetShippingMethodsAsync();
            var methodList = methods.ToList();

            if (methodList.Count == 0)
            {
                return "No shipping methods configured.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Shipping Methods ({methodList.Count})");
            sb.AppendLine();

            foreach (var m in methodList)
            {
                sb.AppendLine($"- **{m.Name}** ({m.Code})");
                sb.AppendLine($"  Cost: {m.Cost.Amount:C}");

                if (!string.IsNullOrEmpty(m.EstimatedDelivery))
                {
                    sb.AppendLine($"  Delivery: {m.EstimatedDelivery}");
                }

                if (!string.IsNullOrEmpty(m.Carrier))
                {
                    sb.AppendLine($"  Carrier: {m.Carrier}");
                }

                if (!string.IsNullOrEmpty(m.Description))
                {
                    sb.AppendLine($"  {m.Description}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get shipping methods");
            return $"Error retrieving shipping methods: {ex.Message}";
        }
    }

    [KernelFunction("get_payment_methods")]
    [Description("Gets all available payment methods.")]
    public async Task<string> GetPaymentMethodsAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var checkoutService = scope.ServiceProvider.GetService<ICheckoutService>();

            if (checkoutService is null)
            {
                return "Checkout service not available.";
            }

            var methods = await checkoutService.GetPaymentMethodsAsync();
            var methodList = methods.ToList();

            if (methodList.Count == 0)
            {
                return "No payment methods configured.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Payment Methods ({methodList.Count})");
            sb.AppendLine();

            foreach (var m in methodList)
            {
                sb.AppendLine($"- **{m.Name}** ({m.Type})");

                if (!string.IsNullOrEmpty(m.Description))
                {
                    sb.AppendLine($"  {m.Description}");
                }

                if (m.RequiresRedirect)
                {
                    sb.AppendLine("  (Requires redirect)");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to get payment methods");
            return $"Error retrieving payment methods: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Commerce Contact Sync
    // ──────────────────────────────────────────────────────────────

    [KernelFunction("sync_commerce_contacts")]
    [Description("Syncs commerce metrics (total orders, total spent, last order date, average order value) to contact records for segmentation. Optionally sync a single email or all contacts.")]
    public async Task<string> SyncCommerceContactsAsync(
        [Description("Optional: sync only this email. Leave empty to sync all contacts with orders.")] string? email = null)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetService<ICommerceContactSyncService>();

            if (syncService is null)
            {
                return "Commerce contact sync service is not registered. Ensure AddBaselineEcommerce() is called.";
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                await syncService.SyncContactAsync(email);
                return $"Commerce metrics synced for contact '{email}'.";
            }

            await syncService.SyncAllContactsAsync();
            return "Commerce metrics synced for all contacts with orders.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIRA Ecommerce: Failed to sync commerce contacts");
            return $"Error syncing commerce contacts: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

    private static string FormatDiscount(PromotionDiscountType type, decimal value) =>
        type switch
        {
            PromotionDiscountType.Percentage => $"{value}% off",
            PromotionDiscountType.FixedAmount => $"${value:F2} off",
            _ => $"{value}"
        };

    private static string FormatAddress(Address addr) =>
        $"{addr.FullName}\n{addr.AddressLine1}" +
        (!string.IsNullOrEmpty(addr.AddressLine2) ? $"\n{addr.AddressLine2}" : "") +
        $"\n{addr.City}, {addr.StateProvince} {addr.PostalCode}\n{addr.CountryCode}";

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
