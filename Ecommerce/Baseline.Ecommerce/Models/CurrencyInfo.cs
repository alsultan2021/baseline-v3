using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.CurrencyInfo), Baseline.Ecommerce.Models.CurrencyInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Currency info object for storing currency definitions.
/// Currencies define different monetary units used in ecommerce transactions.
/// </summary>
public class CurrencyInfo : AbstractInfo<CurrencyInfo, IInfoProvider<CurrencyInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.currency";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<CurrencyInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.Currency",
        idColumn: nameof(CurrencyID),
        timeStampColumn: nameof(CurrencyLastModified),
        guidColumn: nameof(CurrencyGuid),
        codeNameColumn: nameof(CurrencyCode),
        displayNameColumn: nameof(CurrencyDisplayName),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>
    /// Currency ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int CurrencyID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CurrencyID)), 0);
        set => SetValue(nameof(CurrencyID), value);
    }

    /// <summary>
    /// Currency GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid CurrencyGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(CurrencyGuid)), Guid.Empty);
        set => SetValue(nameof(CurrencyGuid), value);
    }

    /// <summary>
    /// ISO 4217 currency code (e.g., USD, EUR, CAD).
    /// </summary>
    [DatabaseField]
    public virtual string CurrencyCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(CurrencyCode)), string.Empty);
        set => SetValue(nameof(CurrencyCode), value);
    }

    /// <summary>
    /// Currency display name (e.g., "US Dollar", "Euro").
    /// </summary>
    [DatabaseField]
    public virtual string CurrencyDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(CurrencyDisplayName)), string.Empty);
        set => SetValue(nameof(CurrencyDisplayName), value);
    }

    /// <summary>
    /// Currency symbol (e.g., $, €, £).
    /// </summary>
    [DatabaseField]
    public virtual string CurrencySymbol
    {
        get => ValidationHelper.GetString(GetValue(nameof(CurrencySymbol)), string.Empty);
        set => SetValue(nameof(CurrencySymbol), value);
    }

    /// <summary>
    /// Number of decimal places for this currency (typically 2, but 0 for JPY, etc.).
    /// </summary>
    [DatabaseField]
    public virtual int CurrencyDecimalPlaces
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CurrencyDecimalPlaces)), 2);
        set => SetValue(nameof(CurrencyDecimalPlaces), value);
    }

    /// <summary>
    /// Currency format pattern for display (e.g., "{0}{1}" for "$100.00" or "{1} {0}" for "100.00 €").
    /// {0} = symbol, {1} = amount.
    /// </summary>
    [DatabaseField]
    public virtual string CurrencyFormatPattern
    {
        get => ValidationHelper.GetString(GetValue(nameof(CurrencyFormatPattern)), "{0}{1}");
        set => SetValue(nameof(CurrencyFormatPattern), value);
    }

    /// <summary>
    /// Indicates if this is the default/base currency for the system.
    /// </summary>
    [DatabaseField]
    public virtual bool CurrencyIsDefault
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(CurrencyIsDefault)), false);
        set => SetValue(nameof(CurrencyIsDefault), value);
    }

    /// <summary>
    /// Indicates if the currency is enabled for use.
    /// </summary>
    [DatabaseField]
    public virtual bool CurrencyEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(CurrencyEnabled)), true);
        set => SetValue(nameof(CurrencyEnabled), value);
    }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    [DatabaseField]
    public virtual int CurrencyOrder
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CurrencyOrder)), 0);
        set => SetValue(nameof(CurrencyOrder), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime CurrencyLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(CurrencyLastModified)), DateTime.MinValue);
        set => SetValue(nameof(CurrencyLastModified), value);
    }

    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject() => Provider.Delete(this);

    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject() => Provider.Set(this);

    /// <summary>
    /// Creates a new instance of <see cref="CurrencyInfo"/>.
    /// </summary>
    public CurrencyInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from the given DataRow.
    /// </summary>
    public CurrencyInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Formats the given amount using this currency's format pattern and symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <returns>Formatted currency string.</returns>
    public string FormatAmount(decimal amount)
    {
        var formattedAmount = amount.ToString($"N{CurrencyDecimalPlaces}");
        return string.Format(CurrencyFormatPattern, CurrencySymbol, formattedAmount);
    }
}
