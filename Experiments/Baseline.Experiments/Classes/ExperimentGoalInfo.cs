using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Experiments.Classes.ExperimentGoalInfo), Baseline.Experiments.Classes.ExperimentGoalInfo.OBJECT_TYPE)]

namespace Baseline.Experiments.Classes;

/// <summary>
/// Represents a persisted goal/conversion metric for an experiment.
/// </summary>
public class ExperimentGoalInfo : AbstractInfo<ExperimentGoalInfo, IInfoProvider<ExperimentGoalInfo>>, IInfoWithId, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.experimentgoal";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<ExperimentGoalInfo>),
        OBJECT_TYPE,
        "Baseline.ExperimentGoal",
        nameof(ExperimentGoalID),
        null,
        nameof(ExperimentGoalGUID),
        null,
        nameof(Name),
        null, null, null)
    {
        TouchCacheDependencies = true,
        DependsOn =
        [
            new(nameof(ExperimentID), ExperimentInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    [DatabaseField]
    public virtual int ExperimentGoalID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExperimentGoalID)), 0);
        set => SetValue(nameof(ExperimentGoalID), value);
    }

    [DatabaseField]
    public virtual Guid ExperimentGoalGUID
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ExperimentGoalGUID)), Guid.Empty);
        set => SetValue(nameof(ExperimentGoalGUID), value, Guid.Empty);
    }

    [DatabaseField]
    public virtual int ExperimentID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExperimentID)), 0);
        set => SetValue(nameof(ExperimentID), value);
    }

    [DatabaseField]
    public virtual string Name
    {
        get => ValidationHelper.GetString(GetValue(nameof(Name)), "");
        set => SetValue(nameof(Name), value);
    }

    [DatabaseField]
    public virtual string CodeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(CodeName)), "");
        set => SetValue(nameof(CodeName), value);
    }

    [DatabaseField]
    public virtual string GoalType
    {
        get => ValidationHelper.GetString(GetValue(nameof(GoalType)), "PageVisit");
        set => SetValue(nameof(GoalType), value);
    }

    [DatabaseField]
    public virtual string? Target
    {
        get => ValidationHelper.GetString(GetValue(nameof(Target)), null);
        set => SetValue(nameof(Target), value);
    }

    [DatabaseField]
    public virtual bool IsPrimary
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsPrimary)), false);
        set => SetValue(nameof(IsPrimary), value);
    }

    [DatabaseField]
    public virtual double GoalValue
    {
        get => ValidationHelper.GetDouble(GetValue(nameof(GoalValue)), 0.0);
        set => SetValue(nameof(GoalValue), value);
    }

    public ExperimentGoalInfo() : base(TYPEINFO) { }
}
