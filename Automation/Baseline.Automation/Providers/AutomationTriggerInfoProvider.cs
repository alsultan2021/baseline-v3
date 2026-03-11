using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationTriggerInfo"/> objects.
/// Maps to CMS.Automation.Internal.ObjectWorkflowTriggerInfoProvider.
/// </summary>
public class AutomationTriggerInfoProvider : AbstractInfoProvider<AutomationTriggerInfo, AutomationTriggerInfoProvider>
{
    public AutomationTriggerInfoProvider() : base(AutomationTriggerInfo.TYPEINFO) { }

    /// <summary>Gets all trigger definitions.</summary>
    public static ObjectQuery<AutomationTriggerInfo> GetAutomationTriggers() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets a trigger by ID.</summary>
    public static AutomationTriggerInfo? GetAutomationTriggerInfo(int triggerId) =>
        ProviderObject.GetInfoById(triggerId);

    /// <summary>Gets a trigger by GUID.</summary>
    public static AutomationTriggerInfo? GetAutomationTriggerInfo(Guid triggerGuid) =>
        ProviderObject.GetInfoByGuid(triggerGuid);

    /// <summary>Sets (creates or updates) a trigger.</summary>
    public static void SetAutomationTriggerInfo(AutomationTriggerInfo triggerInfo) =>
        ProviderObject.SetInfo(triggerInfo);

    /// <summary>Deletes a trigger.</summary>
    public static void DeleteAutomationTriggerInfo(AutomationTriggerInfo triggerInfo) =>
        ProviderObject.DeleteInfo(triggerInfo);
}
