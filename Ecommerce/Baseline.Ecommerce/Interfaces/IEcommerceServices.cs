namespace Baseline.Ecommerce;

/// <summary>
/// Service for managing shopping cart operations.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Gets the current cart.
    /// </summary>
    Task<Cart> GetCartAsync();

    /// <summary>
    /// Gets the underlying Kentico Commerce ShoppingCartInfo.
    /// Use this for checkout flows that require the native cart type.
    /// </summary>
    /// <returns>The ShoppingCartInfo or null if no cart exists.</returns>
    Task<CMS.Commerce.ShoppingCartInfo?> GetNativeCartAsync();

    /// <summary>
    /// Checks if the cart has any items.
    /// </summary>
    Task<bool> HasItemsAsync();

    /// <summary>
    /// Adds an item to the cart.
    /// </summary>
    Task<CartResult> AddItemAsync(AddToCartRequest request);

    /// <summary>
    /// Updates the quantity of a cart item.
    /// </summary>
    Task<CartResult> UpdateQuantityAsync(Guid cartItemId, int quantity);

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    Task<CartResult> RemoveItemAsync(Guid cartItemId);

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    Task<CartResult> ClearCartAsync();

    /// <summary>
    /// Applies a discount code to the cart.
    /// </summary>
    Task<DiscountResult> ApplyDiscountAsync(string code);

    /// <summary>
    /// Removes a discount from the cart.
    /// </summary>
    Task<CartResult> RemoveDiscountAsync(string code);

    /// <summary>
    /// Gets the cart item count.
    /// </summary>
    Task<int> GetItemCountAsync();

    /// <summary>
    /// Validates the cart before checkout.
    /// </summary>
    Task<CartValidationResult> ValidateCartAsync();
}

/// <summary>
/// Service for managing checkout workflow.
/// </summary>
public interface ICheckoutService
{
    /// <summary>
    /// Gets the current checkout session.
    /// </summary>
    Task<CheckoutSession?> GetSessionAsync();

    /// <summary>
    /// Starts a new checkout session.
    /// </summary>
    Task<CheckoutSession> StartCheckoutAsync();

    /// <summary>
    /// Sets the shipping address.
    /// </summary>
    Task<CheckoutResult> SetShippingAddressAsync(Address address);

    /// <summary>
    /// Sets the billing address.
    /// </summary>
    Task<CheckoutResult> SetBillingAddressAsync(Address address);

    /// <summary>
    /// Gets available shipping methods.
    /// </summary>
    Task<IEnumerable<ShippingMethod>> GetShippingMethodsAsync();

    /// <summary>
    /// Sets the shipping method.
    /// </summary>
    Task<CheckoutResult> SetShippingMethodAsync(Guid shippingMethodId);

    /// <summary>
    /// Gets available payment methods.
    /// </summary>
    Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync();

    /// <summary>
    /// Sets the payment method.
    /// </summary>
    Task<CheckoutResult> SetPaymentMethodAsync(Guid paymentMethodId);

    /// <summary>
    /// Completes the checkout and creates the order.
    /// </summary>
    Task<OrderResult> CompleteCheckoutAsync(CompleteCheckoutRequest request);

    /// <summary>
    /// Cancels the current checkout session.
    /// </summary>
    Task CancelCheckoutAsync();
}

/// <summary>
/// Service for managing orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    Task<Order?> GetOrderAsync(Guid orderId);

    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    Task<Order?> GetOrderByNumberAsync(string orderNumber);

    /// <summary>
    /// Gets orders for the current user.
    /// </summary>
    Task<IEnumerable<OrderSummary>> GetUserOrdersAsync(int? limit = null);

    /// <summary>
    /// Gets order history with pagination.
    /// </summary>
    Task<PagedResult<OrderSummary>> GetOrderHistoryAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Creates a new order with all related entities including promotions.
    /// Persists the order, addresses, items, and applied promotions to Kentico Commerce.
    /// </summary>
    /// <param name="request">The order creation request containing checkout data and pricing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with the created order or error details.</returns>
    Task<OrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an order if allowed.
    /// </summary>
    Task<OrderResult> CancelOrderAsync(Guid orderId, string? reason = null);

    /// <summary>
    /// Gets order status.
    /// </summary>
    Task<OrderStatus?> GetOrderStatusAsync(Guid orderId);
}

