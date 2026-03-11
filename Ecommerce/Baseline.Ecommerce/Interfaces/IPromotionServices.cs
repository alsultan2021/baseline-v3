namespace Baseline.Ecommerce;

/// <summary>
/// Service for managing promotions (catalog and order discounts).
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Gets all active catalog promotions.
    /// </summary>
    Task<IReadOnlyList<CatalogPromotion>> GetActiveCatalogPromotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active order promotions.
    /// </summary>
    Task<IReadOnlyList<OrderPromotion>> GetActiveOrderPromotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a catalog promotion by ID.
    /// </summary>
    Task<CatalogPromotion?> GetCatalogPromotionByIdAsync(int promotionId);

    /// <summary>
    /// Gets an order promotion by ID.
    /// </summary>
    Task<OrderPromotion?> GetOrderPromotionByIdAsync(int promotionId);

    /// <summary>
    /// Gets all catalog promotions for a product.
    /// </summary>
    Task<IReadOnlyList<CatalogPromotion>> GetPromotionsForProductAsync(int contentItemId);

    /// <summary>
    /// Validates a coupon code.
    /// </summary>
    Task<PromotionCouponValidationResult> ValidateCouponAsync(string couponCode);

    /// <summary>
    /// Applies a coupon code to a cart/checkout.
    /// </summary>
    Task<CouponApplicationResult> ApplyCouponAsync(string couponCode, Guid checkoutSessionId);

    /// <summary>
    /// Removes a coupon from a cart/checkout.
    /// </summary>
    Task<CouponApplicationResult> RemoveCouponAsync(string couponCode, Guid checkoutSessionId);

    /// <summary>
    /// Calculates the best catalog discount for a product.
    /// </summary>
    Task<CatalogDiscountResult> CalculateBestCatalogDiscountAsync(
        int contentItemId,
        decimal unitPrice,
        string? taxCategory = null,
        IEnumerable<string>? productCategories = null);

    /// <summary>
    /// Calculates the best order discount for an order.
    /// </summary>
    Task<OrderDiscountResult> CalculateBestOrderDiscountAsync(
        decimal orderSubtotal,
        int itemCount,
        Guid? customerId = null,
        IEnumerable<string>? appliedCoupons = null);

    /// <summary>
    /// Gets all active shipping promotions.
    /// </summary>
    Task<IReadOnlyList<ShippingPromotion>> GetActiveShippingPromotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active Buy X Get Y promotions.
    /// </summary>
    Task<IReadOnlyList<BuyXGetYPromotion>> GetActiveBuyXGetYPromotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shipping promotion by ID.
    /// </summary>
    Task<ShippingPromotion?> GetShippingPromotionByIdAsync(int promotionId);

    /// <summary>
    /// Gets a Buy X Get Y promotion by ID.
    /// </summary>
    Task<BuyXGetYPromotion?> GetBuyXGetYPromotionByIdAsync(int promotionId);

    /// <summary>
    /// Calculates the best shipping discount for an order.
    /// </summary>
    Task<ShippingDiscountResult> CalculateBestShippingDiscountAsync(
        decimal shippingCost,
        decimal orderSubtotal,
        int itemCount,
        string? shippingZone = null);

    /// <summary>
    /// Calculates the best Buy X Get Y discount for a product.
    /// </summary>
    Task<BuyXGetYDiscountResult> CalculateBestBuyXGetYDiscountAsync(
        int contentItemId,
        decimal unitPrice,
        int quantity,
        IEnumerable<string>? productCategories = null);
}

/// <summary>
/// Service for managing coupon codes.
/// </summary>
public interface ICouponService
{
    /// <summary>
    /// Validates a coupon code.
    /// </summary>
    Task<PromotionCouponValidationResult> ValidateAsync(string couponCode);

    /// <summary>
    /// Gets a coupon by code.
    /// </summary>
    Task<PromotionCoupon?> GetByCodeAsync(string couponCode);

    /// <summary>
    /// Gets all active coupons for a promotion.
    /// </summary>
    Task<IReadOnlyList<PromotionCoupon>> GetCouponsForPromotionAsync(int promotionId);

    /// <summary>
    /// Records a coupon redemption.
    /// </summary>
    Task<CouponRedemptionResult> RecordRedemptionAsync(string couponCode, Guid orderId);

    /// <summary>
    /// Gets the redemption count for a coupon.
    /// </summary>
    Task<int> GetRedemptionCountAsync(string couponCode);

    /// <summary>
    /// Checks if a coupon has reached its usage limit.
    /// </summary>
    Task<bool> HasReachedUsageLimitAsync(string couponCode);
}

/// <summary>
/// Service for managing product stock/inventory.
/// </summary>
public interface IProductStockService
{
    /// <summary>
    /// Gets the stock level for a product by its GUID.
    /// </summary>
    Task<ProductStock?> GetStockAsync(Guid productGuid);

    /// <summary>
    /// Gets stock levels for multiple products by their GUIDs.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ProductStock>> GetStocksAsync(IEnumerable<Guid> productGuids);

    /// <summary>
    /// Checks if a product is in stock.
    /// </summary>
    Task<bool> IsInStockAsync(Guid productGuid, decimal quantity = 1);

