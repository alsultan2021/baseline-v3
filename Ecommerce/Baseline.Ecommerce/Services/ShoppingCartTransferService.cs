using System.Text.Json;
using CMS.Commerce;
using Ecommerce.Extensions;
using Ecommerce.Models;
using Kentico.Commerce.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of <see cref="IShoppingCartTransferService"/>.
/// Handles transferring shopping cart items between guest and authenticated sessions.
/// </summary>
public class ShoppingCartTransferService(
    ICurrentShoppingCartRetriever currentShoppingCartRetriever,
    ICurrentShoppingCartCreator currentShoppingCartCreator,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ShoppingCartTransferService> logger) : IShoppingCartTransferService
{
    private const string GuestCartKey = "Baseline_GuestCartItems";
    private const string CheckoutPrefix = "Baseline_Checkout_";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public async Task StoreGuestCartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentCart = await currentShoppingCartRetriever.Get(cancellationToken);
            if (currentCart == null)
            {
                logger.LogDebug("No current cart found to store");
                return;
            }

            var cartData = currentCart.GetShoppingCartDataModel();
            if (cartData.Items.Count == 0)
            {
                logger.LogDebug("Cart is empty, nothing to store");
                return;
            }

            var cartItems = cartData.Items.Select(item => new StoredCartItem
            {
                ContentItemId = item.ContentItemId,
                VariantId = item.VariantId,
                Quantity = item.Quantity
            }).ToList();

            SetSessionJson(GuestCartKey, cartItems);
            logger.LogDebug("Stored {Count} guest cart items", cartItems.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store guest cart");
        }
    }

    /// <inheritdoc/>
    public async Task RestoreGuestCartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var storedItems = GetSessionJson<List<StoredCartItem>>(GuestCartKey);
            if (storedItems == null || storedItems.Count == 0)
            {
                logger.LogDebug("No stored guest cart items to restore");
                return;
            }

            var userCart = await currentShoppingCartRetriever.Get(cancellationToken)
                         ?? await currentShoppingCartCreator.Create();

            var cartData = userCart.GetShoppingCartDataModel();

            foreach (var item in storedItems)
            {
                var existingItem = cartData.Items.FirstOrDefault(x =>
                    x.ContentItemId == item.ContentItemId && x.VariantId == item.VariantId);

                if (existingItem != null)
                {
                    existingItem.Quantity += item.Quantity;
                }
                else
                {
                    cartData.Items.Add(new ShoppingCartDataItem
                    {
                        ContentItemId = item.ContentItemId,
                        Quantity = item.Quantity,
                        VariantId = item.VariantId
                    });
                }
            }

            userCart.StoreShoppingCartDataModel(cartData);
            RemoveSession(GuestCartKey);

            logger.LogDebug("Restored {Count} guest cart items to authenticated user cart", storedItems.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore guest cart");
        }
    }

    /// <inheritdoc/>
    public bool HasStoredGuestCart() =>
        !string.IsNullOrEmpty(httpContextAccessor.HttpContext?.Session.GetString(GuestCartKey));

    /// <inheritdoc/>
    public int GetStoredGuestCartItemCount() =>
        GetSessionJson<List<StoredCartItem>>(GuestCartKey)?.Sum(item => item.Quantity) ?? 0;

    /// <inheritdoc/>
    public void StoreCheckoutData(string key, object data)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        SetSessionJson($"{CheckoutPrefix}{key}", data);
    }

    /// <inheritdoc/>
    public T? GetCheckoutData<T>(string key) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return GetSessionJson<T>($"{CheckoutPrefix}{key}");
    }

    /// <inheritdoc/>
    public void ClearCheckoutData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        RemoveSession($"{CheckoutPrefix}{key}");
    }

    /// <inheritdoc/>
    public void ClearAllCheckoutData()
    {
        var session = httpContextAccessor.HttpContext?.Session;
        if (session == null) return;

        // Clear known checkout keys
        foreach (var key in new[] { "Customer", "BillingAddress", "ShippingAddress", "PaymentMethod", "ShippingMethod", "OrderNotes" })
        {
            RemoveSession($"{CheckoutPrefix}{key}");
        }
    }

    /// <summary>
    /// Clears any stored guest cart data.
    /// </summary>
    public void ClearStoredCart() => RemoveSession(GuestCartKey);

    private void SetSessionJson<T>(string key, T data)
    {
        try
        {
            httpContextAccessor.HttpContext?.Session.SetString(key, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to serialize session data for key: {Key}", key);
        }
    }

    private T? GetSessionJson<T>(string key) where T : class
    {
        try
        {
            var json = httpContextAccessor.HttpContext?.Session.GetString(key);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize session data for key: {Key}", key);
            RemoveSession(key);
            return null;
        }
    }

    private void RemoveSession(string key) =>
        httpContextAccessor.HttpContext?.Session.Remove(key);
}

/// <summary>
/// Represents a stored cart item for transfer between sessions.
/// </summary>
public sealed class StoredCartItem
{
    /// <summary>
    /// The content item ID of the product.
    /// </summary>
    public int ContentItemId { get; set; }

    /// <summary>
    /// The variant ID if applicable.
    /// </summary>
    public int? VariantId { get; set; }

    /// <summary>
    /// The quantity of the item.
    /// </summary>
    public int Quantity { get; set; }
}
