using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Triggers;

/// <summary>
/// Executes trigger evaluation and dispatches matched triggers to start processes.
/// Maps to CMS.Automation.Internal.Trigger.
/// </summary>
public class AutomationTriggerExecutor(
    IAutomationEngine automationEngine,
    IEnumerable<ITrigger> triggers,
    ILogger<AutomationTriggerExecutor> logger)
{
    private readonly Dictionary<string, ITrigger> _triggersByObjectType =
        triggers.ToDictionary(t => t.ObjectType, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Evaluates all active triggers against the given event data and starts matching processes.
    /// </summary>
    public async Task<int> ExecuteTriggersAsync(TriggerEventData eventData)
    {
        int started = 0;

        try
        {
            started = await automationEngine.FireTriggerAsync(eventData);

            logger.LogDebug("Trigger execution for {TriggerType} started {Count} processes for contact {ContactId}",
                eventData.TriggerType, started, eventData.ContactId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing triggers for contact {ContactId}", eventData.ContactId);
        }

        return started;
    }

    /// <summary>
    /// Evaluates a specific trigger definition against event data.
    /// </summary>
    public async Task<bool> EvaluateTriggerAsync(Models.AutomationTriggerInfo triggerInfo, TriggerEventData eventData)
    {
        var objectType = triggerInfo.AutomationTriggerObjectType;

        if (!_triggersByObjectType.TryGetValue(objectType, out var trigger))
        {
            logger.LogWarning("No trigger executor registered for object type {ObjectType}", objectType);
            return false;
        }

        return await trigger.EvaluateAsync(triggerInfo, eventData);
    }

    /// <summary>
    /// Gets whether a trigger executor is registered for the given object type.
    /// </summary>
    public bool HasTriggerExecutor(string objectType) =>
        _triggersByObjectType.ContainsKey(objectType);
}
