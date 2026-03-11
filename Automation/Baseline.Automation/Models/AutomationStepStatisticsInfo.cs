using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Automation.Models.AutomationStepStatisticsInfo), Baseline.Automation.Models.AutomationStepStatisticsInfo.OBJECT_TYPE)]

namespace Baseline.Automation.Models;

/// <summary>
/// Info object for storing aggregated statistics per automation step.
/// Tracks how many contacts have passed through, are waiting at, etc.
/// Maps to CMS.Automation.Internal.AutomationStepStatisticsInfo.
/// </summary>
public class AutomationStepStatisticsInfo : AbstractInfo<AutomationStepStatisticsInfo, IInfoProvider<AutomationStepStatisticsInfo>>, IInfoWithId
{
    public const string OBJECT_TYPE = "baseline.automationstepstatistics";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<AutomationStepStatisticsInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.AutomationStepStatistics",
        idColumn: nameof(AutomationStepStatisticsID),
        timeStampColumn: nameof(AutomationStepStatisticsLastModified),
        guidColumn: null,
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: nameof(AutomationStepStatisticsStepID),
        parentObjectType: AutomationStepInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = false,
        SupportsCloning = false,
        LogEvents = false
    };

    [DatabaseField]
    public virtual int AutomationStepStatisticsID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepStatisticsID)), 0);
        set => SetValue(nameof(AutomationStepStatisticsID), value);
    }

    /// <summary>FK to the step.</summary>
    [DatabaseField]
    public virtual int AutomationStepStatisticsStepID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepStatisticsStepID)), 0);
        set => SetValue(nameof(AutomationStepStatisticsStepID), value);
    }

    /// <summary>Number of contacts currently at this step.</summary>
    [DatabaseField]
    public virtual int AutomationStepStatisticsContactsAtStep
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepStatisticsContactsAtStep)), 0);
        set => SetValue(nameof(AutomationStepStatisticsContactsAtStep), value);
    }

    /// <summary>Total contacts that have passed through this step.</summary>
    [DatabaseField]
    public virtual int AutomationStepStatisticsTotalPassed
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepStatisticsTotalPassed)), 0);
        set => SetValue(nameof(AutomationStepStatisticsTotalPassed), value);
    }

    /// <summary>Average time spent at this step in seconds.</summary>
    [DatabaseField]
    public virtual double AutomationStepStatisticsAvgTimeSeconds
    {
        get => ValidationHelper.GetDouble(GetValue(nameof(AutomationStepStatisticsAvgTimeSeconds)), 0);
        set => SetValue(nameof(AutomationStepStatisticsAvgTimeSeconds), value);
    }

    /// <summary>Number of contacts that timed out at this step.</summary>
    [DatabaseField]
    public virtual int AutomationStepStatisticsTimeoutCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(AutomationStepStatisticsTimeoutCount)), 0);
        set => SetValue(nameof(AutomationStepStatisticsTimeoutCount), value);
    }

    [DatabaseField]
    public virtual DateTime AutomationStepStatisticsLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AutomationStepStatisticsLastModified)), DateTime.MinValue);
        set => SetValue(nameof(AutomationStepStatisticsLastModified), value);
    }

    public AutomationStepStatisticsInfo() : base(TYPEINFO) { }

    public AutomationStepStatisticsInfo(System.Data.DataRow dr) : base(TYPEINFO, dr) { }
}
