using Baseline.AI;
using Baseline.Ecommerce.Automation;
using Baseline.Ecommerce.Plugins;
using CMS.Commerce;
using Baseline.Ecommerce.Installers;
using Baseline.Ecommerce.Interfaces;
using Baseline.Ecommerce.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Extension methods for registering Baseline v3 Ecommerce services.
/// </summary>
public static class BaselineEcommerceServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline v3 Ecommerce services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Ecommerce options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers all core e-commerce services. Site-specific implementations
    /// can be registered after this call to override defaults:
    /// <list type="bullet">
    /// <item><description><c>IProductRepository</c> - Must be implemented by the site to query product content types</description></item>
    /// <item><description><c>IOrderNotificationService</c> - Optional: Implement for order email notifications</description></item>
    /// <item><description><c>IShoppingCartTransferService</c> - Optional: Implement for guest cart persistence</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddBaselineEcommerce(options =>
    /// {
    ///     options.EnableGuestCheckout = true;
    ///     options.Pricing.DefaultCurrency = "EUR";
    ///     options.Cart.SessionTimeoutMinutes = 2880;
    /// });
    /// 
    /// // Register site-specific product repository
    /// services.AddScoped&lt;IProductRepository, MyProductRepository&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineEcommerce(
        this IServiceCollection services,
        Action<BaselineEcommerceOptions>? configure = null)
    {
        // Register options using the Options pattern
        services.AddOptions<BaselineEcommerceOptions>()
            .Configure(opt => configure?.Invoke(opt));

        // Build options for conditional registration
        var options = new BaselineEcommerceOptions();
        configure?.Invoke(options);

        // Register cart services (requires Kentico Commerce services)
        if (options.EnableCart)
        {
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IShoppingCartTransferService, ShoppingCartTransferService>();
        }

        // Register checkout services (requires ICartService and IOrderService)
        if (options.EnableCheckout)
        {
            services.AddScoped<ICheckoutService, CheckoutService>();
        }

        // Register order services
        services.AddScoped<IOrderService, OrderService>();

        // Register product and pricing services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPricingService, PricingService>();

        // Shared Kentico DC promotion evaluation helper (used by both pricing services)
        services.AddScoped<KenticoDcPromotionHelper>();

        // Register currency, exchange rate, and tax class services
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();
        services.AddScoped<ITaxClassService, TaxClassService>();

        // Register promotion and coupon services
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ICouponService, CouponService>();

        // Register catalog promotion evaluator with complex rule engine
        services.AddScoped<ICatalogPromotionEvaluator, CatalogPromotionEvaluator>();

        // Register product stock service
        services.AddScoped<IProductStockService, ProductStockService>();

        // Register inventory reservation service for cart-level holds
        services.AddScoped<IInventoryReservationService, InventoryReservationService>();

        // Register wallet services for member balance management and checkout integration
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IWalletCheckoutService, WalletCheckoutService>();

        // Register loyalty points service
        services.AddOptions<LoyaltyPointsOptions>()
            .BindConfiguration(LoyaltyPointsOptions.SectionName);
        services.AddScoped<ILoyaltyPointsService, LoyaltyPointsService>();

        // Register subscription services (SaaS billing)
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<IBillingSubscriptionService, BillingSubscriptionService>();

        // Register no-op product data retriever for Kentico Commerce price calculation pipeline.
        // Sites should override with their own implementation that queries their product catalog.
        services.AddTransient<CMS.Commerce.IProductDataRetriever<CMS.Commerce.ProductIdentifier, CMS.Commerce.ProductData>, NoOpProductDataRetriever>();

        // Register no-op tax calculation step. Sites should register a real implementation for production.
        // Use AddTaxCalculation() extension method to register the default tax step with options.
        services.AddTransient(typeof(ITaxPriceCalculationStep<,>), typeof(NoOpTaxPriceCalculationStep<,>));

        // Register customer data retriever (no-op by default, sites should override)
        services.AddScoped<ICustomerDataRetriever, NoOpCustomerDataRetriever>();

        // Register price calculation service for v2 compatibility
        services.AddScoped<global::Ecommerce.Services.IPriceCalculationService, PriceCalculationService>();

        // Register URL provider for e-commerce page URLs
        services.AddScoped<global::Ecommerce.Services.IWebPageUrlProvider, WebPageUrlProvider>();

        // Register country/state repository for address forms
        services.AddScoped<global::Ecommerce.Services.ICountryStateRepository, CountryStateRepository>();

        // Register Tax module installer for automatic tax class creation
        services.AddSingleton<ITaxModuleInstaller, TaxModuleInstaller>();

        // Register Currency module installer for automatic currency and exchange rate data class creation
        services.AddSingleton<ICurrencyModuleInstaller, CurrencyModuleInstaller>();

        // Register Wallet module installer for automatic wallet and wallet transaction data class creation
        services.AddSingleton<IWalletModuleInstaller, WalletModuleInstaller>();

        // Register Gift Card module installer for gift card data class creation
        services.AddSingleton<IGiftCardModuleInstaller, GiftCardModuleInstaller>();

        // Register Gift Card services for creation, validation, and redemption
        services.AddScoped<IGiftCardService, GiftCardService>();
        services.AddScoped<IGiftCardEmailService, GiftCardEmailService>();
        services.AddScoped<IGiftCardOrderFulfillmentService, GiftCardOrderFulfillmentService>();

        // Register gift card options
        services.AddOptions<GiftCardFulfillmentOptions>()
            .BindConfiguration("Baseline:Ecommerce:GiftCard:Fulfillment");
        services.AddOptions<GiftCardEmailOptions>()
            .BindConfiguration("Baseline:Ecommerce:GiftCard:Email");

        // Register Product Stock module installer for automatic product stock data class creation
        services.AddSingleton<IProductStockModuleInstaller, ProductStockModuleInstaller>();

        // Register Fulfillment Type module installer for checkout behavior configuration
        services.AddSingleton<IFulfillmentTypeModuleInstaller, FulfillmentTypeModuleInstaller>();

        // Register Subscription module installer for subscription plan and customer subscription data classes
        services.AddSingleton<ISubscriptionModuleInstaller, SubscriptionModuleInstaller>();

        // Register Wishlist module installer for wishlist data class creation
        services.AddSingleton<IWishlistModuleInstaller, WishlistModuleInstaller>();

        // Register Wishlist service for add/remove/query operations
        services.AddScoped<IWishlistService, WishlistService>();

        // Register Ecommerce module installer for reusable schemas (ProductFields)
        services.AddSingleton<BaselineEcommerceModuleInstaller>();

        // Register Contact Commerce Fields installer (adds custom columns to OM_Contact for segmentation)
        services.AddSingleton<IContactCommerceFieldsInstaller, ContactCommerceFieldsInstaller>();

        // Register Commerce-to-Contact sync service (populates custom commerce metrics on ContactInfo)
        services.AddScoped<ICommerceContactSyncService, CommerceContactSyncService>();

        // Register shipping cost calculator (uses Kentico ShippingMethodInfo)
        services.AddScoped<IShippingCostCalculator, ShippingCostCalculator>();

        // Register payment method resolver (uses Kentico PaymentMethodInfo)
        services.AddScoped<IPaymentMethodResolver, PaymentMethodResolver>();

        // Register order mapper (uses Kentico OrderInfo, OrderItemInfo, OrderAddressInfo)
        services.AddScoped<IOrderMapper, OrderMapper>();

        // Register default price formatter (sites can override with RegisterImplementation attribute)
        // Registered as Singleton because it has no scoped dependencies and is consumed by CMS.Commerce.IOrderNotificationService (singleton)
        services.AddSingleton<IPriceFormatter, PriceFormatter>();

        // Register product variants extractor (aggregates all IProductTypeVariantsExtractor implementations)
        services.AddSingleton<IProductVariantsExtractor, ProductVariantsExtractor>();

        // Register product name provider (formats product names with variant information)
        services.AddSingleton<IProductNameProvider, ProductNameProvider>();

        // Register SKU validator (validates SKU uniqueness across content items)
        services.AddScoped<IProductSkuValidator, ProductSkuValidator>();

        // Register order number generator (generates sequential order numbers per year)
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();

        // Register tag title retriever (retrieves tag titles from taxonomy)
        services.AddScoped<ITagTitleRetriever, TagTitleRetriever>();

        // Register product parameters extractor (aggregates all IProductTypeParametersExtractor implementations)
        services.AddSingleton<IProductParametersExtractor, ProductParametersExtractor>();

        // Register fulfillment type service (database-driven checkout behavior based on content type)
        // Replaces the hardcoded ProductType enum with admin-manageable configuration
        services.AddScoped<IFulfillmentTypeService, FulfillmentTypeService>();

        // Register shipping repository (caches shipping method queries)
        services.AddScoped<IShippingRepository, ShippingRepository>();

        // Register no-op payment gateway as default (sites should register actual payment provider)
        services.TryAddScoped<IPaymentGateway, NoOpPaymentGateway>();

        // Note: The following interfaces must be registered by the site:
        // - IProductRepository or IProductRepository<T>: For product content queries
        // - IFulfillmentTypeResolver (optional): For site-specific content type to fulfillment type mappings
        // - IPaymentGateway: Register via payment provider extensions (Clover, Stripe, etc.) to replace no-op

        // Add MVC controllers
        services.AddControllers()
            .AddApplicationPart(typeof(BaselineEcommerceServiceCollectionExtensions).Assembly);

        // Register AIRA plugin for e-commerce data access in Kentico AIRA chat
        services.AddAiraPlugin<EcommerceAiraPlugin>(opts =>
            opts.EnhancementPrompt = "Format replies as helpful e-commerce assistant responses. " +
                "Use product SKUs, order numbers, and pricing data to provide accurate answers.");

        // Register no-op automation event interceptor as default.
        // The Baseline.Automation module overrides this with the real implementation
        // when AddBaselineAutomation() is called from the starting site.
        services.TryAddScoped<IAutomationEventInterceptor, NullAutomationEventInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds a default no-op implementation of IOrderNotificationService.
    /// Use this when order notifications are not required.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNoOpOrderNotifications(this IServiceCollection services)
    {
        services.AddScoped<IOrderNotificationService, NoOpOrderNotificationService>();
        return services;
    }

    /// <summary>
    /// Adds email-based order notifications that integrate with Kentico Commerce's notification system.
    /// Requires order status notifications to be configured in Xperience administration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailOrderNotifications(this IServiceCollection services)
    {
        services.AddScoped<IOrderNotificationService, EmailOrderNotificationService>();
        return services;
    }

    /// <summary>
    /// Adds tax calculation support with the specified options.
    /// Replaces the default no-op tax calculation step.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for tax options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddTaxCalculation(options =>
    /// {
    ///     options.DefaultTaxRate = 0.1m; // 10% default
    ///     options.TaxRatesByCategory["Food"] = 0.05m; // 5% for food
    ///     options.TaxRatesByCategory["Digital"] = 0.2m; // 20% for digital
    ///     options.TaxRatesByRegion["CA"] = 0.0725m; // California rate
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTaxCalculation(
        this IServiceCollection services,
        Action<TaxCalculationOptions>? configure = null)
    {
        // Configure tax options
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Replace no-op with default implementation
        // Remove existing registration first
        var existingDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ITaxPriceCalculationStep<,>));
        if (existingDescriptor != null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddTransient(typeof(ITaxPriceCalculationStep<,>), typeof(DefaultTaxPriceCalculationStep<,>));
        return services;
    }

    /// <summary>
    /// Adds a custom tax calculation step implementation.
    /// Use this when integrating with external tax services like Avalara or TaxJar.
    /// </summary>
    /// <typeparam name="TStep">The custom tax calculation step type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomTaxCalculation<TStep>(this IServiceCollection services)
        where TStep : class
    {
        // Remove existing registration first
        var existingDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ITaxPriceCalculationStep<,>));
        if (existingDescriptor != null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddTransient(typeof(ITaxPriceCalculationStep<,>), typeof(TStep));
        return services;
    }

    /// <summary>
    /// Adds Avalara AvaTax external tax service for tax calculation, address validation, and transaction recording.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Avalara options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddAvalaraTaxService(options =>
    /// {
    ///     options.AccountId = "123456789";
    ///     options.LicenseKey = "your-license-key";
    ///     options.CompanyCode = "DEFAULT";
    ///     options.UseSandbox = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddAvalaraTaxService(
        this IServiceCollection services,
        Action<AvalaraOptions>? configure = null)
    {
        services.AddOptions<AvalaraOptions>()
            .BindConfiguration(AvalaraOptions.SectionName)
            .Configure(opt => configure?.Invoke(opt));

        services.AddScoped<AvalaraTaxService>();
        services.AddScoped<IExternalTaxService>(sp => sp.GetRequiredService<AvalaraTaxService>());

        return services;
    }

    /// <summary>
    /// Adds TaxJar external tax service for tax calculation and transaction recording.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for TaxJar options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddTaxJarTaxService(options =>
    /// {
    ///     options.ApiToken = "your-api-token";
    ///     options.UseSandbox = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTaxJarTaxService(
        this IServiceCollection services,
        Action<TaxJarOptions>? configure = null)
    {
        services.AddOptions<TaxJarOptions>()
            .BindConfiguration(TaxJarOptions.SectionName)
            .Configure(opt => configure?.Invoke(opt));

        services.AddScoped<TaxJarTaxService>();
        services.AddScoped<IExternalTaxService>(sp => sp.GetRequiredService<TaxJarTaxService>());

        return services;
    }

    /// <summary>
    /// Adds a no-op external tax service that always returns empty results.
    /// Use this when external tax calculation is not needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNoOpExternalTaxService(this IServiceCollection services)
    {
        services.AddScoped<IExternalTaxService, NoOpExternalTaxService>();
        return services;
    }
}

/// <summary>
/// No-op implementation of external tax service for when tax calculation is handled internally.
/// </summary>
file sealed class NoOpExternalTaxService : IExternalTaxService
{
    public string ProviderName => "None";
    public bool IsAvailable => false;

    public Task<TaxCalculationResult> CalculateTaxAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TaxCalculationResult.Failed("No external tax service configured"));

    public Task<bool> CommitTransactionAsync(
        string providerTransactionId,
        string orderNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> VoidTransactionAsync(
        string providerTransactionId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<AddressValidationResult> ValidateAddressAsync(
        Address address,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AddressValidationResult.Valid());

    public Task<decimal> GetTaxRateAsync(
        Address address,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0m);

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
