using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationTriggerInfo), Baseline.Automation.Models.AutomationTriggerInfo.OBJECT_TYPE)]

namespace Baseline.Automation.Models;

/// <summary>
/// Info object representing a trigger definition attached to an automation process.
/// Maps to CMS.Automation.Internal.ObjectWorkflowTriggerInfo.
/// </summary>
public class AutomationTriggerInfo : AbstractInfo<AutomationTriggerInfo, IInfoProvider<AutomationTriggerInfo>>, IInfoWithId, IInfoWithGuid
{
    public const string OBJECT_TYPE = "baseline.automationtrigger";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationTriggerInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationTrigger",
        idColumn: nameof(AutomationTriggerID),
        timeStampColumn: nameof(AutomationTriggerLastModified),
        guidColumn: nameof(AutomationTriggerGuid),
        codeNameColumn: null,
        displayNameColumn: nameof(AutomationTriggerDisplayName),
        binaryColumn: null,
        parentIDColumn: nameof(AutomationTriggerProcessID),
        parentObjectType: AutomationProcessInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    [DatabaseField]
    public virtual int AutomationTriggerID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTriggerID)), 0);
        set => SetValue(nameof(AutomationTriggerID), value);
    }

    [DatabaseField]
    public virtual Guid AutomationTriggerGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationTriggerGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationTriggerGuid), value);
    }

    /// <summary>FK to the automation process.</summary>
    [DatabaseField]
    public virtual int AutomationTriggerProcessID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTriggerProcessID)), 0);
        set => SetValue(nameof(AutomationTriggerProcessID), value);
    }

    /// <summary>Display name of the trigger.</summary>
    [DatabaseField]
    public virtual string AutomationTriggerDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationTriggerDisplayName)), string.Empty);
        set => SetValue(nameof(AutomationTriggerDisplayName), value);
    }

    /// <summary>Trigger type (FormSubmission, MemberRegistration, etc.).</summary>
    [DatabaseField]
    public virtual int AutomationTriggerType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationTriggerType)), 0);
        set => SetValue(nameof(AutomationTriggerType), value);
    }

    /// <summary>Object type that the trigger watches (e.g., "om.contact").</summary>
    [DatabaseField]
    public virtual string AutomationTriggerObjectType
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationTriggerObjectType)), string.Empty);
        set => SetValue(nameof(AutomationTriggerObjectType), value);
    }

    /// <summary>Trigger-specific parameters as JSON.</summary>
    [DatabaseField]
    public virtual string AutomationTriggerParameters
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationTriggerParameters)), string.Empty);
        set => SetValue(nameof(AutomationTriggerParameters), value);
    }

    /// <summary>Macro condition for trigger evaluation.</summary>
    [DatabaseField]
    public virtual string AutomationTriggerMacroCondition
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationTriggerMacroCondition)), string.Empty);
        set => SetValue(nameof(AutomationTriggerMacroCondition), value);
    }

    [DatabaseField]
    public virtual DateTime AutomationTriggerLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationTriggerLastModified)), DateTime.MinValue);
        set => SetValue(nameof(AutomationTriggerLastModified), value);
    }

    public AutomationTriggerInfo() : base(TYPEINFO) { }

    public AutomationTriggerInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}
