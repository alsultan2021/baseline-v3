using CMS.DataEngine;
using Baseline.Automation.Models;

namespace Baseline.Automation.Providers;

/// <summary>
/// Info provider for <see cref="AutomationStepInfo"/> objects.
/// Maps to CMS.AutomationEngine.Internal.WorkflowStepInfoProvider.
/// </summary>
public class AutomationStepInfoProvider : AbstractInfoProvider<AutomationStepInfo, AutomationStepInfoProvider>
{
    public AutomationStepInfoProvider() : base(AutomationStepInfo.TYPEINFO) { }

    /// <summary>Gets all automation steps.</summary>
    public static ObjectQuery<AutomationStepInfo> GetAutomationSteps() =>
        ProviderObject.GetObjectQuery();

    /// <summary>Gets a step by ID.</summary>
    public static AutomationStepInfo? GetAutomationStepInfo(int stepId) =>
        ProviderObject.GetInfoById(stepId);

    /// <summary>Gets a step by GUID.</summary>
    public static AutomationStepInfo? GetAutomationStepInfo(Guid stepGuid) =>
        ProviderObject.GetInfoByGuid(stepGuid);

    /// <summary>Gets a step by code name.</summary>
    public static AutomationStepInfo? GetAutomationStepInfo(string stepName) =>
        ProviderObject.GetInfoByCodeName(stepName);

    /// <summary>Sets (creates or updates) a step.</summary>
    public static void SetAutomationStepInfo(AutomationStepInfo stepInfo) =>
        ProviderObject.SetInfo(stepInfo);

    /// <summary>Deletes a step.</summary>
    public static void DeleteAutomationStepInfo(AutomationStepInfo stepInfo) =>
        ProviderObject.DeleteInfo(stepInfo);
}
