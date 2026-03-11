using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationTransitionInfo), Baseline.Automation.Models.AutomationTransitionInfo.OBJECT_TYPE)]

namespace Baseline.Automation.Models;

/// <summary>
/// Info object representing a transition (connection) between two automation steps.
/// Maps to CMS.AutomationEngine.Internal.WorkflowTransitionInfo.
/// </summary>
public class AutomationTransitionInfo : AbstractInfo<AutomationTransitionInfo, IInfoProvider<AutomationTransitionInfo>>, IInfoWithId, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.automationtransition";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationTransitionInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationTransition",
        idColumn: nameof(AutomationTransitionID),
        timeStampColumn: null,
        guidColumn: nameof(AutomationTransitionGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = false,
        DependsOn =
        [
            new ObjectDependency(nameof(AutomationTransitionSourceStepID), AutomationStepInfo.OBJECT_TYPE, ObjectDependencyEnum.Required),
            new ObjectDependency(nameof(AutomationTransitionTargetStepID), AutomationStepInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    [DatabaseField]
    public virtual int AutomationTransitionID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTransitionID)), 0);
        set => SetValue(nameof(AutomationTransitionID), value);
    }

    [DatabaseField]
    public virtual Guid AutomationTransitionGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationTransitionGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationTransitionGuid), value);
    }

    /// <summary>FK to the source step.</summary>
    [DatabaseField]
    public virtual int AutomationTransitionSourceStepID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTransitionSourceStepID)), 0);
        set => SetValue(nameof(AutomationTransitionSourceStepID), value);
    }

    /// <summary>FK to the target step.</summary>
    [DatabaseField]
    public virtual int AutomationTransitionTargetStepID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTransitionTargetStepID)), 0);
        set => SetValue(nameof(AutomationTransitionTargetStepID), value);
    }

    /// <summary>GUID of the source point on the source step.</summary>
    [DatabaseField]
    public virtual Guid AutomationTransitionSourcePointGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationTransitionSourcePointGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationTransitionSourcePointGuid), value);
    }

    /// <summary>Transition type (Manual, Automatic).</summary>
    [DatabaseField]
    public virtual int AutomationTransitionType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTransitionType)), 0);
        set => SetValue(nameof(AutomationTransitionType), value);
    }

    public AutomationTransitionInfo() : base(TYPEINFO) { }

    public AutomationTransitionInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}
