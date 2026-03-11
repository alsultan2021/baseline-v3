using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationStepInfo), Baseline.Automation.Models.AutomationStepInfo.OBJECT_TYPE)]

namespace Baseline.Automation.Models;

/// <summary>
/// Info object representing a single step within an automation process.
/// Stores step configuration, position, and type data.
/// Maps to CMS.AutomationEngine.Internal.WorkflowStepInfo.
/// </summary>
public class AutomationStepInfo : AbstractInfo<AutomationStepInfo, IInfoProvider<AutomationStepInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.automationstep";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationStepInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationStep",
        idColumn: nameof(AutomationStepID),
        timeStampColumn: nameof(AutomationStepLastModified),
        guidColumn: nameof(AutomationStepGuid),
        codeNameColumn: nameof(AutomationStepName),
        displayNameColumn: nameof(AutomationStepDisplayName),
        binaryColumn: null,
        parentIDColumn: nameof(AutomationStepProcessID),
        parentObjectType: AutomationProcessInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    [DatabaseField]
    public virtual int AutomationStepID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepID)), 0);
        set => SetValue(nameof(AutomationStepID), value);
    }

    [DatabaseField]
    public virtual Guid AutomationStepGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationStepGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationStepGuid), value);
    }

    [DatabaseField]
    public virtual string AutomationStepName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationStepName)), string.Empty);
        set => SetValue(nameof(AutomationStepName), value);
    }

    [DatabaseField]
    public virtual string AutomationStepDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationStepDisplayName)), string.Empty);
        set => SetValue(nameof(AutomationStepDisplayName), value);
    }

    /// <summary>FK to the process this step belongs to.</summary>
    [DatabaseField]
    public virtual int AutomationStepProcessID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepProcessID)), 0);
        set => SetValue(nameof(AutomationStepProcessID), value);
    }

    /// <summary>Step type (Start, Finished, Action, Condition, etc.).</summary>
    [DatabaseField]
    public virtual int AutomationStepType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepType)), 0);
        set => SetValue(nameof(AutomationStepType), value);
    }

    /// <summary>Order of the step within the process.</summary>
    [DatabaseField]
    public virtual int AutomationStepOrder
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepOrder)), 0);
        set => SetValue(nameof(AutomationStepOrder), value);
    }

    /// <summary>Step definition XML (source points, action config, timeout).</summary>
    [DatabaseField]
    public virtual string AutomationStepDefinition
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationStepDefinition)), string.Empty);
        set => SetValue(nameof(AutomationStepDefinition), value);
    }

    /// <summary>FK to the action associated with this step (if Action type).</summary>
    [DatabaseField]
    public virtual int AutomationStepActionID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepActionID)), 0);
        set => SetValue(nameof(AutomationStepActionID), value);
    }

    /// <summary>X position in the graph designer.</summary>
    [DatabaseField]
    public virtual int AutomationStepPositionX
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepPositionX)), 0);
        set => SetValue(nameof(AutomationStepPositionX), value);
    }

    /// <summary>Y position in the graph designer.</summary>
    [DatabaseField]
    public virtual int AutomationStepPositionY
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepPositionY)), 0);
        set => SetValue(nameof(AutomationStepPositionY), value);
    }

    [DatabaseField]
    public virtual DateTime AutomationStepLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationStepLastModified)), DateTime.MinValue);
        set => SetValue(nameof(AutomationStepLastModified), value);
    }

    public AutomationStepInfo() : base(TYPEINFO) { }

    public AutomationStepInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}
