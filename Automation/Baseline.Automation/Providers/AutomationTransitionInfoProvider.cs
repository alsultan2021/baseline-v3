using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationTransitionInfo"/> objects.
/// Maps to CMS.AutomationEngine.Internal.WorkflowTransitionInfoProvider.
/// </summary>
public class AutomationTransitionInfoProvider : AbstractInfoProvider<AutomationTransitionInfo, AutomationTransitionInfoProvider>
{
    public AutomationTransitionInfoProvider() : base(AutomationTransitionInfo.TYPEINFO) { }

    /// <summary>Gets all transitions.</summary>
    public static ObjectQuery<AutomationTransitionInfo> GetAutomationTransitions() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets a transition by ID.</summary>
    public static AutomationTransitionInfo? GetAutomationTransitionInfo(int transitionId) =>
        ProviderObject.GetInfoById(transitionId);

    /// <summary>Gets a transition by GUID.</summary>
    public static AutomationTransitionInfo? GetAutomationTransitionInfo(Guid transitionGuid) =>
        ProviderObject.GetInfoByGuid(transitionGuid);

    /// <summary>Sets (creates or updates) a transition.</summary>
    public static void SetAutomationTransitionInfo(AutomationTransitionInfo transitionInfo) =>
        ProviderObject.SetInfo(transitionInfo);

    /// <summary>Deletes a transition.</summary>
    public static void DeleteAutomationTransitionInfo(AutomationTransitionInfo transitionInfo) =>
        ProviderObject.DeleteInfo(transitionInfo);
}
