# Baseline.Ecommerce v3

E-commerce module for Xperience by Kentico that wraps Kentico Commerce APIs with Baseline patterns.

## Overview

This module provides:

- **Shopping Cart** - `ICartService` for managing shopping carts with add, update, remove operations
- **Checkout** - `ICheckoutService` for checkout workflows (cart → order conversion)
- **Order Management** - `IOrderService` for order retrieval and status management
- **Product Data** - `IProductDataRetriever<T>` for fetching product data with pricing
- **Customer Data** - `ICustomerDataRetriever` for customer and address management
- **Tax Calculation** - `ITaxPriceCalculationStep<,>` for configurable tax calculation

## Architecture

The module follows the standard Baseline v3 pattern:

```
Baseline.Ecommerce/        # Core services, interfaces, models
Baseline.Ecommerce.Admin/  # Admin UI module registration
Baseline.Ecommerce.RCL/    # Razor Class Library (views, tag helpers)
```

## Dependencies

- `Kentico.Xperience.WebApp` (Kentico Commerce APIs)
- `XperienceCommunity.ChannelSettings` (configuration)
- `Baseline.Core` (base patterns, caching, logging)
- `Baseline.Account` (customer/member integration)

## Installation

### 1. Add Project References

```xml
<ProjectReference Include="path/to/Baseline.Ecommerce/Baseline.Ecommerce.csproj" />
<ProjectReference Include="path/to/Baseline.Ecommerce.Admin/Baseline.Ecommerce.Admin.csproj" />
<ProjectReference Include="path/to/Baseline.Ecommerce.RCL/Baseline.Ecommerce.RCL.csproj" />
```

### 2. Register Services

In `Program.cs`:

```csharp
builder.Services.AddBaselineEcommerce();

// Optional: Configure tax calculation
builder.Services.AddTaxCalculation(options =>
{
    options.DefaultTaxRate = 0.10m; // 10%
    options.PricesIncludeTax = false;
    options.CategoryTaxRates["Food"] = 0.0m;
    options.CategoryTaxRates["Digital"] = 0.05m;
});
```

### 3. Implement Site-Specific Services

You must implement `IProductDataRetriever<T>` where `T` is your product fields interface:

```csharp
public interface IProductFields
{
    ContentItemAsset? ProductFieldImage { get; }
    IEnumerable<TagReference>? ProductFieldCategory { get; }
    decimal ProductFieldPrice { get; }
    string ProductFieldSKU { get; }
    // ... your fields
}

public class SiteProductDataRetriever : ProductDataRetrieverBase<IProductFields>
{
    protected override ProductData MapToProductData(
        ContentItemContext contentItem,
        IProductFields productFields,
        CatalogPrices? prices)
    {
        return new ProductData
        {
            ProductId = contentItem.ContentItemID.ToString(),
            SKU = productFields.ProductFieldSKU,
            Price = prices?.Price ?? productFields.ProductFieldPrice,
            // ... mapping
        };
    }
}
```

Register your implementation:

```csharp
services.AddSingleton<IProductDataRetriever<IProductFields>, SiteProductDataRetriever>();
```

## Services

### ICartService

```csharp
public interface ICartService
{
    Task<CartData> GetCurrentCartAsync(CancellationToken cancellationToken = default);
    Task<CartData> AddItemAsync(string productId, int quantity = 1, ...);
    Task<CartData> UpdateItemQuantityAsync(string itemId, int quantity, ...);
    Task<CartData> RemoveItemAsync(string itemId, ...);
    Task ClearCartAsync(CancellationToken cancellationToken = default);
}
```

### ICheckoutService

```csharp
public interface ICheckoutService
{
    Task<CheckoutData> GetCheckoutDataAsync(CancellationToken cancellationToken = default);
    Task<OrderConfirmation> SubmitOrderAsync(CheckoutSubmission submission, ...);
    Task<CheckoutData> ApplyCouponAsync(string couponCode, ...);
}
```

