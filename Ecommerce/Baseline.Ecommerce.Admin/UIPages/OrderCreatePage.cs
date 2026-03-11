using System.Text.Json;
using CMS.Commerce;
using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.Services;
using Baseline.Ecommerce.Admin.ViewModels;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

// Register the Create Order page under the existing Kentico Orders application (hidden from nav)
[assembly: UIPage(
    parentType: typeof(OrdersApplication),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.OrderCreatePage),
    name: "Create Order",
    templateName: TemplateNames.EDIT,
    order: 200)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for manually creating orders.
/// Useful for phone orders, in-person sales, or administrative order creation.
/// </summary>
[UINavigation(false)]
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class OrderCreatePage : ModelEditPage<OrderCreateViewModel>
{
    private readonly IInfoProvider<OrderInfo> _orderProvider;
    private readonly IInfoProvider<OrderItemInfo> _orderItemProvider;
    private readonly IInfoProvider<OrderAddressInfo> _orderAddressProvider;
    private readonly IInfoProvider<CustomerInfo> _customerProvider;
    private readonly IInfoProvider<ShippingMethodInfo> _shippingMethodProvider;
    private readonly IInfoProvider<PaymentMethodInfo> _paymentMethodProvider;
    private readonly IInfoProvider<OrderStatusInfo> _orderStatusProvider;
    private OrderCreateViewModel? _model = null;

    public OrderCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<OrderInfo> orderProvider,
        IInfoProvider<OrderItemInfo> orderItemProvider,
        IInfoProvider<OrderAddressInfo> orderAddressProvider,
        IInfoProvider<CustomerInfo> customerProvider,
        IInfoProvider<ShippingMethodInfo> shippingMethodProvider,
        IInfoProvider<PaymentMethodInfo> paymentMethodProvider,
        IInfoProvider<OrderStatusInfo> orderStatusProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _orderProvider = orderProvider;
        _orderItemProvider = orderItemProvider;
        _orderAddressProvider = orderAddressProvider;
        _customerProvider = customerProvider;
        _shippingMethodProvider = shippingMethodProvider;
        _paymentMethodProvider = paymentMethodProvider;
        _orderStatusProvider = orderStatusProvider;
    }

    protected override OrderCreateViewModel Model => _model ??= new OrderCreateViewModel();

    public override Task ConfigurePage()
    {
        PageConfiguration.SubmitConfiguration.Label = "Create Order";
        PageConfiguration.Headline = "Create Manual Order";
        return base.ConfigurePage();
    }

    protected override async Task<ICommandResponse> ProcessFormData(OrderCreateViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            // Parse order items from JSON
            List<OrderItemInput> orderItems;
            try
            {
                orderItems = JsonSerializer.Deserialize<List<OrderItemInput>>(model.OrderItemsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            catch (JsonException ex)
            {
                return GetErrorResponse($"Invalid order items JSON format: {ex.Message}");
            }

            if (orderItems.Count == 0)
            {
                return GetErrorResponse("At least one order item is required.");
            }

            // Validate payment method
            if (!int.TryParse(model.PaymentMethodID, out var paymentMethodId) || paymentMethodId <= 0)
            {
                return GetErrorResponse("Please select a valid payment method.");
            }

            // Validate order status
            if (!int.TryParse(model.OrderStatusID, out var orderStatusId) || orderStatusId <= 0)
            {
                return GetErrorResponse("Please select a valid order status.");
            }

            // Parse shipping method (can be 0 for digital delivery)
            int.TryParse(model.ShippingMethodID, out var shippingMethodId);

            // Get shipping and payment methods
            var shippingMethod = shippingMethodId > 0
                ? await _shippingMethodProvider.GetAsync(shippingMethodId)
                : null;
            var paymentMethod = await _paymentMethodProvider.GetAsync(paymentMethodId);
            var orderStatus = await _orderStatusProvider.GetAsync(orderStatusId);

            if (paymentMethod == null)
            {
                return GetErrorResponse("Selected payment method not found.");
            }

            if (orderStatus == null)
            {
                return GetErrorResponse("Selected order status not found.");
            }

            // Create or find customer
            var customer = await GetOrCreateCustomerAsync(model);

            // Calculate totals
            decimal subtotal = orderItems.Sum(item => item.Quantity * item.UnitPrice);
            decimal shippingPrice = shippingMethod?.ShippingMethodPrice ?? 0;
            decimal grandTotal = subtotal + shippingPrice;

            // Generate order number
            string orderNumber = await GenerateOrderNumberAsync();

            // Create order
            var order = new OrderInfo
            {
                OrderNumber = orderNumber,
                OrderCreatedWhen = DateTime.UtcNow,
                OrderOrderStatusID = orderStatusId,
                OrderCustomerID = customer.CustomerID,
                OrderTotalPrice = subtotal,
                OrderTotalTax = 0, // Can be extended for tax calculation
                OrderTotalShipping = shippingPrice,
                OrderGrandTotal = grandTotal,
                OrderShippingMethodID = shippingMethod?.ShippingMethodID ?? 0,
                OrderShippingMethodDisplayName = shippingMethod?.ShippingMethodDisplayName ?? "Digital Delivery",
                OrderShippingMethodPrice = shippingPrice,
                OrderPaymentMethodID = paymentMethodId,
                OrderPaymentMethodDisplayName = paymentMethod.PaymentMethodDisplayName,
                OrderGUID = Guid.NewGuid()
            };

            // Add internal notes if provided
            if (!string.IsNullOrWhiteSpace(model.InternalNotes))
            {
                order.SetValue("OrderInternalNotes", model.InternalNotes);
            }

            await _orderProvider.SetAsync(order);

            // Create billing address
            var billingAddress = new OrderAddressInfo
            {
                OrderAddressOrderID = order.OrderID,
                OrderAddressType = OrderAddressType.Billing,
                OrderAddressFirstName = model.FirstName,
                OrderAddressLastName = model.LastName,
                OrderAddressEmail = model.Email,
                OrderAddressPhone = model.PhoneNumber ?? string.Empty,
                OrderAddressCompany = model.Company ?? string.Empty,
                OrderAddressLine1 = model.BillingLine1,
                OrderAddressLine2 = model.BillingLine2 ?? string.Empty,
                OrderAddressCity = model.BillingCity,
                OrderAddressZip = model.BillingPostalCode,
                OrderAddressCountryID = int.TryParse(model.BillingCountryID, out var billingCountryId) ? billingCountryId : 0,
            };
            await _orderAddressProvider.SetAsync(billingAddress);

            // Create shipping address
            var shippingAddress = new OrderAddressInfo
            {
                OrderAddressOrderID = order.OrderID,
                OrderAddressType = OrderAddressType.Shipping,
                OrderAddressFirstName = model.FirstName,
                OrderAddressLastName = model.LastName,
                OrderAddressEmail = model.Email,
                OrderAddressPhone = model.PhoneNumber ?? string.Empty,
                OrderAddressCompany = model.Company ?? string.Empty,
                OrderAddressLine1 = model.ShippingSameAsBilling ? model.BillingLine1 : (model.ShippingLine1 ?? model.BillingLine1),
                OrderAddressLine2 = model.ShippingSameAsBilling ? (model.BillingLine2 ?? string.Empty) : (model.ShippingLine2 ?? string.Empty),
                OrderAddressCity = model.ShippingSameAsBilling ? model.BillingCity : (model.ShippingCity ?? model.BillingCity),
                OrderAddressZip = model.ShippingSameAsBilling ? model.BillingPostalCode : (model.ShippingPostalCode ?? model.BillingPostalCode),
                OrderAddressCountryID = model.ShippingSameAsBilling
                    ? billingCountryId
                    : (int.TryParse(model.ShippingCountryID, out var shippingCountryId) ? shippingCountryId : billingCountryId),
            };
            await _orderAddressProvider.SetAsync(shippingAddress);

            // Create order items
            foreach (var item in orderItems)
            {
                var orderItem = new OrderItemInfo
                {
                    OrderItemOrderID = order.OrderID,
                    OrderItemName = item.Name,
                    OrderItemSKU = item.SKU,
                    OrderItemQuantity = item.Quantity,
                    OrderItemUnitPrice = item.UnitPrice,
                    OrderItemTotalPrice = item.Quantity * item.UnitPrice,
                    OrderItemTotalTax = 0,
                    OrderItemTaxRate = 0
                };
                await _orderItemProvider.SetAsync(orderItem);
            }

            var successMessage = $"Order {orderNumber} created successfully! Total: {grandTotal:C2}";

            if (model.MarkAsPaid)
            {
                successMessage += " (Marked as paid)";
            }

            return GetSuccessResponse(successMessage);
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error creating order: {ex.Message}");
        }
    }

    private async Task<CustomerInfo> GetOrCreateCustomerAsync(OrderCreateViewModel model)
    {
        // Try to find existing customer by email
        var existingCustomers = await _customerProvider.Get()
            .WhereEquals(nameof(CustomerInfo.CustomerEmail), model.Email)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        var existingCustomer = existingCustomers.FirstOrDefault();
        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        // Create new customer
        var customer = new CustomerInfo
        {
            CustomerFirstName = model.FirstName,
            CustomerLastName = model.LastName,
            CustomerEmail = model.Email,
            CustomerPhone = model.PhoneNumber ?? string.Empty,
            CustomerCreatedWhen = DateTime.UtcNow
        };

        await _customerProvider.SetAsync(customer);
        return customer;
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        // Generate unique order number with date prefix
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(1000, 9999);

        // Ensure uniqueness
        var orderNumber = $"MAN-{datePrefix}-{random}";

        var existing = await _orderProvider.Get()
            .WhereEquals(nameof(OrderInfo.OrderNumber), orderNumber)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        if (existing.Any())
        {
            // If collision, add milliseconds
            orderNumber = $"MAN-{datePrefix}-{random}-{DateTime.UtcNow.Millisecond}";
        }

        return orderNumber;
    }

    private ICommandResponse GetSuccessResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
        response.AddSuccessMessage(message);
        return response;
    }

    private ICommandResponse GetErrorResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
        response.AddErrorMessage(message);
        return response;
    }
}
