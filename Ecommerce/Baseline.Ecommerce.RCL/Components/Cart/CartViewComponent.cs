using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// Renders the full shopping cart view.
/// </summary>
public class CartViewComponent(ICartService cartService) : ViewComponent
{
    /// <summary>
    /// Renders the full cart.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await cartService.GetCartAsync();

        var model = new CartViewModel
        {
            Items = cart?.Items.Select(i => new CartItemViewModel
            {
                Id = i.Id.ToString(),
                Name = i.ProductName,
                ImageUrl = i.ImageUrl,
                UnitPrice = i.UnitPrice.Amount,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal.Amount,
                Sku = i.Sku
            }).ToList() ?? [],
            Subtotal = cart?.Totals.Subtotal.Amount ?? 0,
            Tax = cart?.Totals.Tax.Amount ?? 0,
            Shipping = cart?.Totals.Shipping.Amount ?? 0,
            Discount = cart?.Totals.Discount.Amount ?? 0,
            Total = cart?.Totals.Total.Amount ?? 0,
            CurrencyCode = cart?.Totals.Total.Currency ?? "USD",
            IsEmpty = cart?.ItemCount == 0
        };

        return View(model);
    }
}

/// <summary>
/// View model for full cart view.
/// </summary>
public class CartViewModel
{
    public IReadOnlyList<CartItemViewModel> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsEmpty { get; set; } = true;
}

/// <summary>
/// View model for a cart item.
/// </summary>
public class CartItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? Sku { get; set; }
}