### IOrderService

```csharp
public interface IOrderService
{
    Task<OrderData?> GetOrderAsync(int orderId, ...);
    Task<IEnumerable<OrderData>> GetOrdersForCustomerAsync(int customerId, ...);
    Task<OrderData> UpdateOrderStatusAsync(int orderId, string status, ...);
}
```

### IProductDataRetriever<T>

Abstract base class pattern - implement `ProductDataRetrieverBase<T>` for your site.

### ICustomerDataRetriever

Abstract base class pattern - optionally implement `CustomerDataRetrieverBase` for your site.
A `NoOpCustomerDataRetriever` is provided as a default placeholder.

## Configuration

### EcommerceOptions

```csharp
services.Configure<EcommerceOptions>(options =>
{
    options.DefaultCurrency = "USD";
    options.CartSessionKey = "BaselineCart";
    options.EnableGuestCheckout = true;
    options.RequireEmailVerification = false;
});
```

### TaxCalculationOptions

```csharp
services.Configure<TaxCalculationOptions>(options =>
{
    options.DefaultTaxRate = 0.10m;
    options.PricesIncludeTax = false;
    options.CategoryTaxRates["Food"] = 0.0m;
    options.RegionTaxRates["CA"] = 0.0725m;
});
```

## Models

| Model                 | Description                      |
| --------------------- | -------------------------------- |
| `CartData`            | Shopping cart with items, totals |
| `CartItemData`        | Individual cart item             |
| `ProductData`         | Product information with pricing |
| `CheckoutData`        | Checkout state with addresses    |
| `OrderData`           | Order information                |
| `OrderConfirmation`   | Order submission result          |
| `CustomerData`        | Customer profile                 |
| `CustomerAddressData` | Customer address                 |

## View Models

| ViewModel               | Description       |
| ----------------------- | ----------------- |
| `ShoppingCartViewModel` | Cart display      |
| `CartItemViewModel`     | Cart item display |
| `CheckoutViewModel`     | Checkout page     |
| `OrderSummaryViewModel` | Order display     |
| `AddToCartViewModel`    | Add-to-cart form  |

## Extending

### Custom Tax Calculation

```csharp
public class MyTaxCalculationStep<TRequest, TResult>
    : TaxPriceCalculationStepBase<TRequest, TResult>
    where TRequest : PriceCalculationRequest
    where TResult : PriceCalculationResult
{
    protected override decimal CalculateTax(TRequest request, TResult result)
    {
        // Your tax logic
        return calculatedTax;
    }
}

// Register:
services.AddCustomTaxCalculation<MyTaxCalculationStep<,>>();
```

### Custom Product Data Retriever

Extend `ProductDataRetrieverBase<T>` or `ExtendedProductDataRetrieverBase<T>` for additional data:

```csharp
public class ExtendedSiteProductDataRetriever : ExtendedProductDataRetrieverBase<IProductFields>
{
    protected override ExtendedProductData MapToExtendedProductData(
        ContentItemContext contentItem,
        IProductFields productFields,
        CatalogPrices? prices)
    {
        var baseData = MapToProductData(contentItem, productFields, prices);
        return new ExtendedProductData
        {
            // Base data
            ProductId = baseData.ProductId,
            // Extended fields
            CustomField = productFields.MyCustomField
        };
    }
}
```

## Phase Status

- ✅ Phase 1: Core Structure (interfaces, models, view models)
- ✅ Phase 2: Service Implementations (cart, checkout, order, product retriever)
- ✅ Phase 3: Advanced Features (tax calculation, customer retriever, site implementation)
- ✅ Phase 4: Admin UI (Currency, Tax Class, Fulfillment Type, Promotion, Product Stock, Wallet management)
- 📋 Phase 5: RCL Views & Tag Helpers (pending)
- 📋 Phase 6: Testing & Documentation (pending)

## License

MIT License - See repository root for details.
