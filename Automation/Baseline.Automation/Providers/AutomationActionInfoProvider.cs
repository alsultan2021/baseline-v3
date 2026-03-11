using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationActionInfo"/> objects.
/// Maps to CMS.AutomationEngine.Internal.WorkflowActionInfoProvider.
/// </summary>
public class AutomationActionInfoProvider : AbstractInfoProvider<AutomationActionInfo, AutomationActionInfoProvider>
{
    public AutomationActionInfoProvider() : base(AutomationActionInfo.TYPEINFO) { }

    /// <summary>Gets all actions.</summary>
    public static ObjectQuery<AutomationActionInfo> GetAutomationActions() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets an action by ID.</summary>
    public static AutomationActionInfo? GetAutomationActionInfo(int actionId) =>
        ProviderObject.GetInfoById(actionId);

    /// <summary>Gets an action by GUID.</summary>
    public static AutomationActionInfo? GetAutomationActionInfo(Guid actionGuid) =>
        ProviderObject.GetInfoByGuid(actionGuid);

    /// <summary>Gets an action by code name.</summary>
    public static AutomationActionInfo? GetAutomationActionInfo(string actionName) =>
        ProviderObject.GetInfoByCodeName(actionName);

    /// <summary>Sets (creates or updates) an action.</summary>
    public static void SetAutomationActionInfo(AutomationActionInfo actionInfo) =>
        ProviderObject.SetInfo(actionInfo);

    /// <summary>Deletes an action.</summary>
    public static void DeleteAutomationActionInfo(AutomationActionInfo actionInfo) =>
        ProviderObject.DeleteInfo(actionInfo);
}
