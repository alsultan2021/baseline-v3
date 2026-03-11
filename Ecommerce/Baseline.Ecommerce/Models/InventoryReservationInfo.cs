using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.InventoryReservationInfo), Baseline.Ecommerce.Models.InventoryReservationInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Info object for tracking inventory reservations.
/// Supports cart-level holds with time-based expiry.
/// </summary>
public class InventoryReservationInfo : AbstractInfo<InventoryReservationInfo, IInfoProvider<InventoryReservationInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.inventoryreservation";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<InventoryReservationInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.InventoryReservation",
        idColumn: nameof(InventoryReservationID),
        timeStampColumn: nameof(InventoryReservationLastModified),
        guidColumn: nameof(InventoryReservationGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>
    /// Inventory reservation ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int InventoryReservationID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(InventoryReservationID)), 0);
        set => SetValue(nameof(InventoryReservationID), value);
    }

    /// <summary>
    /// Inventory reservation GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid InventoryReservationGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(InventoryReservationGuid)), Guid.Empty);
        set => SetValue(nameof(InventoryReservationGuid), value);
    }

    /// <summary>
    /// Cart ID that owns this reservation.
    /// </summary>
    [DatabaseField]
    public virtual Guid InventoryReservationCartId
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(InventoryReservationCartId)), Guid.Empty);
        set => SetValue(nameof(InventoryReservationCartId), value);
    }

    /// <summary>
    /// Product GUID being reserved.
    /// </summary>
    [DatabaseField]
    public virtual Guid InventoryReservationProductGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(InventoryReservationProductGuid)), Guid.Empty);
        set => SetValue(nameof(InventoryReservationProductGuid), value);
    }

    /// <summary>
    /// Product SKU for display and logging.
    /// </summary>
    [DatabaseField]
    public virtual string InventoryReservationProductSku
    {
        get => ValidationHelper.GetString(GetValue(nameof(InventoryReservationProductSku)), string.Empty);
        set => SetValue(nameof(InventoryReservationProductSku), value);
    }

    /// <summary>
    /// Quantity reserved.
    /// </summary>
    [DatabaseField]
    public virtual decimal InventoryReservationQuantity
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(InventoryReservationQuantity)), 0m);
        set => SetValue(nameof(InventoryReservationQuantity), value);
    }

    /// <summary>
    /// Reservation status (Active, Released, Committed, Expired).
    /// </summary>
    [DatabaseField]
    public virtual string InventoryReservationStatus
    {
        get => ValidationHelper.GetString(GetValue(nameof(InventoryReservationStatus)), ReservationStatus.Active.ToString());
        set => SetValue(nameof(InventoryReservationStatus), value);
    }

    /// <summary>
    /// When the reservation expires.
    /// </summary>
    [DatabaseField]
    public virtual DateTime InventoryReservationExpiresAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(InventoryReservationExpiresAt)), DateTime.MinValue);
        set => SetValue(nameof(InventoryReservationExpiresAt), value);
    }

    /// <summary>
    /// When the reservation was created.
    /// </summary>
    [DatabaseField]
    public virtual DateTime InventoryReservationCreated
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(InventoryReservationCreated)), DateTime.MinValue);
        set => SetValue(nameof(InventoryReservationCreated), value);
    }

    /// <summary>
    /// Order number if reservation was committed.
    /// </summary>
    [DatabaseField]
    public virtual string? InventoryReservationOrderNumber
    {
        get => ValidationHelper.GetString(GetValue(nameof(InventoryReservationOrderNumber)), null);
        set => SetValue(nameof(InventoryReservationOrderNumber), value, string.Empty);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime InventoryReservationLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(InventoryReservationLastModified)), DateTime.MinValue);
        set => SetValue(nameof(InventoryReservationLastModified), value);
    }

    /// <summary>
    /// Member ID that owns this reservation (for tracking).
    /// </summary>
    [DatabaseField]
    public virtual int InventoryReservationMemberId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(InventoryReservationMemberId)), 0);
        set => SetValue(nameof(InventoryReservationMemberId), value);
    }

    /// <summary>
    /// Gets the typed status.
    /// </summary>
    public ReservationStatus Status
    {
        get => Enum.TryParse<ReservationStatus>(InventoryReservationStatus, out var status)
            ? status
            : ReservationStatus.Active;
        set => InventoryReservationStatus = value.ToString();
    }

    /// <summary>
    /// Checks if the reservation is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > InventoryReservationExpiresAt && Status == ReservationStatus.Active;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public InventoryReservationInfo() : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Constructor with data row.
    /// </summary>
    public InventoryReservationInfo(System.Data.DataRow dr) : base(TYPEINFO, dr)
    {
    }
}

/// <summary>
/// Reservation status values.
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// Reservation is active and holds inventory.
    /// </summary>
    Active,

    /// <summary>
    /// Reservation was manually released.
    /// </summary>
    Released,

    /// <summary>
    /// Reservation was converted to an order.
    /// </summary>
    Committed,

    /// <summary>
    /// Reservation expired (timed out).
    /// </summary>
    Expired
}
