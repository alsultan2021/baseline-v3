using Baseline.Ecommerce.Models;
using CMS.ContentEngine;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using XperienceCommunity.ChannelSettings.Repositories;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for managing product stock/inventory.
/// Uses CommerceChannelSettings for inventory configuration.
/// </summary>
public class ProductStockService(
    IInfoProvider<ProductStockInfo> stockProvider,
    IChannelCustomSettingsRepository channelSettingsRepository,
    IMemoryCache cache,
    ILogger<ProductStockService> logger) : IProductStockService
{
    private readonly IInfoProvider<ProductStockInfo> stockProvider = stockProvider;
    private readonly IMemoryCache cache = cache;
    private readonly ILogger<ProductStockService> logger = logger;

    private const string ChannelSettingsCacheKey = "Baseline.Ecommerce.StockSettings";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets whether stock management is enabled from channel settings.
    /// </summary>
    public async Task<bool> IsStockManagementEnabledAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.EnableStockManagement ?? true;
    }

    /// <summary>
    /// Gets the low stock threshold from channel settings.
    /// </summary>
    public async Task<int> GetLowStockThresholdAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.LowStockThreshold ?? 5;
    }

    /// <summary>
    /// Gets whether backorders are allowed from channel settings.
    /// </summary>
    public async Task<bool> AreBackordersAllowedAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.AllowBackorders ?? false;
    }

    /// <summary>
    /// Gets whether out of stock products should be hidden from channel settings.
    /// </summary>
    public async Task<bool> ShouldHideOutOfStockAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.HideOutOfStock ?? false;
    }

    private async Task<CommerceChannelSettings?> GetChannelSettingsAsync()
    {
        return await cache.GetOrCreateAsync(ChannelSettingsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;
            return await channelSettingsRepository.GetSettingsModel<CommerceChannelSettings>();
        });
    }

    /// <inheritdoc/>
    public async Task<ProductStock?> GetStockAsync(Guid productGuid)
    {
        if (productGuid == Guid.Empty)
        {
            return null;
        }

        var cacheKey = $"baseline.ecommerce.stock.{productGuid}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;

            // Since ProductStockProduct is stored as JSON, we need to load all and filter
            var stocks = await stockProvider
                .Get()
                .GetEnumerableTypedResultAsync();

            var stockInfo = stocks.FirstOrDefault(s => s.GetProductGuid() == productGuid);
            return stockInfo is null ? null : MapToProductStock(stockInfo);
        });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, ProductStock>> GetStocksAsync(IEnumerable<Guid> productGuids)
    {
        var guids = productGuids.Where(g => g != Guid.Empty).ToHashSet();

        if (!guids.Any())
        {
            return new Dictionary<Guid, ProductStock>();
        }

        // Since ProductStockProduct is stored as JSON, we need to load all and filter
        var stocks = await stockProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        return stocks
            .Where(s => s.GetProductGuid() is Guid g && guids.Contains(g))
            .Select(MapToProductStock)
            .ToDictionary(s => s.ProductGuid);
    }

    /// <inheritdoc/>
    public async Task<bool> IsInStockAsync(Guid productGuid, decimal quantity = 1)
    {
        // Check if stock management is enabled at channel level
        if (!await IsStockManagementEnabledAsync())
        {
            return true; // Stock management disabled, always in stock
        }

        var stock = await GetStockAsync(productGuid);

        if (stock is null)
        {
            // No stock record means unlimited or digital product
            return true;
        }

        // Check product-level backorder setting, then channel-level
        if (stock.AllowBackorders || await AreBackordersAllowedAsync())
        {
            return true;
        }

        return stock.AvailableQuantity - stock.ReservedQuantity >= quantity;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetAvailableQuantityAsync(Guid productGuid)
    {
        // Check if stock management is enabled at channel level
        if (!await IsStockManagementEnabledAsync())
        {
            return decimal.MaxValue; // Stock management disabled
        }

        var stock = await GetStockAsync(productGuid);

        if (stock is null)
        {
            return decimal.MaxValue; // Unlimited
        }

        return Math.Max(0, stock.AvailableQuantity - stock.ReservedQuantity);
    }

    /// <inheritdoc/>
    public async Task<bool> IsLowStockAsync(Guid productGuid)
    {
        var stock = await GetStockAsync(productGuid);
        if (stock is null)
        {
            return false;
        }

        var threshold = await GetLowStockThresholdAsync();
        var available = stock.AvailableQuantity - stock.ReservedQuantity;
        return available > 0 && available <= threshold;
    }

    /// <inheritdoc/>
    public async Task<StockReservationResult> ReserveStockAsync(Guid productGuid, decimal quantity, Guid reservationId)
    {
        if (productGuid == Guid.Empty)
        {
            return new StockReservationResult(false, "Invalid product GUID.", null, null);
        }

        // Since ProductStockProduct is stored as JSON, we need to load all and filter
        var stocks = await stockProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        var stockInfo = stocks.FirstOrDefault(s => s.GetProductGuid() == productGuid);

        if (stockInfo is null)
        {
            // No stock tracking for this product
            return new StockReservationResult(true, null, reservationId, quantity);
        }

        var available = stockInfo.GetEffectiveAvailableQuantity();

        if (available < quantity && !stockInfo.ProductStockAllowBackorders)
        {
            return new StockReservationResult(false, $"Insufficient stock. Only {available} available.", null, null);
        }

        stockInfo.ProductStockReservedQuantity += quantity;
        await stockProvider.SetAsync(stockInfo);

        ClearCache(productGuid);

        logger.LogInformation("Reserved {Quantity} units of product {ProductGuid} with reservation {ReservationId}",
            quantity, productGuid, reservationId);

        return new StockReservationResult(true, null, reservationId, quantity);
    }

    /// <inheritdoc/>
    public async Task<StockReservationResult> ReleaseReservationAsync(Guid reservationId)
    {
        // In a real implementation, you would track reservations in a separate table
        // For now, this is a placeholder
        logger.LogInformation("Released reservation {ReservationId}", reservationId);
        return new StockReservationResult(true, null, reservationId, null);
    }

    /// <inheritdoc/>
    public async Task<StockReservationResult> ConfirmReservationAsync(Guid reservationId)
    {
        // In a real implementation, this would convert reserved quantity to actual deduction
        logger.LogInformation("Confirmed reservation {ReservationId}", reservationId);
        return new StockReservationResult(true, null, reservationId, null);
    }

    /// <inheritdoc/>
    public async Task<StockUpdateResult> DecreaseStockAsync(Guid productGuid, decimal quantity, string reason)
    {
        if (productGuid == Guid.Empty)
        {
            return new StockUpdateResult(false, "Invalid product GUID.", null, null);
        }

        // Since ProductStockProduct is stored as JSON, we need to load all and filter
        var stocks = await stockProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        var stockInfo = stocks.FirstOrDefault(s => s.GetProductGuid() == productGuid);

        if (stockInfo is null)
        {
            return new StockUpdateResult(false, "No stock record found for this product.", null, null);
        }

        var previousQuantity = stockInfo.ProductStockAvailableQuantity;

        if (previousQuantity < quantity && !stockInfo.ProductStockAllowBackorders)
        {
            return new StockUpdateResult(false, "Insufficient stock.", previousQuantity, previousQuantity);
        }

        stockInfo.ProductStockAvailableQuantity = Math.Max(0, previousQuantity - quantity);
        await stockProvider.SetAsync(stockInfo);

        ClearCache(productGuid);

        logger.LogInformation("Decreased stock for product {ProductGuid} by {Quantity}. Reason: {Reason}. Previous: {Previous}, New: {New}",
            productGuid, quantity, reason, previousQuantity, stockInfo.ProductStockAvailableQuantity);

        return new StockUpdateResult(true, null, previousQuantity, stockInfo.ProductStockAvailableQuantity);
    }

    /// <inheritdoc/>
    public async Task<StockUpdateResult> IncreaseStockAsync(Guid productGuid, decimal quantity, string reason)
    {
        if (productGuid == Guid.Empty)
        {
            return new StockUpdateResult(false, "Invalid product GUID.", null, null);
        }

        // Since ProductStockProduct is stored as JSON, we need to load all and filter
        var stocks = await stockProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        var stockInfo = stocks.FirstOrDefault(s => s.GetProductGuid() == productGuid);

        if (stockInfo is null)
        {
            // Create a new stock record
            stockInfo = new ProductStockInfo
            {
                ProductStockAvailableQuantity = quantity,
                ProductStockReservedQuantity = 0m,
                ProductStockAllowBackorders = false,
                ProductStockTrackingEnabled = true
            };
            stockInfo.SetProductGuid(productGuid);

            await stockProvider.SetAsync(stockInfo);

            ClearCache(productGuid);

            logger.LogInformation("Created stock record for product {ProductGuid} with quantity {Quantity}. Reason: {Reason}",
                productGuid, quantity, reason);

            return new StockUpdateResult(true, null, 0m, quantity);
        }

        var previousQuantity = stockInfo.ProductStockAvailableQuantity;
        stockInfo.ProductStockAvailableQuantity += quantity;
        await stockProvider.SetAsync(stockInfo);

        ClearCache(productGuid);

        logger.LogInformation("Increased stock for product {ProductGuid} by {Quantity}. Reason: {Reason}. Previous: {Previous}, New: {New}",
            productGuid, quantity, reason, previousQuantity, stockInfo.ProductStockAvailableQuantity);

        return new StockUpdateResult(true, null, previousQuantity, stockInfo.ProductStockAvailableQuantity);
    }

    /// <inheritdoc/>
    public async Task<StockUpdateResult> SetStockAsync(Guid productGuid, decimal quantity, string reason)
    {
        if (productGuid == Guid.Empty)
        {
            return new StockUpdateResult(false, "Invalid product GUID.", null, null);
        }

        // Since ProductStockProduct is stored as JSON, we need to load all and filter
        var stocks = await stockProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        var stockInfo = stocks.FirstOrDefault(s => s.GetProductGuid() == productGuid);

        if (stockInfo is null)
        {
            stockInfo = new ProductStockInfo
            {
                ProductStockAvailableQuantity = quantity,
                ProductStockReservedQuantity = 0m,
                ProductStockAllowBackorders = false,
                ProductStockTrackingEnabled = true
            };
            stockInfo.SetProductGuid(productGuid);

            await stockProvider.SetAsync(stockInfo);

            ClearCache(productGuid);

            logger.LogInformation("Created stock record for product {ProductGuid} with quantity {Quantity}. Reason: {Reason}",
                productGuid, quantity, reason);

            return new StockUpdateResult(true, null, 0m, quantity);
        }

        var previousQuantity = stockInfo.ProductStockAvailableQuantity;
        stockInfo.ProductStockAvailableQuantity = quantity;
        await stockProvider.SetAsync(stockInfo);

        ClearCache(productGuid);

        logger.LogInformation("Set stock for product {ProductGuid} to {Quantity}. Reason: {Reason}. Previous: {Previous}",
            productGuid, quantity, reason, previousQuantity);

        return new StockUpdateResult(true, null, previousQuantity, quantity);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProductStock>> GetLowStockProductsAsync(decimal threshold)
    {
        var stocks = await stockProvider
            .Get()
            .WhereEquals(nameof(ProductStockInfo.ProductStockTrackingEnabled), true)
            .WhereLessOrEquals(nameof(ProductStockInfo.ProductStockAvailableQuantity), threshold)
            .WhereGreaterThan(nameof(ProductStockInfo.ProductStockAvailableQuantity), 0)
            .GetEnumerableTypedResultAsync();

        return stocks.Select(MapToProductStock).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProductStock>> GetOutOfStockProductsAsync()
    {
        var stocks = await stockProvider
            .Get()
            .WhereEquals(nameof(ProductStockInfo.ProductStockTrackingEnabled), true)
            .WhereLessOrEquals(nameof(ProductStockInfo.ProductStockAvailableQuantity), 0)
            .GetEnumerableTypedResultAsync();

        return stocks.Select(MapToProductStock).ToList().AsReadOnly();
    }

    private void ClearCache(Guid productGuid) =>
        cache.Remove($"baseline.ecommerce.stock.{productGuid}");

    private static ProductStock MapToProductStock(ProductStockInfo info)
    {
        return new ProductStock(
            info.ProductStockID,
            info.GetProductGuid() ?? Guid.Empty,
            info.ProductStockAvailableQuantity,
            info.ProductStockReservedQuantity,
            info.ProductStockMinimumThreshold,
            info.ProductStockAllowBackorders,
            info.GetStockStatus(),
            info.ProductStockLastModified);
    }
}
