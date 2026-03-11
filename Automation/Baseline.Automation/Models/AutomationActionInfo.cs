using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationActionInfo), Baseline.Automation.Models.AutomationActionInfo.OBJECT_TYPE)]

namespace Baseline.Automation.Models;

/// <summary>
/// Info object representing an automation action definition.
/// Actions are reusable operations that can be assigned to Action steps.
/// Maps to CMS.AutomationEngine.Internal.WorkflowActionInfo.
/// </summary>
public class AutomationActionInfo : AbstractInfo<AutomationActionInfo, IInfoProvider<AutomationActionInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.automationaction";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationActionInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationAction",
        idColumn: nameof(AutomationActionID),
        timeStampColumn: nameof(AutomationActionLastModified),
        guidColumn: nameof(AutomationActionGuid),
        codeNameColumn: nameof(AutomationActionName),
        displayNameColumn: nameof(AutomationActionDisplayName),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    [DatabaseField]
    public virtual int AutomationActionID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationActionID)), 0);
        set => SetValue(nameof(AutomationActionID), value);
    }

    [DatabaseField]
    public virtual Guid AutomationActionGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationActionGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationActionGuid), value);
    }

    [DatabaseField]
    public virtual string AutomationActionName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionName)), string.Empty);
        set => SetValue(nameof(AutomationActionName), value);
    }

    [DatabaseField]
    public virtual string AutomationActionDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionDisplayName)), string.Empty);
        set => SetValue(nameof(AutomationActionDisplayName), value);
    }

    /// <summary>Assembly-qualified name of the action implementation class.</summary>
    [DatabaseField]
    public virtual string AutomationActionAssemblyName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionAssemblyName)), string.Empty);
        set => SetValue(nameof(AutomationActionAssemblyName), value);
    }

    /// <summary>Full class name of the action implementation.</summary>
    [DatabaseField]
    public virtual string AutomationActionClassName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionClassName)), string.Empty);
        set => SetValue(nameof(AutomationActionClassName), value);
    }

    /// <summary>Default parameters for the action as XML or JSON.</summary>
    [DatabaseField]
    public virtual string AutomationActionParameters
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionParameters)), string.Empty);
        set => SetValue(nameof(AutomationActionParameters), value);
    }

    /// <summary>Description of the action's purpose.</summary>
    [DatabaseField]
    public virtual string AutomationActionDescription
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionDescription)), string.Empty);
        set => SetValue(nameof(AutomationActionDescription), value);
    }

    /// <summary>Icon CSS class for admin UI display.</summary>
    [DatabaseField]
    public virtual string AutomationActionIconClass
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationActionIconClass)), string.Empty);
        set => SetValue(nameof(AutomationActionIconClass), value);
    }

    /// <summary>Whether this action is enabled and available for use.</summary>
    [DatabaseField]
    public virtual bool AutomationActionEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(AutomationActionEnabled)), true);
        set => SetValue(nameof(AutomationActionEnabled), value);
    }

    [DatabaseField]
    public virtual DateTime AutomationActionLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationActionLastModified)), DateTime.MinValue);
        set => SetValue(nameof(AutomationActionLastModified), value);
    }

    public AutomationActionInfo() : base(TYPEINFO) { }

    public AutomationActionInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}