/// <summary>
/// Service for product catalog operations.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    Task<Product?> GetProductAsync(int productId);

    /// <summary>
    /// Gets a product by SKU.
    /// </summary>
    Task<Product?> GetProductBySkuAsync(string sku);

    /// <summary>
    /// Gets products by category.
    /// </summary>
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId);

    /// <summary>
    /// Searches products.
    /// </summary>
    Task<PagedResult<Product>> SearchProductsAsync(ProductSearchRequest request);

    /// <summary>
    /// Gets product availability.
    /// </summary>
    Task<ProductAvailability> GetAvailabilityAsync(int productId);

    /// <summary>
    /// Gets the calculated price for a product.
    /// </summary>
    Task<ProductPrice> GetPriceAsync(int productId, int quantity = 1);
}

/// <summary>
/// Service for pricing calculations.
/// Provides both simple and Kentico-aligned pipeline calculation methods.
/// </summary>
/// <remarks>
/// TODO: Extract tax calculation into a dedicated ITaxCalculationService.
/// CalculateTaxAsync currently embeds tax logic that should be independently testable
/// and reusable by both IPricingService and the legacy IPriceCalculationService.
/// Also consider integrating with Kentico DC's tax pipeline once available.
/// See: https://docs.kentico.com/developers-and-admins/digital-commerce/digital-commerce-customization
/// </remarks>
public interface IPricingService
{
    /// <summary>
    /// Calculates prices using the full pipeline pattern.
    /// This is the primary Kentico-aligned method.
    /// </summary>
    /// <param name="request">The calculation request with items and mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete calculation result with itemized prices and totals.</returns>
    Task<PriceCalculationResult> CalculateAsync(
        PriceCalculationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the price for a product.
    /// </summary>
    Task<Money> CalculatePriceAsync(int productId, int quantity = 1);

    /// <summary>
    /// Calculates tax for an amount.
    /// </summary>
    Task<Money> CalculateTaxAsync(Money amount, Address? shippingAddress = null);

    /// <summary>
    /// Calculates shipping cost.
    /// </summary>
    Task<Money> CalculateShippingAsync(Cart cart, Guid shippingMethodId);

    /// <summary>
    /// Calculates cart totals.
    /// </summary>
    Task<CartTotals> CalculateCartTotalsAsync(Cart cart);

    /// <summary>
    /// Formats a money value for display using database-backed currency settings.
    /// Prefers ICurrencyService for formatting with proper currency symbols and patterns.
    /// </summary>
    /// <param name="amount">The money value to format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Formatted string representation of the money value.</returns>
    Task<string> FormatMoneyAsync(Money amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats a money value for display (synchronous fallback).
    /// For database-backed currency formatting, prefer FormatMoneyAsync.
    /// </summary>
    string FormatMoney(Money amount);
}

#region Subscription Services

/// <summary>
/// Service for managing newsletter subscriptions.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Subscribes an email to a newsletter.
    /// </summary>
    Task<SubscriptionResult> SubscribeAsync(SubscriptionRequest request);

    /// <summary>
    /// Unsubscribes an email from a newsletter.
    /// </summary>
    Task<SubscriptionResult> UnsubscribeAsync(string email, string? newsletterName = null);

    /// <summary>
    /// Unsubscribes using a token from unsubscribe link.
    /// </summary>
    Task<SubscriptionResult> UnsubscribeByTokenAsync(string token);

    /// <summary>
    /// Confirms a subscription (double opt-in).
    /// </summary>
    Task<SubscriptionResult> ConfirmSubscriptionAsync(string token);

    /// <summary>
    /// Gets subscription status for an email.
    /// </summary>
    Task<SubscriptionStatus?> GetStatusAsync(string email, string? newsletterName = null);

    /// <summary>
    /// Updates subscriber preferences.
    /// </summary>
    Task<SubscriptionResult> UpdatePreferencesAsync(string email, SubscriberPreferences preferences);

    /// <summary>
    /// Gets all newsletters available for subscription.
    /// </summary>
    Task<IEnumerable<NewsletterInfo>> GetNewslettersAsync();

    /// <summary>
    /// Gets subscriptions for an email.
    /// </summary>
    Task<IReadOnlyList<SubscriptionInfo>> GetSubscriptionsAsync(string email);

    /// <summary>
    /// Validates an unsubscribe request.
    /// </summary>
    Task<UnsubscribeValidationResult> ValidateUnsubscribeRequestAsync(string email, string hash);
}

/// <summary>
/// Service for managing subscribers (admin operations).
/// </summary>
public interface ISubscriberService
{
    /// <summary>
    /// Gets a subscriber by email.
    /// </summary>
    Task<Subscriber?> GetByEmailAsync(string email);

