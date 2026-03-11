using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationStepHistoryInfo"/> objects.
/// Maps to CMS.Automation.Internal.AutomationHistoryInfoProvider.
/// </summary>
public class AutomationHistoryInfoProvider : AbstractInfoProvider<AutomationStepHistoryInfo, AutomationHistoryInfoProvider>
{
    public AutomationHistoryInfoProvider() : base(AutomationStepHistoryInfo.TYPEINFO) { }

    /// <summary>Gets all history records.</summary>
    public static ObjectQuery<AutomationStepHistoryInfo> GetAutomationHistories() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets a history record by ID.</summary>
    public static AutomationStepHistoryInfo? GetAutomationHistoryInfo(int historyId) =>
        ProviderObject.GetInfoById(historyId);

    /// <summary>Sets (creates or updates) a history record.</summary>
    public static void SetAutomationHistoryInfo(AutomationStepHistoryInfo historyInfo) =>
        ProviderObject.SetInfo(historyInfo);

    /// <summary>Deletes a history record.</summary>
    public static void DeleteAutomationHistoryInfo(AutomationStepHistoryInfo historyInfo) =>
        ProviderObject.DeleteInfo(historyInfo);
}
