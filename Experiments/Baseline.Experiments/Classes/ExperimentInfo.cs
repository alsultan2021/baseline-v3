using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Experiments.Classes.ExperimentInfo), Baseline.Experiments.Classes.ExperimentInfo.OBJECT_TYPE)]

namespace Baseline.Experiments.Classes;

/// <summary>
/// Represents a persisted A/B testing experiment.
/// </summary>
public class ExperimentInfo : AbstractInfo<ExperimentInfo, IInfoProvider<ExperimentInfo>>, IInfoWithId, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.experiment";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<ExperimentInfo>),
        OBJECT_TYPE,
        "Baseline.Experiment",
        nameof(ExperimentID),
        nameof(ModifiedAtUtc),
        nameof(ExperimentGUID),
        nameof(Name),
        nameof(Name),
        null, null, null)
    {
        TouchCacheDependencies = true
    };

    [DatabaseField]
    public virtual int ExperimentID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExperimentID)), 0);
        set => SetValue(nameof(ExperimentID), value);
    }

    [DatabaseField]
    public virtual Guid ExperimentGUID
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ExperimentGUID)), Guid.Empty);
        set => SetValue(nameof(ExperimentGUID), value, Guid.Empty);
    }

    [DatabaseField]
    public virtual string Name
    {
        get => ValidationHelper.GetString(GetValue(nameof(Name)), "");
        set => SetValue(nameof(Name), value);
    }

    [DatabaseField]
    public virtual string Description
    {
        get => ValidationHelper.GetString(GetValue(nameof(Description)), "");
        set => SetValue(nameof(Description), value);
    }

    /// <summary>
    /// Page, Widget, Email, Custom
    /// </summary>
    [DatabaseField]
    public virtual string ExperimentType
    {
        get => ValidationHelper.GetString(GetValue(nameof(ExperimentType)), "Page");
        set => SetValue(nameof(ExperimentType), value);
    }

    /// <summary>
    /// Draft, Scheduled, Running, Paused, Completed, Archived
    /// </summary>
    [DatabaseField]
    public virtual string Status
    {
        get => ValidationHelper.GetString(GetValue(nameof(Status)), "Draft");
        set => SetValue(nameof(Status), value);
    }

    [DatabaseField]
    public virtual double ConfidenceLevel
    {
        get => ValidationHelper.GetDouble(GetValue(nameof(ConfidenceLevel)), 0.95);
        set => SetValue(nameof(ConfidenceLevel), value);
    }

    [DatabaseField]
    public virtual int MinimumSampleSize
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(MinimumSampleSize)), 100);
        set => SetValue(nameof(MinimumSampleSize), value);
    }

    [DatabaseField]
    public virtual DateTime? StartDateUtc
    {
        get
        {
            var val = GetValue(nameof(StartDateUtc));
            return val == null ? null : ValidationHelper.GetDateTime(val, DateTime.MinValue);
        }
        set => SetValue(nameof(StartDateUtc), value);
    }

    [DatabaseField]
    public virtual DateTime? EndDateUtc
    {
        get
        {
            var val = GetValue(nameof(EndDateUtc));
            return val == null ? null : ValidationHelper.GetDateTime(val, DateTime.MinValue);
        }
        set => SetValue(nameof(EndDateUtc), value);
    }

    [DatabaseField]
    public virtual string? TargetPath
    {
        get => ValidationHelper.GetString(GetValue(nameof(TargetPath)), null);
        set => SetValue(nameof(TargetPath), value);
    }

    [DatabaseField]
    public virtual string? WidgetIdentifier
    {
        get => ValidationHelper.GetString(GetValue(nameof(WidgetIdentifier)), null);
        set => SetValue(nameof(WidgetIdentifier), value);
    }

    /// <summary>
    /// JSON-serialized <see cref="Models.TrafficAllocation"/>.
    /// </summary>
    [DatabaseField]
    public virtual string? TrafficAllocationJson
    {
        get => ValidationHelper.GetString(GetValue(nameof(TrafficAllocationJson)), null);
        set => SetValue(nameof(TrafficAllocationJson), value);
    }

    [DatabaseField]
    public virtual DateTime CreatedAtUtc
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(CreatedAtUtc)), DateTime.UtcNow);
        set => SetValue(nameof(CreatedAtUtc), value);
    }

    [DatabaseField]
    public virtual DateTime ModifiedAtUtc
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ModifiedAtUtc)), DateTime.UtcNow);
        set => SetValue(nameof(ModifiedAtUtc), value);
    }

    [DatabaseField]
    public virtual string? CreatedBy
    {
        get => ValidationHelper.GetString(GetValue(nameof(CreatedBy)), null);
        set => SetValue(nameof(CreatedBy), value);
    }

    public ExperimentInfo() : base(TYPEINFO) { }
}
