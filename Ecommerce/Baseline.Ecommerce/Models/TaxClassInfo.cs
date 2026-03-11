using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.TaxClassInfo), Baseline.Ecommerce.Models.TaxClassInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Tax class info object for storing tax classifications.
/// Tax classes define different categories of products for tax purposes (e.g., Standard, Reduced, Zero-rated).
/// </summary>
public class TaxClassInfo : AbstractInfo<TaxClassInfo, IInfoProvider<TaxClassInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.taxclass";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<TaxClassInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.TaxClass",
        idColumn: nameof(TaxClassID),
        timeStampColumn: nameof(TaxClassLastModified),
        guidColumn: nameof(TaxClassGuid),
        codeNameColumn: nameof(TaxClassName),
        displayNameColumn: nameof(TaxClassDisplayName),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>
    /// Tax class ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int TaxClassID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TaxClassID)), 0);
        set => SetValue(nameof(TaxClassID), value);
    }

    /// <summary>
    /// Tax class GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid TaxClassGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(TaxClassGuid)), Guid.Empty);
        set => SetValue(nameof(TaxClassGuid), value);
    }

    /// <summary>
    /// Tax class code name (unique identifier).
    /// </summary>
    [DatabaseField]
    public virtual string TaxClassName
    {
        get => ValidationHelper.GetString(GetValue(nameof(TaxClassName)), string.Empty);
        set => SetValue(nameof(TaxClassName), value);
    }

    /// <summary>
    /// Tax class display name.
    /// </summary>
    [DatabaseField]
    public virtual string TaxClassDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(TaxClassDisplayName)), string.Empty);
        set => SetValue(nameof(TaxClassDisplayName), value);
    }

    /// <summary>
    /// Tax class description.
    /// </summary>
    [DatabaseField]
    public virtual string TaxClassDescription
    {
        get => ValidationHelper.GetString(GetValue(nameof(TaxClassDescription)), string.Empty);
        set => SetValue(nameof(TaxClassDescription), value);
    }

    /// <summary>
    /// Default tax rate for this class (percentage).
    /// </summary>
    [DatabaseField]
    public virtual decimal TaxClassDefaultRate
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(TaxClassDefaultRate)), 0m);
        set => SetValue(nameof(TaxClassDefaultRate), value);
    }

    /// <summary>
    /// Whether this is the default tax class.
    /// </summary>
    [DatabaseField]
    public virtual bool TaxClassIsDefault
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(TaxClassIsDefault)), false);
        set => SetValue(nameof(TaxClassIsDefault), value);
    }

    /// <summary>
    /// Whether this tax class is tax-exempt.
    /// </summary>
    [DatabaseField]
    public virtual bool TaxClassIsExempt
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(TaxClassIsExempt)), false);
        set => SetValue(nameof(TaxClassIsExempt), value);
    }

    /// <summary>
    /// Display order.
    /// </summary>
    [DatabaseField]
    public virtual int TaxClassOrder
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TaxClassOrder)), 0);
        set => SetValue(nameof(TaxClassOrder), value);
    }

    /// <summary>
    /// Whether the tax class is enabled.
    /// </summary>
    [DatabaseField]
    public virtual bool TaxClassEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(TaxClassEnabled)), true);
        set => SetValue(nameof(TaxClassEnabled), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime TaxClassLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(TaxClassLastModified)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(TaxClassLastModified), value);
    }

    /// <summary>
    /// Constructor for deserialization.
    /// </summary>
    protected TaxClassInfo(SerializationInfo info, StreamingContext context)
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public TaxClassInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from the given DataRow.
    /// </summary>
    public TaxClassInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }
}

/// <summary>
/// Constants for the Baseline Ecommerce module.
/// </summary>
public static class BaselineEcommerceConstants
{
    /// <summary>
    /// Module name for Baseline Ecommerce data classes.
    /// </summary>
    public const string ModuleName = "XperienceCommunity.Baseline.Ecommerce";

    /// <summary>
    /// Display name for the module.
    /// </summary>
    public const string ModuleDisplayName = "Baseline Ecommerce";

    /// <summary>
    /// Description for the module.
    /// </summary>
    public const string ModuleDescription = "Ecommerce extensions for XperienceCommunity Baseline including Tax Classes, Currencies, and Account Management.";
}
