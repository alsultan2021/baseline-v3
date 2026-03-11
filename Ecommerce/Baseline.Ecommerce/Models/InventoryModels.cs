namespace Baseline.Ecommerce;

/// <summary>
/// Represents an inventory reservation for a cart.
/// </summary>
public class InventoryReservation
{
    /// <summary>
    /// Unique reservation ID.
    /// </summary>
    public Guid ReservationId { get; init; }

    /// <summary>
    /// Cart ID that owns this reservation.
    /// </summary>
    public Guid CartId { get; init; }

    /// <summary>
    /// Reserved items in this reservation.
    /// </summary>
    public IReadOnlyList<ReservedItem> Items { get; init; } = [];

    /// <summary>
    /// When the reservation was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the reservation expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Current status of the reservation.
    /// </summary>
    public string Status { get; init; } = "Active";

    /// <summary>
    /// Order number if committed.
    /// </summary>
    public string? OrderNumber { get; init; }

    /// <summary>
    /// Time remaining before expiry.
    /// </summary>
    public TimeSpan TimeRemaining => ExpiresAt > DateTime.UtcNow
        ? ExpiresAt - DateTime.UtcNow
        : TimeSpan.Zero;

    /// <summary>
    /// Whether the reservation is still active.
    /// </summary>
    public bool IsActive => Status == "Active" && DateTime.UtcNow < ExpiresAt;
}

/// <summary>
/// Represents a reserved item within a reservation.
/// </summary>
public class ReservedItem
{
    /// <summary>
    /// Product GUID.
    /// </summary>
    public Guid ProductGuid { get; init; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity reserved.
    /// </summary>
    public decimal Quantity { get; init; }
}

/// <summary>
/// Result of an inventory reservation operation.
/// </summary>
public class ReservationResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The reservation if successful.
    /// </summary>
    public InventoryReservation? Reservation { get; init; }

    /// <summary>
    /// Error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Items that could not be reserved due to insufficient stock.
    /// </summary>
    public IReadOnlyList<InsufficientStockItem> InsufficientStockItems { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ReservationResult Succeeded(InventoryReservation reservation) =>
        new() { Success = true, Reservation = reservation };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ReservationResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };

    /// <summary>
    /// Creates a failed result due to insufficient stock.
    /// </summary>
    public static ReservationResult InsufficientStock(IReadOnlyList<InsufficientStockItem> items) =>
        new()
        {
            Success = false,
            ErrorMessage = "Insufficient stock for one or more items",
            InsufficientStockItems = items
        };
}

/// <summary>
/// Represents an item with insufficient stock.
/// </summary>
public class InsufficientStockItem
{
    /// <summary>
    /// Product GUID.
    /// </summary>
    public Guid ProductGuid { get; init; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity requested.
    /// </summary>
    public decimal RequestedQuantity { get; init; }

    /// <summary>
    /// Quantity available.
    /// </summary>
    public decimal AvailableQuantity { get; init; }
}
