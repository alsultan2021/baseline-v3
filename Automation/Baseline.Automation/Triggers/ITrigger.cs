namespace Baseline.Automation.Triggers;

/// <summary>
/// Interface for trigger executors that evaluate whether a trigger condition is met.
/// Maps to CMS.Automation.Internal.ITrigger.
/// </summary>
public interface ITrigger
{
    /// <summary>
    /// Evaluates whether the trigger conditions are met for the given event data.
    /// </summary>
    /// <param name="triggerInfo">The trigger definition from the database.</param>
    /// <param name="eventData">The event data to evaluate against.</param>
    /// <returns>True if the trigger condition is met.</returns>
    Task<bool> EvaluateAsync(Models.AutomationTriggerInfo triggerInfo, TriggerEventData eventData);

    /// <summary>
    /// Gets the object type this trigger monitors (e.g., "om.contact").
    /// </summary>
    string ObjectType { get; }
}
