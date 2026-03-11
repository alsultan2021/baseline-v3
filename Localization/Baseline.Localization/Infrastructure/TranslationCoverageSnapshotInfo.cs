using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Localization.Infrastructure.TranslationCoverageSnapshotInfo), Baseline.Localization.Infrastructure.TranslationCoverageSnapshotInfo.OBJECT_TYPE)]

namespace Baseline.Localization.Infrastructure;

/// <summary>
/// Stores computed translation coverage statistics per language.
/// Backed by a real DB table for use with admin <c>ListingPage</c>.
/// </summary>
public class TranslationCoverageSnapshotInfo : AbstractInfo<TranslationCoverageSnapshotInfo, IInfoProvider<TranslationCoverageSnapshotInfo>>, IInfoWithId
{
    public const string OBJECT_TYPE = "baseline.translationcoveragesnapshot";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<TranslationCoverageSnapshotInfo>),
        OBJECT_TYPE,
        "Baseline.TranslationCoverageSnapshot",
        nameof(TranslationCoverageSnapshotID),
        nameof(ComputedAtUtc),
        null,
        null,
        nameof(LanguageCode),
        null, null, null)
    {
        TouchCacheDependencies = false,
        LogEvents = false,
        SupportsCloning = false
    };

    public TranslationCoverageSnapshotInfo() : base(TYPEINFO) { }

    [DatabaseField]
    public virtual int TranslationCoverageSnapshotID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TranslationCoverageSnapshotID)), 0);
        set => SetValue(nameof(TranslationCoverageSnapshotID), value);
    }

    [DatabaseField]
    public virtual string LanguageCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(LanguageCode)), "");
        set => SetValue(nameof(LanguageCode), value);
    }

    [DatabaseField]
    public virtual string LanguageDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(LanguageDisplayName)), "");
        set => SetValue(nameof(LanguageDisplayName), value);
    }

    [DatabaseField]
    public virtual int TotalContentItems
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TotalContentItems)), 0);
        set => SetValue(nameof(TotalContentItems), value);
    }

    [DatabaseField]
    public virtual int TranslatedItems
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TranslatedItems)), 0);
        set => SetValue(nameof(TranslatedItems), value);
    }

    [DatabaseField]
    public virtual int CoveragePercent
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CoveragePercent)), 0);
        set => SetValue(nameof(CoveragePercent), value);
    }

    [DatabaseField]
    public virtual bool IsDefault
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsDefault)), false);
        set => SetValue(nameof(IsDefault), value);
    }

    [DatabaseField]
    public virtual DateTime ComputedAtUtc
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ComputedAtUtc)), DateTime.MinValue);
        set => SetValue(nameof(ComputedAtUtc), value);
    }
}
