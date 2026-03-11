using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationProcessInfo"/> objects.
/// Maps to internal Kentico provider pattern for automation processes.
/// </summary>
public class AutomationProcessInfoProvider : AbstractInfoProvider<AutomationProcessInfo, AutomationProcessInfoProvider>
{
    public AutomationProcessInfoProvider() : base(AutomationProcessInfo.TYPEINFO) { }

    /// <summary>Gets all automation processes.</summary>
    public static ObjectQuery<AutomationProcessInfo> GetAutomationProcesses() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets a process by ID.</summary>
    public static AutomationProcessInfo? GetAutomationProcessInfo(int processId) =>
        ProviderObject.GetInfoById(processId);

    /// <summary>Gets a process by GUID.</summary>
    public static AutomationProcessInfo? GetAutomationProcessInfo(Guid processGuid) =>
        ProviderObject.GetInfoByGuid(processGuid);

    /// <summary>Gets a process by code name.</summary>
    public static AutomationProcessInfo? GetAutomationProcessInfo(string processName) =>
        ProviderObject.GetInfoByCodeName(processName);

    /// <summary>Sets (creates or updates) a process.</summary>
    public static void SetAutomationProcessInfo(AutomationProcessInfo processInfo) =>
        ProviderObject.SetInfo(processInfo);

    /// <summary>Deletes a process.</summary>
    public static void DeleteAutomationProcessInfo(AutomationProcessInfo processInfo) =>
        ProviderObject.DeleteInfo(processInfo);
}
