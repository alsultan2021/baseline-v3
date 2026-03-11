using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// Renders the checkout form.
/// </summary>
public class CheckoutViewComponent(ICartService cartService) : ViewComponent
{
    /// <summary>
    /// Renders the checkout form.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await cartService.GetCartAsync();

        if (cart?.ItemCount == 0)
        {
            return View("EmptyCart");
        }

        var model = new CheckoutViewModel
        {
            OrderSummary = new OrderSummaryViewModel
            {
                Items = cart?.Items?.Select(i => new OrderItemViewModel
                {
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.LineTotal.Amount
                }).ToList() ?? [],
                Subtotal = cart?.Totals.Subtotal.Amount ?? 0,
                Tax = cart?.Totals.Tax.Amount ?? 0,
                Shipping = cart?.Totals.Shipping.Amount ?? 0,
                Discount = cart?.Totals.Discount.Amount ?? 0,
                Total = cart?.Totals.Total.Amount ?? 0
            }
        };

        return View(model);
    }
}

/// <summary>
/// View model for checkout.
/// </summary>
public class CheckoutViewModel
{
    // Billing address
    public string? BillingFirstName { get; set; }
    public string? BillingLastName { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingPhone { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingAddress2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }

    // Shipping address
    public bool SameAsShipping { get; set; } = true;
    public string? ShippingFirstName { get; set; }
    public string? ShippingLastName { get; set; }
    public string? ShippingAddress1 { get; set; }
    public string? ShippingAddress2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }

    // Order summary
    public OrderSummaryViewModel OrderSummary { get; set; } = new();

    // Status
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Order summary view model.
/// </summary>
public class OrderSummaryViewModel
{
    public List<OrderItemViewModel> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Order item view model.
/// </summary>
public class OrderItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
