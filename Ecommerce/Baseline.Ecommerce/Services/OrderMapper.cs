using CMS.Commerce;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ICacheDependencyBuilderFactory = CMS.Helpers.ICacheDependencyBuilderFactory;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of IOrderMapper using Kentico Commerce data.
/// Provides order mapping and transformation functionality for various use cases
/// including external system integrations, order exports, and checkout workflows.
/// </summary>
public class OrderMapper(
    IInfoProvider<OrderInfo> orderInfoProvider,
    IInfoProvider<OrderItemInfo> orderItemInfoProvider,
    IInfoProvider<OrderAddressInfo> orderAddressInfoProvider,
    IInfoProvider<CustomerInfo> customerInfoProvider,
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
    IInfoProvider<PaymentMethodInfo> paymentMethodInfoProvider,
    IWebsiteChannelContext websiteChannelContext,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
    IOptions<BaselineEcommerceOptions> options,
    ILogger<OrderMapper> logger) : IOrderMapper
{
    private readonly string _defaultCurrency = options.Value.Pricing.DefaultCurrency;
    private const int CacheMinutes = 5;

    /// <inheritdoc/>
    public Task<CreateOrderRequest> MapToCreateRequestAsync(
        CheckoutSession session,
        PriceCalculationResult priceResult,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Mapping checkout session {SessionId} to create order request", session.Id);

        var request = new CreateOrderRequest
        {
            BillingAddress = session.BillingAddress ?? session.ShippingAddress
                ?? throw new InvalidOperationException("Billing address is required"),
            ShippingAddress = session.UseSameAddressForBilling
                ? session.BillingAddress
                : session.ShippingAddress,
            Items = [], // Items will be loaded from cart separately
            ShippingMethodId = session.ShippingMethodId,
            PaymentMethodId = session.PaymentMethodId,
            PriceCalculation = priceResult
        };

        return Task.FromResult(request);
    }

    /// <inheritdoc/>
    public async Task<OrderMappingResult> MapToExternalAsync(
        Order order,
        OrderMappingContext? context = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Mapping order {OrderNumber} to external format for {TargetSystem}",
            order.OrderNumber, context?.TargetSystem ?? "default");

        try
        {
            var currency = order.Totals.Total.Currency ?? _defaultCurrency;

            var data = new Dictionary<string, object>
            {
                // Order header
                ["order_id"] = order.Id.ToString(),
                ["order_number"] = order.OrderNumber,
                ["status"] = order.Status.ToString(),
                ["created_at"] = order.CreatedAt.ToString("O"),
                ["currency"] = currency,

                // Totals
                ["subtotal"] = order.Totals.Subtotal.Amount,
                ["discount"] = order.Totals.Discount.Amount,
                ["tax"] = order.Totals.Tax.Amount,
                ["shipping"] = order.Totals.Shipping.Amount,
                ["total"] = order.Totals.Total.Amount,

                // Shipping address
                ["shipping_address"] = MapAddress(order.ShippingAddress),

                // Billing address
                ["billing_address"] = MapAddress(order.BillingAddress),

                // Shipping method
                ["shipping_method"] = new Dictionary<string, object>
                {
                    ["id"] = order.ShippingMethod.Id.ToString(),
                    ["name"] = order.ShippingMethod.Name,
                    ["code"] = order.ShippingMethod.Code
                },

                // Payment method
                ["payment_method"] = order.PaymentMethod
            };

            // Include item details if requested
            if (context?.IncludeItemDetails ?? true)
            {
                var items = await MapOrderItemsAsync(order, cancellationToken);
                data["items"] = items.Select(item => new Dictionary<string, object>
                {
                    ["id"] = item.Id.ToString(),
                    ["sku"] = item.Sku,
                    ["name"] = item.Name,
                    ["quantity"] = item.Quantity,
                    ["unit_price"] = item.UnitPrice.Amount,
                    ["line_total"] = item.LineTotal.Amount,
                    ["tax"] = item.Tax.Amount,
                    ["tax_rate"] = item.TaxRate,
                    ["discount"] = item.Discount.Amount,
                    ["weight"] = item.Weight ?? 0,
                    ["attributes"] = item.Attributes
                }).ToList();
            }

            // Include customer data if requested
            if ((context?.IncludeCustomerData ?? true) && order.UserId is { } userId)
            {
                var customer = await GetCustomerAsync(userId, cancellationToken);
                if (customer != null)
                {
                    data["customer"] = new Dictionary<string, object>
                    {
                        ["id"] = customer.CustomerID,
                        ["email"] = customer.CustomerEmail ?? "",
                        ["first_name"] = customer.CustomerFirstName ?? "",
                        ["last_name"] = customer.CustomerLastName ?? ""
                    };
                }
            }

            // Apply custom field mappings if provided
            if (context?.FieldMappings?.Count > 0)
            {
                ApplyFieldMappings(data, context.FieldMappings);
            }

            // Add target system metadata if provided
            if (!string.IsNullOrEmpty(context?.TargetSystem))
            {
                data["_target_system"] = context.TargetSystem;
                data["_format_version"] = context.FormatVersion ?? "1.0";
                data["_mapped_at"] = DateTimeOffset.UtcNow.ToString("O");
            }

            logger.LogInformation("Successfully mapped order {OrderNumber} to external format", order.OrderNumber);
            return OrderMappingResult.Successful(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to map order {OrderNumber} to external format", order.OrderNumber);
            return OrderMappingResult.Failed($"Failed to map order: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<OrderMappingResult<Order>> MapFromExternalAsync(
        IDictionary<string, object> externalData,
        OrderMappingContext? context = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Mapping external data to internal order format");

        var warnings = new List<string>();

        try
        {
            // Extract order ID or number
            var orderNumber = GetDictValue(externalData, "order_number") ?? "";
            var orderIdStr = GetDictValue(externalData, "order_id");

            if (string.IsNullOrEmpty(orderNumber) && string.IsNullOrEmpty(orderIdStr))
            {
                return OrderMappingResult<Order>.Failed("Order number or ID is required");
            }

            Guid.TryParse(orderIdStr, out var orderId);

            // Try to find existing order
            OrderInfo? existingOrder = null;
            if (orderId != Guid.Empty)
            {
                var orders = await orderInfoProvider.Get()
                    .WhereEquals(nameof(OrderInfo.OrderGUID), orderId)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
                existingOrder = orders.FirstOrDefault();
            }
            else if (!string.IsNullOrEmpty(orderNumber))
            {
                var orders = await orderInfoProvider.Get()
                    .WhereEquals(nameof(OrderInfo.OrderNumber), orderNumber)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
                existingOrder = orders.FirstOrDefault();
            }

            if (existingOrder != null)
            {
                // Map existing order from database
                var order = await MapOrderInfoToOrderAsync(existingOrder, cancellationToken);
                return OrderMappingResult<Order>.Successful(order, warnings);
            }

            // Create new order from external data
            var currency = GetDictValue(externalData, "currency") ?? _defaultCurrency;

            var newOrder = new Order
            {
                Id = orderId != Guid.Empty ? orderId : Guid.NewGuid(),
                OrderNumber = orderNumber,
                Status = ParseOrderStatus(GetDictValue(externalData, "status")),
                CreatedAt = ParseDateTime(externalData.TryGetValue("created_at", out var createdVal) ? createdVal : null),
                ShippingAddress = MapFromExternalAddress(
                    externalData.TryGetValue("shipping_address", out var shipAddr) && shipAddr is IDictionary<string, object> shipDict
                        ? shipDict
                        : new Dictionary<string, object>()),
                BillingAddress = MapFromExternalAddress(
                    externalData.TryGetValue("billing_address", out var billAddr) && billAddr is IDictionary<string, object> billDict
                        ? billDict
                        : new Dictionary<string, object>()),
                PaymentMethod = GetDictValue(externalData, "payment_method") ?? "",
                Notes = GetDictValue(externalData, "notes"),
                Totals = new CartTotals
                {
                    Subtotal = new Money { Amount = ParseDecimal(externalData, "subtotal"), Currency = currency },
                    Discount = new Money { Amount = ParseDecimal(externalData, "discount"), Currency = currency },
                    Tax = new Money { Amount = ParseDecimal(externalData, "tax"), Currency = currency },
                    Shipping = new Money { Amount = ParseDecimal(externalData, "shipping"), Currency = currency },
                    Total = new Money { Amount = ParseDecimal(externalData, "total"), Currency = currency }
                }
            };

            // Parse items if present
            if (externalData.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> items)
            {
                foreach (var itemObj in items)
                {
                    if (itemObj is IDictionary<string, object> itemData)
                    {
                        var orderItem = new OrderItem
                        {
                            Id = Guid.TryParse(GetDictValue(itemData, "id"), out var itemId) ? itemId : Guid.NewGuid(),
                            ProductName = GetDictValue(itemData, "name") ?? "",
                            Sku = GetDictValue(itemData, "sku"),
                            Quantity = (int)ParseDecimal(itemData, "quantity"),
                            UnitPrice = new Money { Amount = ParseDecimal(itemData, "unit_price"), Currency = currency },
                            LineTotal = new Money { Amount = ParseDecimal(itemData, "line_total"), Currency = currency }
                        };
                        newOrder.Items.Add(orderItem);
                    }
                }
            }

            // Parse shipping method if present
            if (externalData.TryGetValue("shipping_method", out var shipMethodObj) && shipMethodObj is IDictionary<string, object> shippingData)
            {
                newOrder.ShippingMethod = new ShippingMethod
                {
                    Id = Guid.TryParse(GetDictValue(shippingData, "id"), out var shipId) ? shipId : Guid.Empty,
                    Name = GetDictValue(shippingData, "name") ?? "",
                    Code = GetDictValue(shippingData, "code") ?? ""
                };
            }

            logger.LogInformation("Successfully mapped external data to order {OrderNumber}", newOrder.OrderNumber);
            return OrderMappingResult<Order>.Successful(newOrder, warnings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to map external data to order");
            return OrderMappingResult<Order>.Failed($"Failed to map external data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<OrderSummary> MapToSummaryAsync(Order order, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Mapping order {OrderNumber} to summary", order.OrderNumber);

        return await Task.FromResult(new OrderSummary
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            ItemCount = order.Items.Sum(i => i.Quantity),
            Total = order.Totals.Total,
            CreatedAt = order.CreatedAt
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OrderItemDetail>> MapOrderItemsAsync(Order order, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Mapping {ItemCount} items for order {OrderNumber}", order.Items.Count, order.OrderNumber);

        var currency = order.Totals.Total.Currency ?? _defaultCurrency;

        // Get order items from database for enrichment
        var orderInfo = await GetOrderByGuidAsync(order.Id, cancellationToken);
        if (orderInfo == null)
        {
            // Map from in-memory order items
            return order.Items.Select(item => new OrderItemDetail
            {
                Id = item.Id,
                Sku = item.Sku ?? "",
                Name = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountedUnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal,
                Tax = new Money { Amount = 0, Currency = currency },
                TaxRate = 0
            }).ToList();
        }

        // Get order items from database
        var orderItems = await GetOrderItemsAsync(orderInfo.OrderID, cancellationToken);

        return orderItems.Select(itemInfo => new OrderItemDetail
        {
            Id = itemInfo.OrderItemGUID,
            Sku = itemInfo.OrderItemSKU ?? "",
            Name = itemInfo.OrderItemName ?? "",
            Description = "",
            ImageUrl = null, // Would require product data retrieval
            ProductUrl = null,
            Quantity = (int)itemInfo.OrderItemQuantity,
            UnitPrice = new Money { Amount = itemInfo.OrderItemUnitPrice, Currency = currency },
            DiscountedUnitPrice = new Money { Amount = itemInfo.OrderItemUnitPrice, Currency = currency },
            LineTotal = new Money { Amount = itemInfo.OrderItemTotalPrice, Currency = currency },
            Tax = new Money { Amount = itemInfo.OrderItemTotalTax, Currency = currency },
            TaxRate = itemInfo.OrderItemTaxRate,
            Discount = Money.Zero(currency)
        }).ToList();
    }

    /// <inheritdoc/>
    public IDictionary<string, object> MapAddress(Address address)
    {
        return new Dictionary<string, object>
        {
            ["first_name"] = address.FirstName,
            ["last_name"] = address.LastName,
            ["company"] = address.Company ?? "",
            ["address_line_1"] = address.AddressLine1,
            ["address_line_2"] = address.AddressLine2 ?? "",
            ["city"] = address.City,
            ["state_province"] = address.StateProvince ?? "",
            ["postal_code"] = address.PostalCode,
            ["country_code"] = address.CountryCode,
            ["phone"] = address.Phone ?? "",
            ["email"] = address.Email ?? ""
        };
    }

    /// <inheritdoc/>
    public Address MapFromExternalAddress(IDictionary<string, object> externalAddress)
    {
        return new Address
        {
            FirstName = GetDictValue(externalAddress, "first_name") ?? "",
            LastName = GetDictValue(externalAddress, "last_name") ?? "",
            Company = GetDictValue(externalAddress, "company"),
            AddressLine1 = GetDictValue(externalAddress, "address_line_1") ?? "",
            AddressLine2 = GetDictValue(externalAddress, "address_line_2"),
            City = GetDictValue(externalAddress, "city") ?? "",
            StateProvince = GetDictValue(externalAddress, "state_province"),
            PostalCode = GetDictValue(externalAddress, "postal_code") ?? "",
            CountryCode = GetDictValue(externalAddress, "country_code") ?? "",
            Phone = GetDictValue(externalAddress, "phone"),
            Email = GetDictValue(externalAddress, "email")
        };
    }

    #region Private Helpers

    private static string? GetDictValue(IDictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static decimal ParseDecimal(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return 0;
        if (value is decimal d) return d;
        if (value is double dbl) return (decimal)dbl;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is string str && decimal.TryParse(str, out var parsed)) return parsed;
        return 0;
    }

    private async Task<OrderInfo?> GetOrderByGuidAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var orders = await orderInfoProvider.Get()
            .WhereEquals(nameof(OrderInfo.OrderGUID), orderId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
        return orders.FirstOrDefault();
    }

    private async Task<IEnumerable<OrderItemInfo>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetOrderItemsInternalAsync(orderId, cancellationToken);
        }

        var cacheSettings = new CacheSettings(CacheMinutes, "OrderMapper", "OrderItems", orderId);
        return await cache.LoadAsync(async cs =>
        {
            var result = await GetOrderItemsInternalAsync(orderId, cancellationToken);
            cs.CacheDependency = cacheDependencyBuilderFactory.Create()
                .ForInfoObjects<OrderItemInfo>().All()
                .Builder().Build();
            return result.ToList();
        }, cacheSettings);
    }

    private async Task<IEnumerable<OrderItemInfo>> GetOrderItemsInternalAsync(int orderId, CancellationToken cancellationToken)
    {
        return await orderItemInfoProvider.Get()
            .WhereEquals(nameof(OrderItemInfo.OrderItemOrderID), orderId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }

    private async Task<CustomerInfo?> GetCustomerAsync(int customerId, CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetCustomerInternalAsync(customerId, cancellationToken);
        }

        var cacheSettings = new CacheSettings(CacheMinutes, "OrderMapper", "Customer", customerId);
        return await cache.LoadAsync(async cs =>
        {
            var result = await GetCustomerInternalAsync(customerId, cancellationToken);
            if (result != null)
            {
                cs.CacheDependency = cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<CustomerInfo>().All()
                    .Builder().Build();
            }
            return result;
        }, cacheSettings);
    }

    private async Task<CustomerInfo?> GetCustomerInternalAsync(int customerId, CancellationToken cancellationToken)
    {
        var customers = await customerInfoProvider.Get()
            .WhereEquals(nameof(CustomerInfo.CustomerID), customerId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
        return customers.FirstOrDefault();
    }

    private async Task<Order> MapOrderInfoToOrderAsync(OrderInfo orderInfo, CancellationToken cancellationToken)
    {
        // Get order items
        var orderItems = await GetOrderItemsAsync(orderInfo.OrderID, cancellationToken);

        // Get addresses
        var addresses = await orderAddressInfoProvider.Get()
            .WhereEquals(nameof(OrderAddressInfo.OrderAddressOrderID), orderInfo.OrderID)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var shippingAddress = addresses.FirstOrDefault(a => a.OrderAddressType == OrderAddressType.Shipping);
        var billingAddress = addresses.FirstOrDefault(a => a.OrderAddressType == OrderAddressType.Billing);

        // Get shipping method
        ShippingMethodInfo? shippingMethod = null;
        if (orderInfo.OrderShippingMethodID > 0)
        {
            var methods = await shippingMethodInfoProvider.Get()
                .WhereEquals(nameof(ShippingMethodInfo.ShippingMethodID), orderInfo.OrderShippingMethodID)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
            shippingMethod = methods.FirstOrDefault();
        }

        // Get payment method
        PaymentMethodInfo? paymentMethod = null;
        if (orderInfo.OrderPaymentMethodID > 0)
        {
            var methods = await paymentMethodInfoProvider.Get()
                .WhereEquals(nameof(PaymentMethodInfo.PaymentMethodID), orderInfo.OrderPaymentMethodID)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
            paymentMethod = methods.FirstOrDefault();
        }

        var currency = _defaultCurrency;

        return new Order
        {
            Id = orderInfo.OrderGUID,
            OrderNumber = orderInfo.OrderNumber ?? "",
            UserId = orderInfo.OrderCustomerID,
            Status = MapOrderStatus(orderInfo.OrderOrderStatusID),
            CreatedAt = orderInfo.OrderCreatedWhen,
            ShippedAt = null, // Would need status tracking
            DeliveredAt = null,
            TrackingNumber = null,
            Notes = null,
            ShippingAddress = shippingAddress != null ? MapOrderAddressToAddress(shippingAddress) : new Address(),
            BillingAddress = billingAddress != null ? MapOrderAddressToAddress(billingAddress) : new Address(),
            ShippingMethod = new ShippingMethod
            {
                Id = shippingMethod?.ShippingMethodGUID ?? Guid.Empty,
                Name = orderInfo.OrderShippingMethodDisplayName ?? shippingMethod?.ShippingMethodDisplayName ?? "",
                Code = shippingMethod?.ShippingMethodName ?? "",
                Cost = new Money { Amount = orderInfo.OrderShippingMethodPrice, Currency = currency }
            },
            PaymentMethod = orderInfo.OrderPaymentMethodDisplayName ?? paymentMethod?.PaymentMethodDisplayName ?? "",
            Items = orderItems.Select(item => new OrderItem
            {
                Id = item.OrderItemGUID,
                ProductId = 0, // OrderItemInfo doesn't have ProductContentItemID
                ProductName = item.OrderItemName ?? "",
                Sku = item.OrderItemSKU,
                Quantity = (int)item.OrderItemQuantity,
                UnitPrice = new Money { Amount = item.OrderItemUnitPrice, Currency = currency },
                LineTotal = new Money { Amount = item.OrderItemTotalPrice, Currency = currency }
            }).ToList(),
            Totals = new CartTotals
            {
                Subtotal = new Money { Amount = orderInfo.OrderTotalPrice - orderInfo.OrderTotalTax, Currency = currency },
                Tax = new Money { Amount = orderInfo.OrderTotalTax, Currency = currency },
                Shipping = new Money { Amount = orderInfo.OrderTotalShipping, Currency = currency },
                Total = new Money { Amount = orderInfo.OrderGrandTotal, Currency = currency }
            }
        };
    }

    private static Address MapOrderAddressToAddress(OrderAddressInfo addressInfo)
    {
        return new Address
        {
            FirstName = addressInfo.OrderAddressFirstName ?? "",
            LastName = addressInfo.OrderAddressLastName ?? "",
            Company = addressInfo.OrderAddressCompany,
            AddressLine1 = addressInfo.OrderAddressLine1 ?? "",
            AddressLine2 = addressInfo.OrderAddressLine2,
            City = addressInfo.OrderAddressCity ?? "",
            StateProvince = null, // OrderAddressInfo uses different property
            PostalCode = addressInfo.OrderAddressZip ?? "",
            CountryCode = "", // OrderAddressInfo doesn't have CountryCode directly
            Phone = addressInfo.OrderAddressPhone,
            Email = addressInfo.OrderAddressEmail
        };
    }

    private static OrderStatus MapOrderStatus(int statusId)
    {
        // Map based on common status IDs - sites should override for custom statuses
        return statusId switch
        {
            1 => OrderStatus.Pending,
            2 => OrderStatus.PaymentReceived,
            3 => OrderStatus.Processing,
            4 => OrderStatus.Shipped,
            5 => OrderStatus.Delivered,
            6 => OrderStatus.Cancelled,
            7 => OrderStatus.Refunded,
            _ => OrderStatus.Pending
        };
    }

    private static OrderStatus ParseOrderStatus(string? status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return OrderStatus.Pending;
        }

        return Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var result)
            ? result
            : OrderStatus.Pending;
    }

    private static DateTimeOffset ParseDateTime(object? value)
    {
        if (value is DateTimeOffset dto) return dto;
        if (value is DateTime dt) return new DateTimeOffset(dt);
        if (value is string str && DateTimeOffset.TryParse(str, out var parsed)) return parsed;
        return DateTimeOffset.UtcNow;
    }

    private static void ApplyFieldMappings(IDictionary<string, object> data, IDictionary<string, string> mappings)
    {
        foreach (var mapping in mappings)
        {
            if (data.TryGetValue(mapping.Key, out var value))
            {
                data[mapping.Value] = value;
                data.Remove(mapping.Key);
            }
        }
    }

    #endregion
}