    /// <summary>
    /// Gets a subscriber by ID.
    /// </summary>
    Task<Subscriber?> GetByIdAsync(int subscriberId);

    /// <summary>
    /// Gets all subscribers with filtering and pagination.
    /// </summary>
    Task<SubscriberSearchResult> SearchAsync(SubscriberQuery query);

    /// <summary>
    /// Updates a subscriber.
    /// </summary>
    Task<SubscriptionResult> UpdateAsync(Subscriber subscriber);

    /// <summary>
    /// Deletes a subscriber.
    /// </summary>
    Task<SubscriptionResult> DeleteAsync(int subscriberId);

    /// <summary>
    /// Imports subscribers from a file.
    /// </summary>
    Task<ImportSubscribersResult> ImportAsync(Stream fileStream, string format, string newsletterName);

    /// <summary>
    /// Exports subscribers to a file.
    /// </summary>
    Task<Stream> ExportAsync(string format, SubscriberQuery? query = null);
}

/// <summary>
/// Service for sending newsletter emails.
/// </summary>
public interface INewsletterEmailService
{
    /// <summary>
    /// Sends a newsletter issue to all subscribers.
    /// </summary>
    Task<SendResult> SendNewsletterAsync(int issueId);

    /// <summary>
    /// Sends a test email.
    /// </summary>
    Task<SendResult> SendTestAsync(int issueId, string testEmail);

    /// <summary>
    /// Schedules a newsletter for sending.
    /// </summary>
    Task<SendResult> ScheduleAsync(int issueId, DateTimeOffset sendAt);

    /// <summary>
    /// Cancels a scheduled newsletter.
    /// </summary>
    Task<SendResult> CancelScheduledAsync(int issueId);

    /// <summary>
    /// Gets sending status for an issue.
    /// </summary>
    Task<NewsletterSendStatus?> GetSendStatusAsync(int issueId);
}

/// <summary>
/// Service for newsletter analytics.
/// </summary>
public interface INewsletterAnalyticsService
{
    /// <summary>
    /// Gets analytics for a newsletter issue.
    /// </summary>
    Task<IssueAnalytics> GetIssueAnalyticsAsync(int issueId);

    /// <summary>
    /// Gets overall newsletter analytics.
    /// </summary>
    Task<NewsletterAnalytics> GetNewsletterAnalyticsAsync(string newsletterName, DateTimeOffset? from = null, DateTimeOffset? to = null);

    /// <summary>
    /// Records an email open.
    /// </summary>
    Task RecordOpenAsync(string trackingId);

    /// <summary>
    /// Records a link click.
    /// </summary>
    Task RecordClickAsync(string trackingId, string url);

