using Baseline.DigitalMarketing.Interfaces;

namespace Baseline.DigitalMarketing;

/// <summary>
/// Extension methods for logging ecommerce-related activities.
/// </summary>
public static class EcommerceActivityExtensions
{
    /// <summary>
    /// Standard activity type code names for ecommerce.
    /// </summary>
    public static class ActivityTypes
    {
        public const string Purchase = "purchase";
        public const string AddToCart = "addtocart";
        public const string RemoveFromCart = "removefromcart";
        public const string ViewProduct = "productview";
        public const string ViewCategory = "categoryview";
        public const string BeginCheckout = "begincheckout";
        public const string CartAbandonment = "cartabandonment";
        public const string WishlistAdd = "wishlistadd";
        public const string CouponApplied = "couponapplied";
    }

    /// <summary>
    /// Logs a purchase activity after successful order completion.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="orderNumber">The order number/ID.</param>
    /// <param name="orderTotal">The total order amount.</param>
    /// <param name="currency">The currency code (e.g., "CAD", "USD").</param>
    /// <param name="itemCount">Number of items purchased.</param>
    public static Task LogPurchaseAsync(
        this IActivityLoggingService service,
        string orderNumber,
        decimal orderTotal,
        string currency = "CAD",
        int itemCount = 0)
    {
        var data = new Dictionary<string, string>
        {
            ["ordernumber"] = orderNumber,
            ["ordertotal"] = orderTotal.ToString("F2"),
            ["currency"] = currency,
            ["itemcount"] = itemCount.ToString()
        };

        return service.LogCustomActivityAsync(ActivityTypes.Purchase, data);
    }

    /// <summary>
    /// Logs an add to cart activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="productSku">The product SKU.</param>
    /// <param name="productName">The product name.</param>
    /// <param name="quantity">The quantity added.</param>
    /// <param name="price">The unit price.</param>
    public static Task LogAddToCartAsync(
        this IActivityLoggingService service,
        string productSku,
        string productName,
        int quantity = 1,
        decimal? price = null)
    {
        var data = new Dictionary<string, string>
        {
            ["sku"] = productSku,
            ["product"] = productName,
            ["quantity"] = quantity.ToString()
        };

        if (price.HasValue)
        {
            data["price"] = price.Value.ToString("F2");
        }

        return service.LogCustomActivityAsync(ActivityTypes.AddToCart, data);
    }

    /// <summary>
    /// Logs a remove from cart activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="productSku">The product SKU.</param>
    /// <param name="productName">The product name.</param>
    /// <param name="quantity">The quantity removed.</param>
    public static Task LogRemoveFromCartAsync(
        this IActivityLoggingService service,
        string productSku,
        string productName,
        int quantity = 1)
    {
        var data = new Dictionary<string, string>
        {
            ["sku"] = productSku,
            ["product"] = productName,
            ["quantity"] = quantity.ToString()
        };

        return service.LogCustomActivityAsync(ActivityTypes.RemoveFromCart, data);
    }

    /// <summary>
    /// Logs a product view activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="productSku">The product SKU.</param>
    /// <param name="productName">The product name.</param>
    /// <param name="productUrl">The product page URL.</param>
    public static Task LogProductViewAsync(
        this IActivityLoggingService service,
        string productSku,
        string productName,
        string? productUrl = null)
    {
        var data = new Dictionary<string, string>
        {
            ["sku"] = productSku,
            ["product"] = productName
        };

        if (!string.IsNullOrEmpty(productUrl))
        {
            data["url"] = productUrl;
        }

        return service.LogCustomActivityAsync(ActivityTypes.ViewProduct, data);
    }

    /// <summary>
    /// Logs a category view activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="categoryName">The category name.</param>
    /// <param name="categoryUrl">The category page URL.</param>
    public static Task LogCategoryViewAsync(
        this IActivityLoggingService service,
        string categoryName,
        string? categoryUrl = null)
    {
        var data = new Dictionary<string, string>
        {
            ["category"] = categoryName
        };

        if (!string.IsNullOrEmpty(categoryUrl))
        {
            data["url"] = categoryUrl;
        }

        return service.LogCustomActivityAsync(ActivityTypes.ViewCategory, data);
    }

    /// <summary>
    /// Logs a begin checkout activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="cartTotal">The cart total amount.</param>
    /// <param name="itemCount">Number of items in cart.</param>
    public static Task LogBeginCheckoutAsync(
        this IActivityLoggingService service,
        decimal cartTotal,
        int itemCount)
    {
        var data = new Dictionary<string, string>
        {
            ["carttotal"] = cartTotal.ToString("F2"),
            ["itemcount"] = itemCount.ToString()
        };

        return service.LogCustomActivityAsync(ActivityTypes.BeginCheckout, data);
    }

    /// <summary>
    /// Logs a cart abandonment activity (typically called by background job).
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="cartTotal">The abandoned cart total.</param>
    /// <param name="itemCount">Number of items abandoned.</param>
    /// <param name="hoursAbandoned">Hours since last cart activity.</param>
    public static Task LogCartAbandonmentAsync(
        this IActivityLoggingService service,
        decimal cartTotal,
        int itemCount,
        int hoursAbandoned = 24)
    {
        var data = new Dictionary<string, string>
        {
            ["carttotal"] = cartTotal.ToString("F2"),
            ["itemcount"] = itemCount.ToString(),
            ["hoursabandoned"] = hoursAbandoned.ToString()
        };

        return service.LogCustomActivityAsync(ActivityTypes.CartAbandonment, data);
    }

    /// <summary>
    /// Logs a coupon applied activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="couponCode">The coupon code used.</param>
    /// <param name="discountAmount">The discount amount applied.</param>
    public static Task LogCouponAppliedAsync(
        this IActivityLoggingService service,
        string couponCode,
        decimal discountAmount)
    {
        var data = new Dictionary<string, string>
        {
            ["couponcode"] = couponCode,
            ["discount"] = discountAmount.ToString("F2")
        };

        return service.LogCustomActivityAsync(ActivityTypes.CouponApplied, data);
    }

    /// <summary>
    /// Logs a wishlist add activity.
    /// </summary>
    /// <param name="service">The activity logging service.</param>
    /// <param name="productSku">The product SKU.</param>
    /// <param name="productName">The product name.</param>
    public static Task LogWishlistAddAsync(
        this IActivityLoggingService service,
        string productSku,
        string productName)
    {
        var data = new Dictionary<string, string>
        {
            ["sku"] = productSku,
            ["product"] = productName
        };

        return service.LogCustomActivityAsync(ActivityTypes.WishlistAdd, data);
    }
}
