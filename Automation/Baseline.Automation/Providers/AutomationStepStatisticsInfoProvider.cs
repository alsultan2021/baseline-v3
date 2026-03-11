using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationStepStatisticsInfo"/> objects.
/// </summary>
public class AutomationStepStatisticsInfoProvider : AbstractInfoProvider<AutomationStepStatisticsInfo, AutomationStepStatisticsInfoProvider>
{
    public AutomationStepStatisticsInfoProvider() : base(AutomationStepStatisticsInfo.TYPEINFO) { }

    /// <summary>Gets all step statistics.</summary>
    public static ObjectQuery<AutomationStepStatisticsInfo> GetAutomationStepStatistics() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets statistics by ID.</summary>
    public static AutomationStepStatisticsInfo? GetAutomationStepStatisticsInfo(int statisticsId) =>
        ProviderObject.GetInfoById(statisticsId);

    /// <summary>Sets (creates or updates) step statistics.</summary>
    public static void SetAutomationStepStatisticsInfo(AutomationStepStatisticsInfo statisticsInfo) =>
        ProviderObject.SetInfo(statisticsInfo);

    /// <summary>Deletes step statistics.</summary>
    public static void DeleteAutomationStepStatisticsInfo(AutomationStepStatisticsInfo statisticsInfo) =>
        ProviderObject.DeleteInfo(statisticsInfo);
}