    /// <summary>
    /// Gets the available quantity for a product.
    /// </summary>
    Task<decimal> GetAvailableQuantityAsync(Guid productGuid);

    /// <summary>
    /// Reserves stock for a pending order.
    /// </summary>
    Task<StockReservationResult> ReserveStockAsync(Guid productGuid, decimal quantity, Guid reservationId);

    /// <summary>
    /// Releases a stock reservation.
    /// </summary>
    Task<StockReservationResult> ReleaseReservationAsync(Guid reservationId);

    /// <summary>
    /// Confirms a stock reservation (converts to actual deduction).
    /// </summary>
    Task<StockReservationResult> ConfirmReservationAsync(Guid reservationId);

    /// <summary>
    /// Decreases stock for a completed order.
    /// </summary>
    Task<StockUpdateResult> DecreaseStockAsync(Guid productGuid, decimal quantity, string reason);

    /// <summary>
    /// Increases stock (for returns or restocking).
    /// </summary>
    Task<StockUpdateResult> IncreaseStockAsync(Guid productGuid, decimal quantity, string reason);

    /// <summary>
    /// Sets the stock level for a product.
    /// </summary>
    Task<StockUpdateResult> SetStockAsync(Guid productGuid, decimal quantity, string reason);

    /// <summary>
    /// Gets products with low stock (below threshold).
    /// </summary>
    Task<IReadOnlyList<ProductStock>> GetLowStockProductsAsync(decimal threshold);

    /// <summary>
    /// Gets products that are out of stock.
    /// </summary>
    Task<IReadOnlyList<ProductStock>> GetOutOfStockProductsAsync();
}

/// <summary>
/// Service for managing cart-level inventory reservations.
/// Provides time-limited holds on product inventory during checkout.
/// </summary>
public interface IInventoryReservationService
{
    /// <summary>
    /// Creates inventory reservations for all items in a cart.
    /// </summary>
    /// <param name="cartId">The cart to reserve inventory for.</param>
    /// <param name="duration">How long the reservation should be held.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the reservation or error details.</returns>
    Task<ReservationResult> ReserveInventoryAsync(
        Guid cartId,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an existing reservation, returning items to available stock.
    /// </summary>
    /// <param name="reservationId">The reservation to release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases all reservations for a cart.
    /// </summary>
    /// <param name="cartId">The cart whose reservations should be released.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseCartReservationsAsync(Guid cartId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a reservation to a committed order, making the stock deduction permanent.
    /// </summary>
    /// <param name="reservationId">The reservation to commit.</param>
    /// <param name="orderNumber">The order number for reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the commit operation.</returns>
    Task<ReservationResult> ConvertToCommittedAsync(
        Guid reservationId,
        string orderNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing reservation by ID.
    /// </summary>
    /// <param name="reservationId">The reservation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reservation if found.</returns>
    Task<InventoryReservation?> GetReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active reservations for a cart.
    /// </summary>
    /// <param name="cartId">The cart ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active reservations for the cart.</returns>
    Task<IReadOnlyList<InventoryReservation>> GetCartReservationsAsync(
        Guid cartId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends the duration of an existing reservation.
    /// </summary>
    /// <param name="reservationId">The reservation to extend.</param>
    /// <param name="additionalDuration">Additional time to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with the updated reservation.</returns>
    Task<ReservationResult> ExtendReservationAsync(
        Guid reservationId,
        TimeSpan additionalDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes expired reservations and releases their inventory.
    /// Should be called periodically by a background job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of reservations that were expired.</returns>
    Task<int> ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all items in a cart can be reserved.
    /// Does not actually create a reservation.
    /// </summary>
    /// <param name="cartId">The cart to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating if reservation would succeed.</returns>
    Task<ReservationResult> ValidateReservationAsync(
        Guid cartId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for external tax calculation using providers like Avalara or TaxJar.
/// </summary>
public interface IExternalTaxService
{
    /// <summary>
    /// Gets the provider name (e.g., "Avalara", "TaxJar", "Internal").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets whether the tax service is configured and available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Calculates tax for the given request.
    /// </summary>
    /// <param name="request">The tax calculation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tax calculation result with amounts and breakdown.</returns>
    Task<TaxCalculationResult> CalculateTaxAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits a tax transaction (records the sale with the tax authority).
    /// Called when an order is placed.
    /// </summary>
    /// <param name="providerTransactionId">The transaction ID from a previous calculation.</param>
    /// <param name="orderNumber">The order number for reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the commit was successful.</returns>
    Task<bool> CommitTransactionAsync(
        string providerTransactionId,
        string orderNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a previously committed tax transaction (for order cancellations).
    /// </summary>
    /// <param name="providerTransactionId">The transaction ID to void.</param>
    /// <param name="reason">Reason for voiding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the void was successful.</returns>
    Task<bool> VoidTransactionAsync(
        string providerTransactionId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an address for tax purposes.
    /// </summary>
    /// <param name="address">The address to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Address validation result with corrected address if applicable.</returns>
    Task<AddressValidationResult> ValidateAddressAsync(
        Address address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tax rate for a given address (without calculating for specific items).
    /// </summary>
    /// <param name="address">The address to get rates for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined tax rate for the address.</returns>
    Task<decimal> GetTaxRateAsync(
        Address address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the tax provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the connection is healthy.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}