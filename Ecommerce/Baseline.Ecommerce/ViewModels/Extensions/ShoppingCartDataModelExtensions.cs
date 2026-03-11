using System.Text.Json;
using CMS.Commerce;

namespace Ecommerce.Extensions;

/// <summary>
/// Extension methods for cart models with Kentico Commerce
/// </summary>
public static class ShoppingCartDataModelExtensions
{
    /// <summary>
    /// Deserializes the shopping cart data model from the shopping cart object.
    /// </summary>
    public static Ecommerce.Models.ShoppingCartDataModel GetShoppingCartDataModel(this ShoppingCartInfo shoppingCart)
    {
        if (shoppingCart == null || string.IsNullOrEmpty(shoppingCart.ShoppingCartData))
        {
            return new Ecommerce.Models.ShoppingCartDataModel();
        }

        return JsonSerializer.Deserialize<Ecommerce.Models.ShoppingCartDataModel>(shoppingCart.ShoppingCartData)
               ?? new Ecommerce.Models.ShoppingCartDataModel();
    }

    /// <summary>
    /// Serializes the shopping cart data model and stores it in the shopping cart object.
    /// </summary>
    public static void StoreShoppingCartDataModel(this ShoppingCartInfo shoppingCart, Ecommerce.Models.ShoppingCartDataModel shoppingCartData)
    {
        shoppingCart.ShoppingCartData = JsonSerializer.Serialize(shoppingCartData);
    }

    /// <summary>
    /// Gets the item count from the shopping cart data model.
    /// </summary>
    public static int GetItemCount(this Ecommerce.Models.ShoppingCartDataModel cart)
    {
        return cart.Items?.Sum(x => x.Quantity) ?? 0;
    }
}
