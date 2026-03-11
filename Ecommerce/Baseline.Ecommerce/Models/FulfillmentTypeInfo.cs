using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Baseline.Ecommerce.Models;

[assembly: RegisterObjectType(typeof(FulfillmentTypeInfo), FulfillmentTypeInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Info class for fulfillment types that define checkout behavior.
/// Admin-managed in Kentico to allow project-specific configuration.
/// Examples: Physical (shipping), Ticket (digital), Food (pickup/delivery), etc.
/// </summary>
[InfoCache(InfoCacheBy.ID | InfoCacheBy.Name | InfoCacheBy.Guid)]
public partial class FulfillmentTypeInfo : AbstractInfo<FulfillmentTypeInfo, IInfoProvider<FulfillmentTypeInfo>>
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.fulfillmenttype";

    /// <summary>
    /// Type info for FulfillmentTypeInfo.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<FulfillmentTypeInfo>),
        OBJECT_TYPE,
        "Baseline.FulfillmentType",
        nameof(FulfillmentTypeID),
        nameof(FulfillmentTypeLastModified),
        nameof(FulfillmentTypeGUID),
        nameof(FulfillmentTypeCodeName),
        nameof(FulfillmentTypeDisplayName),
        null,
        null,
        null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        ContinuousIntegrationSettings =
        {
            Enabled = true
        }
    };

    /// <summary>
    /// Fulfillment type ID.
    /// </summary>
    [DatabaseField]
    public virtual int FulfillmentTypeID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(FulfillmentTypeID)), 0);
        set => SetValue(nameof(FulfillmentTypeID), value);
    }

    /// <summary>
    /// Fulfillment type GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid FulfillmentTypeGUID
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(FulfillmentTypeGUID)), Guid.Empty);
        set => SetValue(nameof(FulfillmentTypeGUID), value);
    }

    /// <summary>
    /// Code name for the fulfillment type (e.g., "Physical", "Ticket", "Food").
    /// </summary>
    [DatabaseField]
    public virtual string FulfillmentTypeCodeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(FulfillmentTypeCodeName)), string.Empty);
        set => SetValue(nameof(FulfillmentTypeCodeName), value);
    }

    /// <summary>
    /// Display name for the fulfillment type.
    /// </summary>
    [DatabaseField]
    public virtual string FulfillmentTypeDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(FulfillmentTypeDisplayName)), string.Empty);
        set => SetValue(nameof(FulfillmentTypeDisplayName), value);
    }

    /// <summary>
    /// Description of the fulfillment type behavior.
    /// </summary>
    [DatabaseField]
    public virtual string? FulfillmentTypeDescription
    {
        get => ValidationHelper.GetString(GetValue(nameof(FulfillmentTypeDescription)), null);
        set => SetValue(nameof(FulfillmentTypeDescription), value);
    }

    /// <summary>
    /// Whether this fulfillment type requires shipping address.
    /// </summary>
    [DatabaseField]
    public virtual bool FulfillmentTypeRequiresShipping
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(FulfillmentTypeRequiresShipping)), false);
        set => SetValue(nameof(FulfillmentTypeRequiresShipping), value);
    }

    /// <summary>
    /// Whether this fulfillment type requires billing address.
    /// </summary>
    [DatabaseField]
    public virtual bool FulfillmentTypeRequiresBillingAddress
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(FulfillmentTypeRequiresBillingAddress)), true);
        set => SetValue(nameof(FulfillmentTypeRequiresBillingAddress), value);
    }

    /// <summary>
    /// Whether this fulfillment type supports delivery/pickup options.
    /// </summary>
    [DatabaseField]
    public virtual bool FulfillmentTypeSupportsDeliveryOptions
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(FulfillmentTypeSupportsDeliveryOptions)), false);
        set => SetValue(nameof(FulfillmentTypeSupportsDeliveryOptions), value);
    }

    /// <summary>
    /// Whether this fulfillment type is enabled.
    /// </summary>
    [DatabaseField]
    public virtual bool FulfillmentTypeIsEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(FulfillmentTypeIsEnabled)), true);
        set => SetValue(nameof(FulfillmentTypeIsEnabled), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime FulfillmentTypeLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(FulfillmentTypeLastModified)), DateTime.MinValue);
        set => SetValue(nameof(FulfillmentTypeLastModified), value);
    }

    /// <summary>
    /// Deletes the object using the appropriate provider.
    /// </summary>
    protected override void DeleteObject() => Provider.Delete(this);

    /// <summary>
    /// Updates the object using the appropriate provider.
    /// </summary>
    protected override void SetObject() => Provider.Set(this);

    /// <summary>
    /// Default constructor.
    /// </summary>
    public FulfillmentTypeInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Constructor for data row.
    /// </summary>
    /// <param name="dr">DataRow with the object data.</param>
    public FulfillmentTypeInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}
