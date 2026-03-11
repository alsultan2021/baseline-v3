using Baseline.Ecommerce.Automation;
using Baseline.Ecommerce.Interfaces;
using CMS.Commerce;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of IOrderService using Kentico Commerce OrderInfo.
/// Provides order retrieval, creation, and management functionality.
/// Automatically persists applied promotions from price calculation results.
/// </summary>
public class OrderService(
    IHttpContextAccessor httpContextAccessor,
    IInfoProvider<OrderInfo> orderInfoProvider,
    IInfoProvider<OrderItemInfo> orderItemInfoProvider,
    IInfoProvider<OrderAddressInfo> orderAddressInfoProvider,
    IInfoProvider<OrderStatusInfo> orderStatusInfoProvider,
    IInfoProvider<OrderPromotionInfo> orderPromotionInfoProvider,
    IInfoProvider<CustomerInfo> customerInfoProvider,
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
    IInfoProvider<PaymentMethodInfo> paymentMethodInfoProvider,
    IOrderNumberGenerator orderNumberGenerator,
    IAutomationEventInterceptor automationEvents,
    IOptions<BaselineEcommerceOptions> ecommerceOptions,
    ILogger<OrderService> logger) : IOrderService
{
    /// <summary>
    /// Default currency code from configuration, used when order doesn't store currency.
    /// </summary>
    private string DefaultCurrency => ecommerceOptions.Value.Pricing.DefaultCurrency;
    /// <inheritdoc/>
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        logger.LogDebug("Getting order: {OrderId}", orderId);

        try
        {
            var orders = await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderGUID), orderId)
                .GetEnumerableTypedResultAsync();

            var orderInfo = orders.FirstOrDefault();
            if (orderInfo == null)
            {
                return null;
            }

            return await MapToOrderAsync(orderInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get order: {OrderId}", orderId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        logger.LogDebug("Getting order by number: {OrderNumber}", orderNumber);

        try
        {
            var orders = await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderNumber), orderNumber)
                .GetEnumerableTypedResultAsync();

            var orderInfo = orders.FirstOrDefault();
            if (orderInfo == null)
            {
                return null;
            }

            return await MapToOrderAsync(orderInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get order by number: {OrderNumber}", orderNumber);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OrderSummary>> GetUserOrdersAsync(int? limit = null)
    {
        logger.LogDebug("Getting user orders, limit: {Limit}", limit);

        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
        {
            return [];
        }

        try
        {
            var query = orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderCustomerID), customerId.Value)
                .OrderByDescending(nameof(OrderInfo.OrderCreatedWhen));

            if (limit.HasValue)
            {
                query = query.TopN(limit.Value);
            }

            var orders = (await query.GetEnumerableTypedResultAsync()).ToList();
            var statusMap = await BatchGetOrderStatusesAsync(orders);
            var itemCountMap = await BatchGetOrderItemCountsAsync(orders);
            return orders.Select(o => MapToOrderSummary(o, statusMap, itemCountMap)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user orders");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<OrderSummary>> GetOrderHistoryAsync(int page = 1, int pageSize = 10)
    {
        logger.LogDebug("Getting order history, page: {Page}, size: {PageSize}", page, pageSize);

        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
        {
            return new PagedResult<OrderSummary>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        try
        {
            // Efficient count query - uses COUNT(*) instead of loading all records
            var totalCount = await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderCustomerID), customerId.Value)
                .Column("OrderID")
                .GetCountAsync();

            var orders = (await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderCustomerID), customerId.Value)
                .OrderByDescending(nameof(OrderInfo.OrderCreatedWhen))
                .Page(page - 1, pageSize)
                .GetEnumerableTypedResultAsync()).ToList();

            var statusMap = await BatchGetOrderStatusesAsync(orders);
            var itemCountMap = await BatchGetOrderItemCountsAsync(orders);

            return new PagedResult<OrderSummary>
            {
                Items = orders.Select(o => MapToOrderSummary(o, statusMap, itemCountMap)).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get order history");
            return new PagedResult<OrderSummary>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    /// <inheritdoc/>
    public async Task<OrderResult> CancelOrderAsync(Guid orderId, string? reason = null)
    {
        logger.LogDebug("Cancelling order: {OrderId}, reason: {Reason}", orderId, reason);

        try
        {
            var orders = await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderGUID), orderId)
                .GetEnumerableTypedResultAsync();

            var orderInfo = orders.FirstOrDefault();
            if (orderInfo == null)
            {
                return OrderResult.Failed("Order not found");
            }

            // Check if order can be cancelled (not already shipped/delivered/cancelled)
            var currentStatus = await GetOrderStatusInfoAsync(orderInfo.OrderOrderStatusID);
            if (currentStatus != null)
            {
                var statusName = currentStatus.OrderStatusName?.ToLowerInvariant() ?? "";
                if (statusName.Contains("shipped") || statusName.Contains("delivered") || statusName.Contains("cancelled"))
                {
                    return OrderResult.Failed($"Order cannot be cancelled - current status: {currentStatus.OrderStatusDisplayName}");
                }
            }

            // Find cancelled status
            var cancelledStatus = await GetCancelledOrderStatusAsync();
            if (cancelledStatus == null)
            {
                return OrderResult.Failed("Cancelled order status not configured");
            }

            orderInfo.OrderOrderStatusID = cancelledStatus.OrderStatusID;
            await orderInfoProvider.SetAsync(orderInfo);

            logger.LogInformation("Order {OrderNumber} cancelled. Reason: {Reason}", orderInfo.OrderNumber, reason);

            var order = await MapToOrderAsync(orderInfo);

            // Fire automation trigger for cancellation (best-effort)
            var memberId = await GetMemberIdForCustomerAsync(orderInfo.OrderCustomerID);
            await automationEvents.OnOrderCancelledAsync(order!, reason, memberId);

            return OrderResult.Succeeded(order!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel order: {OrderId}", orderId);
            return OrderResult.Failed($"Failed to cancel order: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<OrderStatus?> GetOrderStatusAsync(Guid orderId)
    {
        logger.LogDebug("Getting order status: {OrderId}", orderId);

        try
        {
            var orders = await orderInfoProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderGUID), orderId)
                .GetEnumerableTypedResultAsync();

            var orderInfo = orders.FirstOrDefault();
            if (orderInfo == null)
            {
                return null;
            }

            var statusInfo = await GetOrderStatusInfoAsync(orderInfo.OrderOrderStatusID);
            return MapToOrderStatus(statusInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get order status: {OrderId}", orderId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating order with {ItemCount} items", request.Items.Count);

        try
        {
            // Validate request
            var validationErrors = ValidateCreateOrderRequest(request);
            if (validationErrors.Count > 0)
            {
                return OrderResult.Failed(string.Join("; ", validationErrors));
            }

            // Use transaction scope for data integrity - all writes succeed or all fail
            using var scope = new CMSTransactionScope();

            // Get or create customer
            var customerId = await GetOrCreateCustomerAsync(request, cancellationToken);

            // Get default order status
            var defaultStatus = await GetDefaultOrderStatusAsync(cancellationToken);
            if (defaultStatus == null)
            {
                return OrderResult.Failed("Default order status not configured");
            }

            // Get shipping method info
            ShippingMethodInfo? shippingMethod = null;
            if (request.ShippingMethodId.HasValue)
            {
                var shippingMethods = await shippingMethodInfoProvider.Get()
                    .WhereEquals(nameof(ShippingMethodInfo.ShippingMethodGUID), request.ShippingMethodId.Value)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
                shippingMethod = shippingMethods.FirstOrDefault();
            }

            // Get payment method info
            PaymentMethodInfo? paymentMethod = null;
            if (request.PaymentMethodId.HasValue)
            {
                var paymentMethods = await paymentMethodInfoProvider.Get()
                    .WhereEquals(nameof(PaymentMethodInfo.PaymentMethodGUID), request.PaymentMethodId.Value)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
                paymentMethod = paymentMethods.FirstOrDefault();
            }

            // Generate order number
            string orderNumber = await GenerateOrderNumberAsync(cancellationToken);

            // Create OrderInfo
            var orderInfo = new OrderInfo
            {
                OrderGUID = Guid.NewGuid(),
                OrderNumber = orderNumber,
                OrderCustomerID = customerId,
                OrderOrderStatusID = defaultStatus.OrderStatusID,
                OrderTotalPrice = request.PriceCalculation.TotalPrice,
                OrderTotalTax = request.PriceCalculation.TotalTax,
                OrderTotalShipping = request.PriceCalculation.ShippingPrice,
                OrderGrandTotal = request.PriceCalculation.GrandTotal,
                OrderShippingMethodID = shippingMethod?.ShippingMethodID ?? 0,
                OrderShippingMethodDisplayName = shippingMethod?.ShippingMethodDisplayName,
                OrderShippingMethodPrice = request.PriceCalculation.ShippingPrice,
                OrderPaymentMethodID = paymentMethod?.PaymentMethodID ?? 0,
                OrderPaymentMethodDisplayName = paymentMethod?.PaymentMethodDisplayName,
                OrderCreatedWhen = DateTime.UtcNow
            };

            await orderInfoProvider.SetAsync(orderInfo, cancellationToken);
            logger.LogDebug("Created order {OrderNumber} with ID {OrderId}", orderNumber, orderInfo.OrderID);

            // Create order addresses
            await CreateOrderAddressesAsync(orderInfo.OrderID, request, cancellationToken);

            // Create order items and persist catalog promotions
            var orderItems = await CreateOrderItemsAsync(orderInfo.OrderID, request, cancellationToken);

            // Persist order-level promotions
            await PersistOrderPromotionsAsync(orderInfo.OrderID, request.PriceCalculation, cancellationToken);

            // Commit the transaction - all writes were successful
            scope.Commit();

            logger.LogInformation("Order {OrderNumber} created successfully with {ItemCount} items and {PromotionCount} promotions",
                orderNumber,
                orderItems.Count,
                request.PriceCalculation.CatalogPromotions.Count + request.PriceCalculation.OrderPromotions.Count);

            // Map to domain model
            var order = await MapToOrderAsync(orderInfo);

            // Fire automation trigger (best-effort, never fails the order)
            await automationEvents.OnOrderCreatedAsync(order!, request.MemberId);

            return OrderResult.Succeeded(order!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order");
            return OrderResult.Failed($"Failed to create order: {ex.Message}");
        }
    }

    #region Private Helpers

    private async Task<int?> GetCurrentCustomerIdAsync()
    {
        // Get current member ID from claims
        var user = httpContextAccessor.HttpContext?.User;
        var memberIdClaim = user?.FindFirst("MemberId") ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out var memberId))
        {
            return null;
        }

        // Find customer by member ID
        var customers = await customerInfoProvider.Get()
            .WhereEquals(nameof(CustomerInfo.CustomerMemberID), memberId)
            .GetEnumerableTypedResultAsync();

        return customers.FirstOrDefault()?.CustomerID;
    }

    private async Task<Order?> MapToOrderAsync(OrderInfo orderInfo)
    {
        // Get order items
        var items = await orderItemInfoProvider.Get()
            .WhereEquals(nameof(OrderItemInfo.OrderItemOrderID), orderInfo.OrderID)
            .GetEnumerableTypedResultAsync();

        // Get addresses
        var addresses = await orderAddressInfoProvider.Get()
            .WhereEquals(nameof(OrderAddressInfo.OrderAddressOrderID), orderInfo.OrderID)
            .GetEnumerableTypedResultAsync();

        var billingAddress = addresses.FirstOrDefault(a => a.OrderAddressType == OrderAddressType.Billing);
        var shippingAddress = addresses.FirstOrDefault(a => a.OrderAddressType == OrderAddressType.Shipping);

        // Get status
        var statusInfo = await GetOrderStatusInfoAsync(orderInfo.OrderOrderStatusID);

        // Use order's currency or fall back to configured default
        var currency = orderInfo.GetStringValue("OrderCurrencyCode", DefaultCurrency);

        return new Order
        {
            Id = orderInfo.OrderGUID,
            OrderNumber = orderInfo.OrderNumber,
            UserId = orderInfo.OrderCustomerID,
            Status = MapToOrderStatus(statusInfo) ?? OrderStatus.Pending,
            Items = items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = 0, // OrderItemInfo doesn't store ContentItemID
                ProductName = i.OrderItemName ?? "Unknown",
                Sku = i.OrderItemSKU,
                Quantity = (int)i.OrderItemQuantity,
                UnitPrice = new Money { Amount = i.OrderItemUnitPrice, Currency = currency },
                LineTotal = new Money { Amount = i.OrderItemTotalPrice, Currency = currency }
            }).ToList(),
            ShippingAddress = MapToAddress(shippingAddress),
            BillingAddress = MapToAddress(billingAddress),
            ShippingMethod = new ShippingMethod
            {
                Name = orderInfo.OrderShippingMethodDisplayName ?? "Standard",
                Cost = new Money { Amount = orderInfo.OrderShippingMethodPrice, Currency = currency }
            },
            PaymentMethod = orderInfo.OrderPaymentMethodDisplayName ?? "Unknown",
            Totals = new CartTotals
            {
                Subtotal = new Money { Amount = orderInfo.OrderTotalPrice, Currency = currency },
                Tax = new Money { Amount = orderInfo.OrderTotalTax, Currency = currency },
                Shipping = new Money { Amount = orderInfo.OrderTotalShipping, Currency = currency },
                Total = new Money { Amount = orderInfo.OrderGrandTotal, Currency = currency }
            },
            CreatedAt = orderInfo.OrderCreatedWhen
        };
    }

    private OrderSummary MapToOrderSummary(
        OrderInfo orderInfo,
        Dictionary<int, OrderStatus> statusMap,
        Dictionary<int, int> itemCountMap) => new()
        {
            Id = orderInfo.OrderGUID,
            OrderNumber = orderInfo.OrderNumber,
            Status = statusMap.GetValueOrDefault(orderInfo.OrderOrderStatusID, OrderStatus.Pending),
            ItemCount = itemCountMap.GetValueOrDefault(orderInfo.OrderID, 0),
            Total = new Money { Amount = orderInfo.OrderGrandTotal, Currency = orderInfo.GetStringValue("OrderCurrencyCode", DefaultCurrency) },
            CreatedAt = orderInfo.OrderCreatedWhen
        };

    /// <summary>
    /// Batch-loads order statuses for a list of orders (avoids N+1 queries).
    /// </summary>
    private async Task<Dictionary<int, OrderStatus>> BatchGetOrderStatusesAsync(List<OrderInfo> orders)
    {
        var statusIds = orders.Select(o => o.OrderOrderStatusID).Distinct().ToList();
        if (statusIds.Count == 0) return [];

        var statuses = await orderStatusInfoProvider.Get()
            .WhereIn(nameof(OrderStatusInfo.OrderStatusID), statusIds)
            .GetEnumerableTypedResultAsync();

        return statuses.ToDictionary(
            s => s.OrderStatusID,
            s => MapToOrderStatus(s) ?? OrderStatus.Pending);
    }

    /// <summary>
    /// Batch-loads item counts for a list of orders (avoids N+1 queries).
    /// </summary>
    private async Task<Dictionary<int, int>> BatchGetOrderItemCountsAsync(List<OrderInfo> orders)
    {
        var orderIds = orders.Select(o => o.OrderID).ToList();
        if (orderIds.Count == 0) return [];

        var items = await orderItemInfoProvider.Get()
            .WhereIn(nameof(OrderItemInfo.OrderItemOrderID), orderIds)
            .Columns(nameof(OrderItemInfo.OrderItemOrderID))
            .GetEnumerableTypedResultAsync();

        return items.GroupBy(i => i.OrderItemOrderID)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Address MapToAddress(OrderAddressInfo? addressInfo)
    {
        if (addressInfo == null)
        {
            return new Address();
        }

        return new Address
        {
            FirstName = addressInfo.OrderAddressFirstName ?? "",
            LastName = addressInfo.OrderAddressLastName ?? "",
            Company = addressInfo.OrderAddressCompany,
            AddressLine1 = addressInfo.OrderAddressLine1 ?? "",
            AddressLine2 = addressInfo.OrderAddressLine2,
            City = addressInfo.OrderAddressCity ?? "",
            PostalCode = addressInfo.OrderAddressZip ?? "",
            CountryCode = addressInfo.OrderAddressCountryID.ToString(),
            StateProvince = addressInfo.OrderAddressStateID.ToString(),
            Phone = addressInfo.OrderAddressPhone,
            Email = addressInfo.OrderAddressEmail
        };
    }

    private async Task<OrderStatusInfo?> GetOrderStatusInfoAsync(int statusId)
    {
        if (statusId <= 0) return null;

        var statuses = await orderStatusInfoProvider.Get()
            .WhereEquals(nameof(OrderStatusInfo.OrderStatusID), statusId)
            .GetEnumerableTypedResultAsync();

        return statuses.FirstOrDefault();
    }

    private async Task<OrderStatusInfo?> GetCancelledOrderStatusAsync()
    {
        var statuses = await orderStatusInfoProvider.Get()
            .WhereContains(nameof(OrderStatusInfo.OrderStatusName), "cancel")
            .GetEnumerableTypedResultAsync();

        return statuses.FirstOrDefault();
    }

    private async Task<OrderStatusInfo?> GetDefaultOrderStatusAsync(CancellationToken cancellationToken)
    {
        // Get first order status sorted by ID (typically the default)
        var statuses = await orderStatusInfoProvider.Get()
            .OrderBy(nameof(OrderStatusInfo.OrderStatusID))
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return statuses.FirstOrDefault();
    }

    private static List<string> ValidateCreateOrderRequest(CreateOrderRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.BillingAddress.Email))
        {
            errors.Add("Billing address email is required");
        }

        if (string.IsNullOrWhiteSpace(request.BillingAddress.AddressLine1))
        {
            errors.Add("Billing address line 1 is required");
        }

        if (!request.Items.Any())
        {
            errors.Add("At least one item is required");
        }

        return errors;
    }

    private async Task<int> GetOrCreateCustomerAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Try to find existing customer by member ID
        if (request.MemberId.HasValue)
        {
            var existingByMember = await customerInfoProvider.Get()
                .WhereEquals(nameof(CustomerInfo.CustomerMemberID), request.MemberId.Value)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var customer = existingByMember.FirstOrDefault();
            if (customer != null)
            {
                return customer.CustomerID;
            }
        }

        // Try to find by email
        var email = request.BillingAddress.Email ?? "";
        if (!string.IsNullOrEmpty(email))
        {
            var existingByEmail = await customerInfoProvider.Get()
                .WhereEquals(nameof(CustomerInfo.CustomerEmail), email)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var customer = existingByEmail.FirstOrDefault();
            if (customer != null)
            {
                return customer.CustomerID;
            }
        }

        // Create new customer
        var newCustomer = new CustomerInfo
        {
            CustomerGUID = Guid.NewGuid(),
            CustomerFirstName = request.BillingAddress.FirstName,
            CustomerLastName = request.BillingAddress.LastName,
            CustomerEmail = email,
            CustomerPhone = request.BillingAddress.Phone,
            CustomerMemberID = request.MemberId ?? 0,
            CustomerCreatedWhen = DateTime.UtcNow
        };

        await customerInfoProvider.SetAsync(newCustomer, cancellationToken);
        logger.LogDebug("Created new customer {CustomerId} for email {Email}", newCustomer.CustomerID, email);

        return newCustomer.CustomerID;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        // Delegate to the injected IOrderNumberGenerator (SQL sequence-based)
        return await orderNumberGenerator.GenerateOrderNumberAsync(cancellationToken);
    }

    private async Task CreateOrderAddressesAsync(int orderId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Create billing address
        var billingAddress = new OrderAddressInfo
        {
            OrderAddressGUID = Guid.NewGuid(),
            OrderAddressOrderID = orderId,
            OrderAddressType = OrderAddressType.Billing,
            OrderAddressFirstName = request.BillingAddress.FirstName,
            OrderAddressLastName = request.BillingAddress.LastName,
            OrderAddressCompany = request.BillingAddress.Company,
            OrderAddressLine1 = request.BillingAddress.AddressLine1,
            OrderAddressLine2 = request.BillingAddress.AddressLine2,
            OrderAddressCity = request.BillingAddress.City,
            OrderAddressZip = request.BillingAddress.PostalCode,
            OrderAddressPhone = request.BillingAddress.Phone,
            OrderAddressEmail = request.BillingAddress.Email
        };

        await orderAddressInfoProvider.SetAsync(billingAddress, cancellationToken);

        // Create shipping address
        var shippingSource = request.ShippingAddress ?? request.BillingAddress;
        var shippingAddress = new OrderAddressInfo
        {
            OrderAddressGUID = Guid.NewGuid(),
            OrderAddressOrderID = orderId,
            OrderAddressType = OrderAddressType.Shipping,
            OrderAddressFirstName = shippingSource.FirstName,
            OrderAddressLastName = shippingSource.LastName,
            OrderAddressCompany = shippingSource.Company,
            OrderAddressLine1 = shippingSource.AddressLine1,
            OrderAddressLine2 = shippingSource.AddressLine2,
            OrderAddressCity = shippingSource.City,
            OrderAddressZip = shippingSource.PostalCode,
            OrderAddressPhone = shippingSource.Phone,
            OrderAddressEmail = shippingSource.Email
        };

        await orderAddressInfoProvider.SetAsync(shippingAddress, cancellationToken);
    }

    private async Task<List<OrderItemInfo>> CreateOrderItemsAsync(int orderId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItemInfo>();
        var priceItems = request.PriceCalculation.Items.ToDictionary(
            i => i.ProductIdentifier,
            i => i);

        foreach (var cartItem in request.Items)
        {
            var productId = cartItem.ProductId.ToString();
            priceItems.TryGetValue(productId, out var priceItem);

            var orderItem = new OrderItemInfo
            {
                OrderItemGUID = Guid.NewGuid(),
                OrderItemOrderID = orderId,
                OrderItemName = cartItem.ProductName,
                OrderItemSKU = cartItem.Sku,
                OrderItemQuantity = cartItem.Quantity,
                OrderItemUnitPrice = priceItem?.UnitPrice ?? cartItem.UnitPrice.Amount,
                OrderItemTotalPrice = priceItem?.LineTotal ?? cartItem.LineTotal.Amount
            };

            await orderItemInfoProvider.SetAsync(orderItem, cancellationToken);
            orderItems.Add(orderItem);

            // Persist catalog promotion for this item if any
            if (priceItem?.CatalogPromotion != null)
            {
                await PersistCatalogPromotionAsync(orderId, orderItem.OrderItemID, priceItem.CatalogPromotion, priceItem.Discount, cancellationToken);
            }
        }

        return orderItems;
    }

    private async Task PersistCatalogPromotionAsync(
        int orderId,
        int orderItemId,
        AppliedPromotion promotion,
        decimal discountAmount,
        CancellationToken cancellationToken)
    {
        // Try to parse promotion ID as int (for Kentico promotions) or use 0
        int.TryParse(promotion.PromotionId, out var promotionId);

        var orderPromotion = new OrderPromotionInfo
        {
            OrderPromotionOrderID = orderId,
            OrderPromotionOrderItemID = orderItemId,
            OrderPromotionPromotionID = promotionId,
            OrderPromotionPromotionDisplayName = promotion.Name,
            OrderPromotionDiscountAmount = discountAmount,
            OrderPromotionPromotionType = PromotionType.Catalog
        };

        await orderPromotionInfoProvider.SetAsync(orderPromotion, cancellationToken);
        logger.LogDebug("Persisted catalog promotion {PromotionName} for order item {OrderItemId}",
            promotion.Name, orderItemId);
    }

    private async Task PersistOrderPromotionsAsync(
        int orderId,
        PriceCalculationResult priceCalculation,
        CancellationToken cancellationToken)
    {
        foreach (var promotion in priceCalculation.OrderPromotions)
        {
            int.TryParse(promotion.PromotionId, out var promotionId);

            var orderPromotion = new OrderPromotionInfo
            {
                OrderPromotionOrderID = orderId,
                OrderPromotionOrderItemID = 0, // Order-level promotion, not item-specific
                OrderPromotionPromotionID = promotionId,
                OrderPromotionPromotionDisplayName = promotion.Name,
                OrderPromotionDiscountAmount = promotion.DiscountAmount,
                OrderPromotionPromotionType = PromotionType.Order
            };

            await orderPromotionInfoProvider.SetAsync(orderPromotion, cancellationToken);
            logger.LogDebug("Persisted order promotion {PromotionName} with discount {Discount}",
                promotion.Name, promotion.DiscountAmount);
        }
    }

    private static OrderStatus? MapToOrderStatus(OrderStatusInfo? statusInfo)
    {
        if (statusInfo == null) return null;

        var name = statusInfo.OrderStatusName?.ToLowerInvariant() ?? "";

        return name switch
        {
            _ when name.Contains("pending") => OrderStatus.Pending,
            _ when name.Contains("paid") || name.Contains("payment") => OrderStatus.PaymentReceived,
            _ when name.Contains("processing") => OrderStatus.Processing,
            _ when name.Contains("shipped") => OrderStatus.Shipped,
            _ when name.Contains("delivered") => OrderStatus.Delivered,
            _ when name.Contains("cancel") => OrderStatus.Cancelled,
            _ when name.Contains("refund") => OrderStatus.Refunded,
            _ => OrderStatus.Pending
        };
    }

    private async Task<int?> GetMemberIdForCustomerAsync(int customerId)
    {
        try
        {
            var customers = await customerInfoProvider.Get()
                .WhereEquals(nameof(CustomerInfo.CustomerID), customerId)
                .GetEnumerableTypedResultAsync();

            var customer = customers.FirstOrDefault();
            return customer?.CustomerMemberID is > 0 ? customer.CustomerMemberID : null;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
