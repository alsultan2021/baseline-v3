using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationProcessInfo), Baseline.Automation.Models.AutomationProcessInfo.OBJECT_TYPE)]
[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationProcessContactStateInfo), Baseline.Automation.Models.AutomationProcessContactStateInfo.OBJECT_TYPE)]
[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationStepHistoryInfo), Baseline.Automation.Models.AutomationStepHistoryInfo.OBJECT_TYPE)]

namespace Baseline.Automation.Models;

/// <summary>
/// Info object for persisting automation process definitions to the database.
/// </summary>
public class AutomationProcessInfo : AbstractInfo<AutomationProcessInfo, IInfoProvider<AutomationProcessInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    /// <summary>Object type name.</summary>
    public const string OBJECT_TYPE = "baseline.automationprocess";

    /// <summary>Type info.</summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationProcessInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationProcess",
        idColumn: nameof(AutomationProcessID),
        timeStampColumn: nameof(AutomationProcessLastModified),
        guidColumn: nameof(AutomationProcessGuid),
        codeNameColumn: nameof(AutomationProcessName),
        displayNameColumn: nameof(AutomationProcessDisplayName),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>Primary key.</summary>
    [DatabaseField]
    public virtual int AutomationProcessID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationProcessID)), 0);
        set => SetValue(nameof(AutomationProcessID), value);
    }

    /// <summary>GUID identifier.</summary>
    [DatabaseField]
    public virtual Guid AutomationProcessGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationProcessGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationProcessGuid), value);
    }

    /// <summary>Code name (unique).</summary>
    [DatabaseField]
    public virtual string AutomationProcessName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessName)), string.Empty);
        set => SetValue(nameof(AutomationProcessName), value);
    }

    /// <summary>Display name.</summary>
    [DatabaseField]
    public virtual string AutomationProcessDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessDisplayName)), string.Empty);
        set => SetValue(nameof(AutomationProcessDisplayName), value);
    }

    /// <summary>Optional description.</summary>
    [DatabaseField]
    public virtual string AutomationProcessDescription
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessDescription)), string.Empty);
        set => SetValue(nameof(AutomationProcessDescription), value);
    }

    /// <summary>Whether the process is enabled.</summary>
    [DatabaseField]
    public virtual bool AutomationProcessIsEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(AutomationProcessIsEnabled)), false);
        set => SetValue(nameof(AutomationProcessIsEnabled), value);
    }

    /// <summary>Recurrence mode.</summary>
    [DatabaseField]
    public virtual string AutomationProcessRecurrence
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessRecurrence)), "OneTime");
        set => SetValue(nameof(AutomationProcessRecurrence), value);
    }

    /// <summary>Trigger definition as JSON.</summary>
    [DatabaseField]
    public virtual string AutomationProcessTriggerJson
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessTriggerJson)), string.Empty);
        set => SetValue(nameof(AutomationProcessTriggerJson), value);
    }

    /// <summary>Steps definition as JSON array.</summary>
    [DatabaseField]
    public virtual string AutomationProcessStepsJson
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessStepsJson)), string.Empty);
        set => SetValue(nameof(AutomationProcessStepsJson), value);
    }

    /// <summary>Created timestamp.</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessCreatedWhen
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessCreatedWhen)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessCreatedWhen), value);
    }

    /// <summary>Last modified timestamp.</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessLastModified)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessLastModified), value);
    }

    /// <summary>Creates an empty instance.</summary>
    public AutomationProcessInfo() : base(TYPEINFO) { }

    /// <summary>Creates an instance from a DataRow.</summary>
    public AutomationProcessInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}

/// <summary>
/// Info object for persisting contact state within an automation process.
/// </summary>
public class AutomationProcessContactStateInfo : AbstractInfo<AutomationProcessContactStateInfo, IInfoProvider<AutomationProcessContactStateInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>Object type name.</summary>
    public const string OBJECT_TYPE = "baseline.automationprocesscontactstate";

    /// <summary>Type info.</summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationProcessContactStateInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationProcessContactState",
        idColumn: nameof(AutomationProcessContactStateID),
        timeStampColumn: nameof(AutomationProcessContactStateLastModified),
        guidColumn: nameof(AutomationProcessContactStateGuid),
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
            new ObjectDependency(nameof(AutomationProcessContactStateProcessID), AutomationProcessInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    /// <summary>Primary key.</summary>
    [DatabaseField]
    public virtual int AutomationProcessContactStateID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationProcessContactStateID)), 0);
        set => SetValue(nameof(AutomationProcessContactStateID), value);
    }

    /// <summary>GUID identifier.</summary>
    [DatabaseField]
    public virtual Guid AutomationProcessContactStateGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationProcessContactStateGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationProcessContactStateGuid), value);
    }

    /// <summary>FK to automation process.</summary>
    [DatabaseField]
    public virtual int AutomationProcessContactStateProcessID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationProcessContactStateProcessID)), 0);
        set => SetValue(nameof(AutomationProcessContactStateProcessID), value);
    }

    /// <summary>Process GUID for easier lookup.</summary>
    [DatabaseField]
    public virtual Guid AutomationProcessContactStateProcessGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationProcessContactStateProcessGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationProcessContactStateProcessGuid), value);
    }

    /// <summary>Contact ID.</summary>
    [DatabaseField]
    public virtual int AutomationProcessContactStateContactID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationProcessContactStateContactID)), 0);
        set => SetValue(nameof(AutomationProcessContactStateContactID), value);
    }

    /// <summary>Current step GUID in the process.</summary>
    [DatabaseField]
    public virtual Guid AutomationProcessContactStateCurrentStepGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationProcessContactStateCurrentStepGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationProcessContactStateCurrentStepGuid), value);
    }

    /// <summary>Status (Active, Waiting, Completed, Failed, Removed).</summary>
    [DatabaseField]
    public virtual string AutomationProcessContactStateStatus
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessContactStateStatus)), "Active");
        set => SetValue(nameof(AutomationProcessContactStateStatus), value);
    }

    /// <summary>Wait until timestamp (for Wait steps).</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessContactStateWaitUntil
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessContactStateWaitUntil)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessContactStateWaitUntil), value);
    }

    /// <summary>Trigger data as JSON.</summary>
    [DatabaseField]
    public virtual string AutomationProcessContactStateTriggerData
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationProcessContactStateTriggerData)), string.Empty);
        set => SetValue(nameof(AutomationProcessContactStateTriggerData), value);
    }

    /// <summary>When the contact entered the current step.</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessContactStateStepEnteredAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessContactStateStepEnteredAt)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessContactStateStepEnteredAt), value);
    }

    /// <summary>When the contact started the process.</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessContactStateStartedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessContactStateStartedAt)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessContactStateStartedAt), value);
    }

    /// <summary>When the contact completed or was removed from the process.</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessContactStateCompletedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessContactStateCompletedAt)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessContactStateCompletedAt), value);
    }

    /// <summary>Last modified timestamp.</summary>
    [DatabaseField]
    public virtual DateTime AutomationProcessContactStateLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationProcessContactStateLastModified)), DateTime.MinValue);
        set => SetValue(nameof(AutomationProcessContactStateLastModified), value);
    }

    /// <summary>Creates an empty instance.</summary>
    public AutomationProcessContactStateInfo() : base(TYPEINFO) { }

    /// <summary>Creates an instance from a DataRow.</summary>
    public AutomationProcessContactStateInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}

