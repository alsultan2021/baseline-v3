using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Experiments.Classes.ExperimentVariantInfo), Baseline.Experiments.Classes.ExperimentVariantInfo.OBJECT_TYPE)]

namespace Baseline.Experiments.Classes;

/// <summary>
/// Represents a persisted variant within an experiment.
/// </summary>
public class ExperimentVariantInfo : AbstractInfo<ExperimentVariantInfo, IInfoProvider<ExperimentVariantInfo>>, IInfoWithId, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.experimentvariant";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<ExperimentVariantInfo>),
        OBJECT_TYPE,
        "Baseline.ExperimentVariant",
        nameof(ExperimentVariantID),
        null,
        nameof(ExperimentVariantGUID),
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
    public virtual int ExperimentVariantID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ExperimentVariantID)), 0);
        set => SetValue(nameof(ExperimentVariantID), value);
    }

    [DatabaseField]
    public virtual Guid ExperimentVariantGUID
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ExperimentVariantGUID)), Guid.Empty);
        set => SetValue(nameof(ExperimentVariantGUID), value, Guid.Empty);
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
    public virtual string Description
    {
        get => ValidationHelper.GetString(GetValue(nameof(Description)), "");
        set => SetValue(nameof(Description), value);
    }

    [DatabaseField]
    public virtual bool IsControl
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsControl)), false);
        set => SetValue(nameof(IsControl), value);
    }

    [DatabaseField]
    public virtual int Weight
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(Weight)), 50);
        set => SetValue(nameof(Weight), value);
    }

    [DatabaseField]
    public virtual string? Configuration
    {
        get => ValidationHelper.GetString(GetValue(nameof(Configuration)), null);
        set => SetValue(nameof(Configuration), value);
    }

    [DatabaseField]
    public virtual string? ContentPath
    {
        get => ValidationHelper.GetString(GetValue(nameof(ContentPath)), null);
        set => SetValue(nameof(ContentPath), value);
    }

    [DatabaseField]
    public virtual string? WidgetConfiguration
    {
        get => ValidationHelper.GetString(GetValue(nameof(WidgetConfiguration)), null);
        set => SetValue(nameof(WidgetConfiguration), value);
    }

    public ExperimentVariantInfo() : base(TYPEINFO) { }
}