    /// <summary>
    /// Gets subscriber engagement score.
    /// </summary>
    Task<double> GetEngagementScoreAsync(int subscriberId);
}

#endregion

#region Paid Subscription/Billing Services

/// <summary>
/// Service for managing paid subscription plans (SaaS billing).
/// </summary>
public interface ISubscriptionPlanService
{
    /// <summary>
    /// Gets all active subscription plans.
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific plan by ID.
    /// </summary>
    Task<SubscriptionPlan?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a plan by code.
    /// </summary>
    Task<SubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plans by tier level.
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetPlansByTierAsync(int tierLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the featured/recommended plan.
    /// </summary>
    Task<SubscriptionPlan?> GetFeaturedPlanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares plans for display.
    /// </summary>
    Task<PlanComparison> ComparePlansAsync(IEnumerable<int> planIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing customer billing subscriptions (SaaS).
/// </summary>
public interface IBillingSubscriptionService
{
    /// <summary>
    /// Gets the current user's active subscription.
    /// </summary>
    Task<CustomerSubscription?> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a subscription by ID.
    /// </summary>
    Task<CustomerSubscription?> GetSubscriptionByIdAsync(int subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscriptions for a customer.
    /// </summary>
    Task<IEnumerable<CustomerSubscription>> GetCustomerSubscriptionsAsync(int customerId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscription by external ID (e.g., Stripe subscription ID).
    /// </summary>
    Task<CustomerSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new subscription.
    /// </summary>
    Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the subscription plan (upgrade/downgrade).
    /// </summary>
    Task<ChangePlanResult> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    Task<CancelSubscriptionResult> CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a cancelled subscription.
    /// </summary>
    Task<CreateSubscriptionResult> ReactivateSubscriptionAsync(int subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a subscription.
    /// </summary>
    Task<CustomerSubscription?> PauseSubscriptionAsync(PauseSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused subscription.
    /// </summary>
    Task<CustomerSubscription?> ResumeSubscriptionAsync(int subscriptionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for subscription coupon management.
/// </summary>
public interface ISubscriptionCouponService
{
    /// <summary>
    /// Gets a coupon by code.
    /// </summary>
    Task<Coupon?> GetCouponByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscription-specific settings for a coupon.
    /// </summary>
    Task<SubscriptionCouponSettings?> GetCouponSettingsAsync(int couponId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a coupon for a specific subscription plan.
    /// </summary>
    Task<CouponValidationResult> ValidateCouponAsync(string code, int planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates discount amount for a coupon on a subscription plan.
    /// </summary>
    Task<decimal> CalculateDiscountAsync(Coupon coupon, decimal originalPrice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records coupon usage for a subscription.
    /// </summary>
    Task RecordCouponUsageAsync(int couponId, int subscriptionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing subscription invoices.
/// </summary>
public interface ISubscriptionInvoiceService
{
    /// <summary>
    /// Gets invoices for a subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionInvoice>> GetSubscriptionInvoicesAsync(int subscriptionId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices for a customer.
    /// </summary>
    Task<IEnumerable<SubscriptionInvoice>> GetCustomerInvoicesAsync(int customerId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific invoice.
    /// </summary>
    Task<SubscriptionInvoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoice by external ID.
    /// </summary>
    Task<SubscriptionInvoice?> GetInvoiceByExternalIdAsync(string externalInvoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the upcoming invoice for a subscription.
    /// </summary>
    Task<SubscriptionInvoice?> GetUpcomingInvoiceAsync(int subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads invoice PDF.
    /// </summary>
    Task<byte[]?> DownloadInvoicePdfAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends invoice email to customer.
    /// </summary>
    Task<bool> SendInvoiceEmailAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an invoice as paid.
    /// </summary>
    Task<SubscriptionInvoice?> MarkInvoicePaidAsync(int invoiceId, decimal amountPaid, string? paymentReference = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids an invoice.
    /// </summary>
    Task<bool> VoidInvoiceAsync(int invoiceId, string? reason = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for subscription payment providers (e.g., Stripe, PayPal).
/// </summary>
public interface ISubscriptionPaymentProvider
{
    /// <summary>
    /// Provider identifier.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Provider display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether the provider is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Creates a customer in the payment provider.
    /// </summary>
    Task<ProviderCustomerResult> CreateCustomerAsync(ProviderCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer in the payment provider.
    /// </summary>
    Task<ProviderCustomerResult> UpdateCustomerAsync(string externalCustomerId, ProviderCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer from the payment provider.
    /// </summary>
    Task<ProviderCustomer?> GetCustomerAsync(string externalCustomerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches a payment method to a customer.
    /// </summary>
    Task<bool> AttachPaymentMethodAsync(string externalCustomerId, string paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the default payment method for a customer.
    /// </summary>
    Task<bool> SetDefaultPaymentMethodAsync(string externalCustomerId, string paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer's payment methods.
    /// </summary>
    Task<IEnumerable<ProviderPaymentMethod>> GetPaymentMethodsAsync(string externalCustomerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a subscription in the payment provider.
    /// </summary>
    Task<ProviderSubscriptionResult> CreateSubscriptionAsync(ProviderSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a subscription in the payment provider.
    /// </summary>
    Task<ProviderSubscriptionResult> UpdateSubscriptionAsync(string externalSubscriptionId, ProviderSubscriptionUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a subscription in the payment provider.
    /// </summary>
    Task<bool> CancelSubscriptionAsync(string externalSubscriptionId, bool cancelImmediately = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available prices/plans from the payment provider.
    /// </summary>
    Task<IEnumerable<ProviderPlan>> ListExternalPlansAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for tracking subscription usage (metered billing).
/// </summary>
public interface ISubscriptionUsageService
{
    /// <summary>
    /// Records a usage event.
    /// </summary>
    Task<bool> RecordUsageAsync(int subscriptionId, string meterId, decimal quantity, string? idempotencyKey = null, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current usage for a subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionUsage>> GetCurrentUsageAsync(int subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage history for a subscription.
    /// </summary>
    Task<IEnumerable<SubscriptionUsage>> GetUsageHistoryAsync(int subscriptionId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage summary for billing period.
    /// </summary>
    Task<UsageSummary> GetUsageSummaryAsync(int subscriptionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for sending subscription-related notifications.
/// </summary>
public interface ISubscriptionNotificationService
{
    /// <summary>
    /// Sends subscription confirmation email.
    /// </summary>
    Task SendSubscriptionConfirmationAsync(int subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends payment failed notification.
    /// </summary>
    Task SendPaymentFailedAsync(int subscriptionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends subscription renewal reminder.
    /// </summary>
    Task SendRenewalReminderAsync(int subscriptionId, int daysUntilRenewal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends trial ending notification.
    /// </summary>
    Task SendTrialEndingAsync(int subscriptionId, int daysRemaining, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends subscription cancelled confirmation.
    /// </summary>
    Task SendCancellationConfirmationAsync(int subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends invoice notification.
    /// </summary>
    Task SendInvoiceNotificationAsync(int invoiceId, CancellationToken cancellationToken = default);
}

#endregion

#region Order Notification Services

/// <summary>
/// Service for sending order-related notifications via email.
/// </summary>
public interface IOrderNotificationService
{
    /// <summary>
    /// Sends an order confirmation email to the customer.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendOrderConfirmationAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an order status update email to the customer.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="newStatus">The new order status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendOrderStatusUpdateAsync(string orderNumber, OrderStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a shipping notification email to the customer.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="trackingNumber">The shipment tracking number.</param>
    /// <param name="carrier">The shipping carrier name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendShippingNotificationAsync(string orderNumber, string trackingNumber, string? carrier = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an order cancellation confirmation email to the customer.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendOrderCancelledAsync(string orderNumber, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a refund confirmation email to the customer.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="refundAmount">The refund amount.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendRefundConfirmationAsync(string orderNumber, Money refundAmount, CancellationToken cancellationToken = default);
}

#endregion

#region Shopping Cart Transfer Services

/// <summary>
/// Service for transferring shopping cart items between guest and authenticated sessions.
/// </summary>
public interface IShoppingCartTransferService
{
    /// <summary>
    /// Stores the current cart items in session before user authentication.
    /// </summary>
    Task StoreGuestCartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores previously stored cart items after user authentication.
    /// </summary>
    Task RestoreGuestCartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there are stored guest cart items available for transfer.
    /// </summary>
    bool HasStoredGuestCart();

    /// <summary>
    /// Gets the count of stored guest cart items.
    /// </summary>
    int GetStoredGuestCartItemCount();

    /// <summary>
    /// Stores checkout data in session.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="data">The data to store.</param>
    void StoreCheckoutData(string key, object data);

    /// <summary>
    /// Retrieves checkout data from session.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="key">The data key.</param>
    T? GetCheckoutData<T>(string key) where T : class;

    /// <summary>
    /// Clears specific checkout data from session.
    /// </summary>
    /// <param name="key">The data key to clear.</param>
    void ClearCheckoutData(string key);

    /// <summary>
    /// Clears all checkout data from session.
    /// </summary>
    void ClearAllCheckoutData();
}

#endregion

#region Wallet Services

/// <summary>
/// Service for managing member wallets and account balances.
/// Supports store credit, loyalty points, prepaid funds, and gift cards.
/// </summary>
public interface IWalletService
{
    #region Wallet Management

    /// <summary>
    /// Gets or creates a wallet for a member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="walletType">Type of wallet (defaults to StoreCredit).</param>
    /// <param name="currencyCode">Currency code (defaults to site default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wallet summary.</returns>
    Task<WalletSummary> GetOrCreateWalletAsync(
        int memberId,
        string? walletType = null,
        string? currencyCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all wallets for a member.
    /// </summary>
    Task<IEnumerable<WalletSummary>> GetMemberWalletsAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a wallet by its GUID.
    /// </summary>
    Task<WalletSummary?> GetWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total available balance across all wallets for checkout.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="currencyCode">Target currency for conversion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total available balance in the specified currency.</returns>
    Task<Money> GetTotalAvailableBalanceAsync(
        int memberId,
        string currencyCode,
        CancellationToken cancellationToken = default);

    #endregion

    #region Transactions

    /// <summary>
    /// Deposits funds into a wallet.
    /// </summary>
    Task<WalletOperationResult> DepositAsync(
        WalletDepositRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws funds from a wallet (for purchases).
    /// </summary>
    Task<WalletOperationResult> WithdrawAsync(
        WalletWithdrawalRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a hold on funds (reserves for pending transaction).
    /// </summary>
    Task<WalletOperationResult> HoldAsync(
        WalletHoldRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a hold on funds.
    /// </summary>
    Task<WalletOperationResult> ReleaseHoldAsync(
        int memberId,
        decimal amount,
        string? reference = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a held amount (converts hold to actual withdrawal).
    /// </summary>
    Task<WalletOperationResult> CaptureHoldAsync(
        int memberId,
        decimal amount,
        int orderId,
        string? reference = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a previous purchase back to the wallet.
    /// </summary>
    Task<WalletOperationResult> RefundAsync(
        int memberId,
        decimal amount,
        int orderId,
        string? description = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Transaction History

    /// <summary>
    /// Gets transaction history for a member.
    /// </summary>
    Task<PagedResult<WalletTransactionSummary>> GetTransactionHistoryAsync(
        int memberId,
        int page = 1,
        int pageSize = 20,
        string? walletType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific transaction by ID.
    /// </summary>
    Task<WalletTransactionSummary?> GetTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a member has sufficient balance for an amount.
    /// </summary>
    Task<bool> HasSufficientBalanceAsync(
        int memberId,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a wallet can perform transactions.
    /// </summary>
    Task<WalletValidationResult> ValidateWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Admin Operations

    /// <summary>
    /// Adjusts wallet balance (admin operation with audit).
    /// </summary>
    Task<WalletOperationResult> AdjustBalanceAsync(
        WalletAdjustmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Freezes a wallet (fraud prevention).
    /// </summary>
    Task<WalletOperationResult> FreezeWalletAsync(
        Guid walletId,
        string reason,
        int adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unfreezes a wallet.
    /// </summary>
    Task<WalletOperationResult> UnfreezeWalletAsync(
        Guid walletId,
        int adminUserId,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Service for integrating wallet payments into checkout flow.
/// </summary>
public interface IWalletCheckoutService
{
    /// <summary>
    /// Gets wallet payment options for checkout.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="orderTotal">Total order amount.</param>
    /// <param name="currencyCode">Order currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available wallet payment options.</returns>
    Task<WalletPaymentOptions> GetPaymentOptionsAsync(
        int memberId,
        decimal orderTotal,
        string currencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies wallet balance to an order.
    /// </summary>
    /// <param name="request">The payment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of applying wallet payment.</returns>
    Task<WalletPaymentResult> ApplyWalletPaymentAsync(
        ApplyWalletPaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending wallet payment (order cancelled).
    /// </summary>
    Task<WalletOperationResult> CancelWalletPaymentAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a wallet payment (order completed).
    /// </summary>
    Task<WalletOperationResult> ConfirmWalletPaymentAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes wallet refund for a cancelled/returned order.
    /// </summary>
    Task<WalletOperationResult> ProcessRefundToWalletAsync(
        int orderId,
        decimal amount,
        string? reason = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing loyalty points programs.
/// Extends wallet functionality with earning rules and redemption options.
/// </summary>
public interface ILoyaltyPointsService
{
    /// <summary>
    /// Gets loyalty points balance for a member.
    /// </summary>
    Task<LoyaltyPointsBalance> GetBalanceAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Awards points for a completed order.
    /// </summary>
    Task<WalletOperationResult> AwardPointsForOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeems points for an order discount.
    /// </summary>
    Task<WalletOperationResult> RedeemPointsAsync(
        int memberId,
        int points,
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets points earning preview for cart.
    /// </summary>
    Task<int> CalculateEarnablePointsAsync(
        Cart cart,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets points redemption value.
    /// </summary>
    Task<Money> CalculateRedemptionValueAsync(
        int points,
        string currencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets loyalty tier for a member.
    /// </summary>
    Task<LoyaltyTier> GetMemberTierAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Awards bonus points (promotions, referrals, etc.).
    /// </summary>
    Task<WalletOperationResult> AwardBonusPointsAsync(
        AwardBonusPointsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates points per currency unit spent.
    /// </summary>
    /// <param name="memberId">Member ID for tier-based multipliers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Points earned per unit of currency.</returns>
    Task<decimal> GetPointsPerCurrencyUnitAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the monetary value of one loyalty point.
    /// </summary>
    /// <param name="currencyCode">Currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Value per point in the specified currency.</returns>
    Task<decimal> GetPointValueAsync(
        string currencyCode,
        CancellationToken cancellationToken = default);
}

#endregion
