namespace Baseline.Automation.Triggers;

/// <summary>
/// Constants for trigger data keys and object types.
/// Maps to CMS.Automation.Internal.TriggerDataConstants.
/// </summary>
public static class TriggerDataConstants
{
    // Object types
    public const string ObjectType_Form = "cms.form";
    public const string ObjectType_Member = "cms.member";
    public const string ObjectType_Activity = "om.activity";
    public const string ObjectType_Webhook = "baseline.webhook";
    public const string ObjectType_Manual = "baseline.manual";
    public const string ObjectType_Scheduled = "baseline.scheduled";
    public const string ObjectType_Unknown = "baseline.unknown";

    // Data keys
    public const string Key_FormCodeName = "FormCodeName";
    public const string Key_MemberEmail = "MemberEmail";
    public const string Key_ActivityType = "ActivityType";
    public const string Key_ActivityValue = "ActivityValue";
    public const string Key_WebhookPayload = "WebhookPayload";
    public const string Key_WebhookSource = "WebhookSource";
    public const string Key_SourceProcessId = "SourceProcessId";
    public const string Key_SourceStepId = "SourceStepId";

    // Trigger parameter keys
    public const string Param_FormCodeName = "FormCodeName";
    public const string Param_ActivityType = "ActivityType";
    public const string Param_MacroCondition = "MacroCondition";
    public const string Param_ScheduleInterval = "ScheduleInterval";
}
