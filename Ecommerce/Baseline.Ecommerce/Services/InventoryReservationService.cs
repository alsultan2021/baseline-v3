using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for managing cart-level inventory reservations.
/// Provides time-limited holds on product inventory during checkout.
/// </summary>
public class InventoryReservationService(
    ICartService cartService,
    IProductStockService productStockService,
    IInfoProvider<InventoryReservationInfo> reservationProvider,
    IMemoryCache cache,
    ILogger<InventoryReservationService> logger) : IInventoryReservationService
{
    private readonly ICartService cartService = cartService;
    private readonly IProductStockService productStockService = productStockService;
    private readonly IInfoProvider<InventoryReservationInfo> reservationProvider = reservationProvider;
    private readonly IMemoryCache cache = cache;
    private readonly ILogger<InventoryReservationService> logger = logger;

    private const string ReservationCachePrefix = "InventoryReservation_";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public async Task<ReservationResult> ReserveInventoryAsync(
        Guid cartId,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the cart
            var cart = await cartService.GetCartAsync();
            if (cart == null || cart.Id != cartId || cart.IsEmpty)
            {
                return ReservationResult.Failed("Cart not found or is empty");
            }

            // Validate stock availability for all items
            var insufficientItems = new List<InsufficientStockItem>();

            foreach (var item in cart.Items)
            {
                var productGuid = await GetProductGuidAsync(item.ProductId, cancellationToken);
                if (productGuid == Guid.Empty)
                {
                    logger.LogWarning("Product {ProductId} not found for reservation", item.ProductId);
                    continue;
                }

                var available = await productStockService.GetAvailableQuantityAsync(productGuid);
                if (available < item.Quantity)
                {
                    insufficientItems.Add(new InsufficientStockItem
                    {
                        ProductGuid = productGuid,
                        Sku = item.Sku ?? string.Empty,
                        ProductName = item.ProductName,
                        RequestedQuantity = item.Quantity,
                        AvailableQuantity = available
                    });
                }
            }

            if (insufficientItems.Count > 0)
            {
                return ReservationResult.InsufficientStock(insufficientItems);
            }

            // Release any existing reservations for this cart
            await ReleaseCartReservationsAsync(cartId, cancellationToken);

            // Create reservations for each item
            var reservationId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.Add(duration);
            var createdAt = DateTime.UtcNow;
            var reservedItems = new List<ReservedItem>();

            foreach (var item in cart.Items)
            {
                var productGuid = await GetProductGuidAsync(item.ProductId, cancellationToken);
                if (productGuid == Guid.Empty)
                {
                    continue;
                }

                // Reserve stock using the product stock service
                var stockResult = await productStockService.ReserveStockAsync(
                    productGuid,
                    item.Quantity,
                    reservationId);

                if (!stockResult.Success)
                {
                    logger.LogWarning("Failed to reserve stock for product {ProductId}: {Error}",
                        item.ProductId, stockResult.ErrorMessage);
                    // Rollback any reservations made so far
                    await productStockService.ReleaseReservationAsync(reservationId);
                    return ReservationResult.Failed(stockResult.ErrorMessage ?? "Failed to reserve stock");
                }

                // Create reservation info record
                var reservationInfo = new InventoryReservationInfo
                {
                    InventoryReservationGuid = Guid.NewGuid(),
                    InventoryReservationCartId = cartId,
                    InventoryReservationProductGuid = productGuid,
                    InventoryReservationProductSku = item.Sku ?? string.Empty,
                    InventoryReservationQuantity = item.Quantity,
                    InventoryReservationStatus = ReservationStatus.Active.ToString(),
                    InventoryReservationExpiresAt = expiresAt,
                    InventoryReservationCreated = createdAt,
                    InventoryReservationLastModified = createdAt
                };

                reservationProvider.Set(reservationInfo);

                reservedItems.Add(new ReservedItem
                {
                    ProductGuid = productGuid,
                    Sku = item.Sku ?? string.Empty,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity
                });
            }

            var reservation = new InventoryReservation
            {
                ReservationId = reservationId,
                CartId = cartId,
                Items = reservedItems,
                CreatedAt = createdAt,
                ExpiresAt = expiresAt,
                Status = "Active"
            };

            // Cache the reservation
            cache.Set(
                $"{ReservationCachePrefix}{reservationId}",
                reservation,
                new MemoryCacheEntryOptions { Size = 1, AbsoluteExpiration = expiresAt });

            logger.LogInformation(
                "Created inventory reservation {ReservationId} for cart {CartId} with {ItemCount} items, expires at {ExpiresAt}",
                reservationId, cartId, reservedItems.Count, expiresAt);

            return ReservationResult.Succeeded(reservation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create inventory reservation for cart {CartId}", cartId);
            return ReservationResult.Failed($"Reservation failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Release from product stock service
            await productStockService.ReleaseReservationAsync(reservationId);

            // Update reservation records
            var reservations = reservationProvider.Get()
                .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
                .ToList();

            foreach (var reservation in reservations)
            {
                reservation.Status = ReservationStatus.Released;
                reservation.InventoryReservationLastModified = DateTime.UtcNow;
                reservationProvider.Set(reservation);
            }

            // Remove from cache
            cache.Remove($"{ReservationCachePrefix}{reservationId}");

            logger.LogInformation("Released inventory reservation {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to release inventory reservation {ReservationId}", reservationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseCartReservationsAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reservations = reservationProvider.Get()
                .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationCartId), cartId)
                .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
                .ToList();

            foreach (var reservation in reservations)
            {
                // Release stock
                await productStockService.ReleaseReservationAsync(reservation.InventoryReservationGuid);

                // Update status
                reservation.Status = ReservationStatus.Released;
                reservation.InventoryReservationLastModified = DateTime.UtcNow;
                reservationProvider.Set(reservation);

                // Remove from cache
                cache.Remove($"{ReservationCachePrefix}{reservation.InventoryReservationGuid}");
            }

            logger.LogInformation("Released {Count} inventory reservations for cart {CartId}", reservations.Count, cartId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to release cart reservations for cart {CartId}", cartId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ReservationResult> ConvertToCommittedAsync(
        Guid reservationId,
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Confirm the stock reservation (makes deduction permanent)
            var stockResult = await productStockService.ConfirmReservationAsync(reservationId);
            if (!stockResult.Success)
            {
                return ReservationResult.Failed(stockResult.ErrorMessage ?? "Failed to confirm stock reservation");
            }

            // Update reservation records
            var reservations = reservationProvider.Get()
                .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
                .ToList();

            var reservedItems = new List<ReservedItem>();
            Guid cartId = Guid.Empty;
            DateTime createdAt = DateTime.UtcNow;

            foreach (var reservation in reservations)
            {
                cartId = reservation.InventoryReservationCartId;
                createdAt = reservation.InventoryReservationCreated;

                reservation.Status = ReservationStatus.Committed;
                reservation.InventoryReservationOrderNumber = orderNumber;
                reservation.InventoryReservationLastModified = DateTime.UtcNow;
                reservationProvider.Set(reservation);

                reservedItems.Add(new ReservedItem
                {
                    ProductGuid = reservation.InventoryReservationProductGuid,
                    Sku = reservation.InventoryReservationProductSku,
                    Quantity = reservation.InventoryReservationQuantity
                });
            }

            // Remove from cache
            cache.Remove($"{ReservationCachePrefix}{reservationId}");

            var committedReservation = new InventoryReservation
            {
                ReservationId = reservationId,
                CartId = cartId,
                Items = reservedItems,
                CreatedAt = createdAt,
                ExpiresAt = DateTime.UtcNow,
                Status = "Committed",
                OrderNumber = orderNumber
            };

            logger.LogInformation(
                "Committed inventory reservation {ReservationId} to order {OrderNumber}",
                reservationId, orderNumber);

            return ReservationResult.Succeeded(committedReservation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to commit inventory reservation {ReservationId}", reservationId);
            return ReservationResult.Failed($"Commit failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task<InventoryReservation?> GetReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        if (cache.TryGetValue($"{ReservationCachePrefix}{reservationId}", out InventoryReservation? cached))
        {
            return Task.FromResult(cached);
        }

        // Load from database
        var reservations = reservationProvider.Get()
            .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
            .ToList();

        if (reservations.Count == 0)
        {
            return Task.FromResult<InventoryReservation?>(null);
        }

        var first = reservations[0];
        var reservation = new InventoryReservation
        {
            ReservationId = reservationId,
            CartId = first.InventoryReservationCartId,
            Items = reservations.Select(r => new ReservedItem
            {
                ProductGuid = r.InventoryReservationProductGuid,
                Sku = r.InventoryReservationProductSku,
                Quantity = r.InventoryReservationQuantity
            }).ToList(),
            CreatedAt = first.InventoryReservationCreated,
            ExpiresAt = first.InventoryReservationExpiresAt,
            Status = first.InventoryReservationStatus,
            OrderNumber = first.InventoryReservationOrderNumber
        };

        return Task.FromResult<InventoryReservation?>(reservation);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<InventoryReservation>> GetCartReservationsAsync(
        Guid cartId,
        CancellationToken cancellationToken = default)
    {
        var reservations = reservationProvider.Get()
            .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationCartId), cartId)
            .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
            .ToList();

        // Group by cart (since each item has its own record)
        var grouped = reservations
            .GroupBy(r => r.InventoryReservationCartId)
            .Select(g => new InventoryReservation
            {
                ReservationId = g.First().InventoryReservationGuid,
                CartId = g.Key,
                Items = g.Select(r => new ReservedItem
                {
                    ProductGuid = r.InventoryReservationProductGuid,
                    Sku = r.InventoryReservationProductSku,
                    Quantity = r.InventoryReservationQuantity
                }).ToList(),
                CreatedAt = g.First().InventoryReservationCreated,
                ExpiresAt = g.First().InventoryReservationExpiresAt,
                Status = g.First().InventoryReservationStatus
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryReservation>>(grouped);
    }

    /// <inheritdoc/>
    public async Task<ReservationResult> ExtendReservationAsync(
        Guid reservationId,
        TimeSpan additionalDuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reservations = reservationProvider.Get()
                .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
                .ToList();

            if (reservations.Count == 0)
            {
                return ReservationResult.Failed("Reservation not found");
            }

            var newExpiresAt = reservations[0].InventoryReservationExpiresAt.Add(additionalDuration);
            var reservedItems = new List<ReservedItem>();

            foreach (var reservation in reservations)
            {
                reservation.InventoryReservationExpiresAt = newExpiresAt;
                reservation.InventoryReservationLastModified = DateTime.UtcNow;
                reservationProvider.Set(reservation);

                reservedItems.Add(new ReservedItem
                {
                    ProductGuid = reservation.InventoryReservationProductGuid,
                    Sku = reservation.InventoryReservationProductSku,
                    Quantity = reservation.InventoryReservationQuantity
                });
            }

            var updatedReservation = new InventoryReservation
            {
                ReservationId = reservationId,
                CartId = reservations[0].InventoryReservationCartId,
                Items = reservedItems,
                CreatedAt = reservations[0].InventoryReservationCreated,
                ExpiresAt = newExpiresAt,
                Status = "Active"
            };

            // Update cache
            cache.Set(
                $"{ReservationCachePrefix}{reservationId}",
                updatedReservation,
                new MemoryCacheEntryOptions { Size = 1, AbsoluteExpiration = newExpiresAt });

            logger.LogInformation(
                "Extended inventory reservation {ReservationId} to {NewExpiresAt}",
                reservationId, newExpiresAt);

            return ReservationResult.Succeeded(updatedReservation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extend inventory reservation {ReservationId}", reservationId);
            return ReservationResult.Failed($"Extension failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<int> ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredReservations = reservationProvider.Get()
                .WhereEquals(nameof(InventoryReservationInfo.InventoryReservationStatus), ReservationStatus.Active.ToString())
                .WhereLessThan(nameof(InventoryReservationInfo.InventoryReservationExpiresAt), DateTime.UtcNow)
                .ToList();

            if (expiredReservations.Count == 0)
            {
                return 0;
            }

            // Group by cart to release reservations together
            var groupedByCart = expiredReservations.GroupBy(r => r.InventoryReservationCartId);

            foreach (var cartGroup in groupedByCart)
            {
                foreach (var reservation in cartGroup)
                {
                    // Release stock
                    await productStockService.ReleaseReservationAsync(reservation.InventoryReservationGuid);

                    // Update status
                    reservation.Status = ReservationStatus.Expired;
                    reservation.InventoryReservationLastModified = DateTime.UtcNow;
                    reservationProvider.Set(reservation);

                    // Remove from cache
                    cache.Remove($"{ReservationCachePrefix}{reservation.InventoryReservationGuid}");
                }
            }

            logger.LogInformation("Processed {Count} expired inventory reservations", expiredReservations.Count);
            return expiredReservations.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process expired inventory reservations");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ReservationResult> ValidateReservationAsync(
        Guid cartId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartService.GetCartAsync();
            if (cart == null || cart.Id != cartId || cart.IsEmpty)
            {
                return ReservationResult.Failed("Cart not found or is empty");
            }

            var insufficientItems = new List<InsufficientStockItem>();

            foreach (var item in cart.Items)
            {
                var productGuid = await GetProductGuidAsync(item.ProductId, cancellationToken);
                if (productGuid == Guid.Empty)
                {
                    continue;
                }

                var available = await productStockService.GetAvailableQuantityAsync(productGuid);
                if (available < item.Quantity)
                {
                    insufficientItems.Add(new InsufficientStockItem
                    {
                        ProductGuid = productGuid,
                        Sku = item.Sku ?? string.Empty,
                        ProductName = item.ProductName,
                        RequestedQuantity = item.Quantity,
                        AvailableQuantity = available
                    });
                }
            }

            if (insufficientItems.Count > 0)
            {
                return ReservationResult.InsufficientStock(insufficientItems);
            }

            // Return a dummy successful result (no actual reservation created)
            return new ReservationResult { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate reservation for cart {CartId}", cartId);
            return ReservationResult.Failed($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the product GUID from the product ID.
    /// This is a placeholder - implement based on your product service.
    /// </summary>
    private Task<Guid> GetProductGuidAsync(int productId, CancellationToken cancellationToken)
    {
        // TODO: Implement product ID to GUID lookup using IProductService
        // This is a placeholder that should be replaced with actual implementation
        // For now, we'll use a simple hash-based conversion
        return Task.FromResult(new Guid(productId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }
}