/// <summary>
/// Info object for recording step execution history.
/// </summary>
public class AutomationStepHistoryInfo : AbstractInfo<AutomationStepHistoryInfo, IInfoProvider<AutomationStepHistoryInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>Object type name.</summary>
    public const string OBJECT_TYPE = "baseline.automationstephistory";

    /// <summary>Type info.</summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationStepHistoryInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationStepHistory",
        idColumn: nameof(AutomationStepHistoryID),
        timeStampColumn: null,
        guidColumn: nameof(AutomationStepHistoryGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: nameof(AutomationStepHistoryContactStateID),
        parentObjectType: AutomationProcessContactStateInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = false,
        SupportsCloning = false,
        LogEvents = false,
        DependsOn =
        [
            new ObjectDependency(nameof(AutomationStepHistoryContactStateID), AutomationProcessContactStateInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    /// <summary>Primary key.</summary>
    [DatabaseField]
    public virtual int AutomationStepHistoryID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepHistoryID)), 0);
        set => SetValue(nameof(AutomationStepHistoryID), value);
    }

    /// <summary>GUID identifier.</summary>
    [DatabaseField]
    public virtual Guid AutomationStepHistoryGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationStepHistoryGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationStepHistoryGuid), value);
    }

    /// <summary>FK to contact state.</summary>
    [DatabaseField]
    public virtual int AutomationStepHistoryContactStateID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepHistoryContactStateID)), 0);
        set => SetValue(nameof(AutomationStepHistoryContactStateID), value);
    }

    /// <summary>Contact state GUID for easier lookup.</summary>
    [DatabaseField]
    public virtual Guid AutomationStepHistoryContactStateGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationStepHistoryContactStateGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationStepHistoryContactStateGuid), value);
    }

    /// <summary>Step GUID.</summary>
    [DatabaseField]
    public virtual Guid AutomationStepHistoryStepGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(AutomationStepHistoryStepGuid)), Guid.Empty);
        set => SetValue(nameof(AutomationStepHistoryStepGuid), value);
    }

    /// <summary>Step name.</summary>
    [DatabaseField]
    public virtual string AutomationStepHistoryStepName
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationStepHistoryStepName)), string.Empty);
        set => SetValue(nameof(AutomationStepHistoryStepName), value);
    }

    /// <summary>Step type.</summary>
    [DatabaseField]
    public virtual string AutomationStepHistoryStepType
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationStepHistoryStepType)), string.Empty);
        set => SetValue(nameof(AutomationStepHistoryStepType), value);
    }

    /// <summary>Whether the step executed successfully.</summary>
    [DatabaseField]
    public virtual bool AutomationStepHistorySuccess
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(AutomationStepHistorySuccess)), false);
        set => SetValue(nameof(AutomationStepHistorySuccess), value);
    }

    /// <summary>Error message if failed.</summary>
    [DatabaseField]
    public virtual string AutomationStepHistoryErrorMessage
    {
        get => ValidationHelper.GetString(GetValue(nameof(AutomationStepHistoryErrorMessage)), string.Empty);
        set => SetValue(nameof(AutomationStepHistoryErrorMessage), value);
    }

    /// <summary>When the step was executed.</summary>
    [DatabaseField]
    public virtual DateTime AutomationStepHistoryExecutedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationStepHistoryExecutedAt)), DateTime.MinValue);
        set => SetValue(nameof(AutomationStepHistoryExecutedAt), value);
    }

    /// <summary>When the step was completed.</summary>
    [DatabaseField]
    public virtual DateTime AutomationStepHistoryCompletedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationStepHistoryCompletedAt)), DateTime.MinValue);
        set => SetValue(nameof(AutomationStepHistoryCompletedAt), value);
    }

    /// <summary>Creates an empty instance.</summary>
    public AutomationStepHistoryInfo() : base(TYPEINFO) { }

    /// <summary>Creates an instance from a DataRow.</summary>
    public AutomationStepHistoryInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}
