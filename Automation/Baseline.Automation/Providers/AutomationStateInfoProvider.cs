using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationProcessContactStateInfo"/> objects.
/// Maps to CMS.Automation.Internal.AutomationStateInfoProvider.
/// </summary>
public class AutomationStateInfoProvider : AbstractInfoProvider<AutomationProcessContactStateInfo, AutomationStateInfoProvider>
{
    public AutomationStateInfoProvider() : base(AutomationProcessContactStateInfo.TYPEINFO) { }

    /// <summary>Gets all contact states.</summary>
    public static ObjectQuery<AutomationProcessContactStateInfo> GetAutomationStates() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets a state by ID.</summary>
    public static AutomationProcessContactStateInfo? GetAutomationStateInfo(int stateId) =>
        ProviderObject.GetInfoById(stateId);

    /// <summary>Gets a state by GUID.</summary>
    public static AutomationProcessContactStateInfo? GetAutomationStateInfo(Guid stateGuid) =>
        ProviderObject.GetInfoByGuid(stateGuid);

    /// <summary>Sets (creates or updates) a state.</summary>
    public static void SetAutomationStateInfo(AutomationProcessContactStateInfo stateInfo) =>
        ProviderObject.SetInfo(stateInfo);

    /// <summary>Deletes a state.</summary>
    public static void DeleteAutomationStateInfo(AutomationProcessContactStateInfo stateInfo) =>
        ProviderObject.DeleteInfo(stateInfo);
}
