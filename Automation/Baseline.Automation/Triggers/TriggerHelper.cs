namespace Baseline.Automation.Triggers;

/// <summary>
/// Helper methods for working with automation triggers.
/// Maps to CMS.Automation.Internal.TriggerHelper.
/// </summary>
public static class TriggerHelper
{
    /// <summary>
    /// Gets the object type string for a given trigger type.
    /// </summary>
    public static string GetObjectType(AutomationTriggerType triggerType) => triggerType switch
    {
        AutomationTriggerType.FormSubmission => TriggerDataConstants.ObjectType_Form,
        AutomationTriggerType.MemberRegistration => TriggerDataConstants.ObjectType_Member,
        AutomationTriggerType.CustomActivity => TriggerDataConstants.ObjectType_Activity,
        AutomationTriggerType.Webhook => TriggerDataConstants.ObjectType_Webhook,
        AutomationTriggerType.Manual => TriggerDataConstants.ObjectType_Manual,
        AutomationTriggerType.Scheduled => TriggerDataConstants.ObjectType_Scheduled,
        _ => TriggerDataConstants.ObjectType_Unknown
    };

    /// <summary>
    /// Determines if the trigger type supports recurrence checking.
    /// </summary>
    public static bool SupportsRecurrence(AutomationTriggerType triggerType) => triggerType switch
    {
        AutomationTriggerType.Manual or AutomationTriggerType.Scheduled => true,
        _ => false
    };

    /// <summary>
    /// Creates trigger event data for a form submission.
    /// </summary>
    public static TriggerEventData CreateFormSubmissionEvent(int contactId, string formCodeName, Dictionary<string, string>? formData = null)
    {
        var data = new Dictionary<string, string>
        {
            [TriggerDataConstants.Key_FormCodeName] = formCodeName
        };

        if (formData is not null)
        {
            foreach (var kvp in formData)
            {
                data[$"FormField_{kvp.Key}"] = kvp.Value;
            }
        }

        return new TriggerEventData
        {
            ContactId = contactId,
            TriggerType = AutomationTriggerType.FormSubmission,
            Data = System.Text.Json.JsonSerializer.Serialize(data)
        };
    }

    /// <summary>
    /// Creates trigger event data for a member registration.
    /// </summary>
    public static TriggerEventData CreateMemberRegistrationEvent(int contactId, string memberEmail) => new()
    {
        ContactId = contactId,
        TriggerType = AutomationTriggerType.MemberRegistration,
        Data = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [TriggerDataConstants.Key_MemberEmail] = memberEmail
        })
    };

    /// <summary>
    /// Creates trigger event data for a custom activity.
    /// </summary>
    public static TriggerEventData CreateCustomActivityEvent(int contactId, string activityType, string? activityValue = null) => new()
    {
        ContactId = contactId,
        TriggerType = AutomationTriggerType.CustomActivity,
        Data = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [TriggerDataConstants.Key_ActivityType] = activityType,
            [TriggerDataConstants.Key_ActivityValue] = activityValue ?? ""
        })
    };
}
