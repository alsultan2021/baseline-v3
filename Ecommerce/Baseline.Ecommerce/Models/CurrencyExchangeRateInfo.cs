using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.CurrencyExchangeRateInfo), Baseline.Ecommerce.Models.CurrencyExchangeRateInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Currency exchange rate info for storing conversion rates between currencies.
/// Exchange rates enable multi-currency support for ecommerce transactions.
/// </summary>
public class CurrencyExchangeRateInfo : AbstractInfo<CurrencyExchangeRateInfo, IInfoProvider<CurrencyExchangeRateInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.currencyexchangerate";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<CurrencyExchangeRateInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.CurrencyExchangeRate",
        idColumn: nameof(ExchangeRateID),
        timeStampColumn: nameof(ExchangeRateLastModified),
        guidColumn: nameof(ExchangeRateGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true,
        DependsOn =
        [
            new ObjectDependency(nameof(ExchangeRateFromCurrencyID), CurrencyInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
            new ObjectDependency(nameof(ExchangeRateToCurrencyID), CurrencyInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    /// <summary>
    /// Exchange rate ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int ExchangeRateID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExchangeRateID)), 0);
        set => SetValue(nameof(ExchangeRateID), value);
    }

    /// <summary>
    /// Exchange rate GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid ExchangeRateGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ExchangeRateGuid)), Guid.Empty);
        set => SetValue(nameof(ExchangeRateGuid), value);
    }

    /// <summary>
    /// Source currency ID (convert FROM this currency).
    /// </summary>
    [DatabaseField]
    public virtual int ExchangeRateFromCurrencyID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExchangeRateFromCurrencyID)), 0);
        set => SetValue(nameof(ExchangeRateFromCurrencyID), value);
    }

    /// <summary>
    /// Target currency ID (convert TO this currency).
    /// </summary>
    [DatabaseField]
    public virtual int ExchangeRateToCurrencyID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExchangeRateToCurrencyID)), 0);
        set => SetValue(nameof(ExchangeRateToCurrencyID), value);
    }

    /// <summary>
    /// The exchange rate multiplier.
    /// To convert: targetAmount = sourceAmount * ExchangeRateValue
    /// </summary>
    [DatabaseField]
    public virtual decimal ExchangeRateValue
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(ExchangeRateValue)), 1m);
        set => SetValue(nameof(ExchangeRateValue), value);
    }

    /// <summary>
    /// Date when this exchange rate becomes effective.
    /// </summary>
    [DatabaseField]
    public virtual DateTime ExchangeRateValidFrom
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ExchangeRateValidFrom)), DateTime.MinValue);
        set => SetValue(nameof(ExchangeRateValidFrom), value);
    }

    /// <summary>
    /// Date when this exchange rate expires (null = no expiration).
    /// </summary>
    [DatabaseField]
    public virtual DateTime? ExchangeRateValidTo
    {
        get
        {
            var val = GetValue(nameof(ExchangeRateValidTo));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetDateTime(val, DateTime.MaxValue);
        }
        set => SetValue(nameof(ExchangeRateValidTo), value);
    }

    /// <summary>
    /// Indicates if this exchange rate is currently active.
    /// </summary>
    [DatabaseField]
    public virtual bool ExchangeRateEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(ExchangeRateEnabled)), true);
        set => SetValue(nameof(ExchangeRateEnabled), value);
    }

    /// <summary>
    /// Source or description for this exchange rate (e.g., "ECB", "Manual", "API").
    /// </summary>
    [DatabaseField]
    public virtual string ExchangeRateSource
    {
        get => ValidationHelper.GetString(GetValue(nameof(ExchangeRateSource)), string.Empty);
        set => SetValue(nameof(ExchangeRateSource), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime ExchangeRateLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ExchangeRateLastModified)), DateTime.MinValue);
        set => SetValue(nameof(ExchangeRateLastModified), value);
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
    /// Creates a new instance of <see cref="CurrencyExchangeRateInfo"/>.
    /// </summary>
    public CurrencyExchangeRateInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from the given DataRow.
    /// </summary>
    public CurrencyExchangeRateInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Checks if this exchange rate is currently valid based on date range.
    /// </summary>
    /// <param name="asOfDate">The date to check validity for (defaults to now).</param>
    /// <returns>True if the exchange rate is valid for the given date.</returns>
    public bool IsValidFor(DateTime? asOfDate = null)
    {
        var date = asOfDate ?? DateTime.UtcNow;

        if (!ExchangeRateEnabled)
        {
            return false;
        }

        if (date < ExchangeRateValidFrom)
        {
            return false;
        }

        if (ExchangeRateValidTo.HasValue && date > ExchangeRateValidTo.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts an amount from the source currency to the target currency.
    /// </summary>
    /// <param name="amount">The amount in the source currency.</param>
    /// <returns>The converted amount in the target currency.</returns>
    public decimal Convert(decimal amount) => amount * ExchangeRateValue;
}
