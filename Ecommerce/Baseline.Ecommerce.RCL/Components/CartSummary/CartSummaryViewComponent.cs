using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// Renders the shopping cart summary/icon.
/// </summary>
public class CartSummaryViewComponent(ICartService cartService) : ViewComponent
{
    /// <summary>
    /// Renders the cart summary.
    /// </summary>
    /// <param name="showItemCount">Whether to show the item count badge.</param>
    /// <param name="showTotal">Whether to show the cart total.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        bool showItemCount = true,
        bool showTotal = false)
    {
        var cart = await cartService.GetCartAsync();

        var model = new CartSummaryViewModel
        {
            ItemCount = cart.ItemCount,
            Total = cart.Totals.Total.Amount,
            CurrencyCode = cart.Totals.Total.Currency,
            ShowItemCount = showItemCount,
            ShowTotal = showTotal
        };

        return View(model);
    }
}

/// <summary>
/// View model for cart summary.
/// </summary>
public class CartSummaryViewModel
{
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool ShowItemCount { get; set; } = true;
    public bool ShowTotal { get; set; }
}
