using Baseline.Automation.Models;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;

namespace Baseline.Automation.Installers;

/// <summary>
/// Installer for automation engine database tables.
/// Creates data classes for AutomationProcess, AutomationProcessContactState, and AutomationStepHistory.
/// </summary>
public class AutomationModuleInstaller(
    IInfoProvider<CMS.Modules.ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService)
{
    /// <summary>
    /// Installs all automation data classes.
    /// </summary>
    public void Install()
    {
        try
        {
            var resource = GetOrCreateModule();
            InstallAutomationProcessDataClass(resource);
            InstallContactStateDataClass(resource);
            InstallStepHistoryDataClass(resource);
            InstallAutomationStepDataClass(resource);
            InstallAutomationActionDataClass(resource);
            InstallAutomationTransitionDataClass(resource);
            InstallAutomationTriggerDataClass(resource);
            InstallAutomationStepStatisticsDataClass(resource);
        }
        catch (Exception ex)
        {
            eventLogService.LogException("AutomationModuleInstaller", "Install", ex);
            throw;
        }
    }

    private CMS.Modules.ResourceInfo GetOrCreateModule()
    {
        var resource = resourceInfoProvider.Get(BaselineAutomationConstants.ModuleName)
            ?? new CMS.Modules.ResourceInfo();

        resource.ResourceName = BaselineAutomationConstants.ModuleName;
        resource.ResourceDisplayName = BaselineAutomationConstants.ModuleDisplayName;
        resource.ResourceDescription = BaselineAutomationConstants.ModuleDescription;
        resource.ResourceIsInDevelopment = false;

        if (resource.HasChanged)
        {
            resourceInfoProvider.Set(resource);
        }

        return resource;
    }

    private static void InstallAutomationProcessDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationProcessInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationProcess";
        info.ClassDisplayName = "Automation Process";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationProcessInfo.AutomationProcessID));

        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessName), FieldDataType.Text, size: 200, allowEmpty: false, caption: "Code Name");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessDisplayName), FieldDataType.Text, size: 200, allowEmpty: false, caption: "Display Name");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessDescription), FieldDataType.LongText, allowEmpty: true, caption: "Description");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessIsEnabled), FieldDataType.Boolean, allowEmpty: false, caption: "Is Enabled");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessRecurrence), FieldDataType.Text, size: 50, allowEmpty: false, caption: "Recurrence");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessTriggerJson), FieldDataType.LongText, allowEmpty: false, caption: "Trigger JSON");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessStepsJson), FieldDataType.LongText, allowEmpty: false, caption: "Steps JSON");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessCreatedWhen), FieldDataType.DateTime, allowEmpty: false, caption: "Created");
        AddField(formInfo, nameof(AutomationProcessInfo.AutomationProcessLastModified), FieldDataType.DateTime, allowEmpty: false, caption: "Last Modified");

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallContactStateDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationProcessContactStateInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationProcessContactState";
        info.ClassDisplayName = "Automation Process Contact State";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateID));

        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateProcessID), FieldDataType.Integer, allowEmpty: false, caption: "Process ID");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateProcessGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateContactID), FieldDataType.Integer, allowEmpty: false, caption: "Contact ID");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateCurrentStepGuid), FieldDataType.Guid, allowEmpty: false, caption: "Current Step GUID");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStatus), FieldDataType.Text, size: 50, allowEmpty: false, caption: "Status");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateWaitUntil), FieldDataType.DateTime, allowEmpty: true, caption: "Wait Until");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateTriggerData), FieldDataType.LongText, allowEmpty: true, caption: "Trigger Data");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStepEnteredAt), FieldDataType.DateTime, allowEmpty: false, caption: "Step Entered At");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateStartedAt), FieldDataType.DateTime, allowEmpty: false, caption: "Started At");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateCompletedAt), FieldDataType.DateTime, allowEmpty: true, caption: "Completed At");
        AddField(formInfo, nameof(AutomationProcessContactStateInfo.AutomationProcessContactStateLastModified), FieldDataType.DateTime, allowEmpty: false, caption: "Last Modified");

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallStepHistoryDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationStepHistoryInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationStepHistory";
        info.ClassDisplayName = "Automation Step History";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationStepHistoryInfo.AutomationStepHistoryID));

        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryContactStateID), FieldDataType.Integer, allowEmpty: false, caption: "Contact State ID");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryContactStateGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryStepGuid), FieldDataType.Guid, allowEmpty: false, caption: "Step GUID");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryStepName), FieldDataType.Text, size: 200, allowEmpty: false, caption: "Step Name");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryStepType), FieldDataType.Text, size: 50, allowEmpty: false, caption: "Step Type");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistorySuccess), FieldDataType.Boolean, allowEmpty: false, caption: "Success");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryErrorMessage), FieldDataType.LongText, allowEmpty: true, caption: "Error Message");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryExecutedAt), FieldDataType.DateTime, allowEmpty: false, caption: "Executed At");
        AddField(formInfo, nameof(AutomationStepHistoryInfo.AutomationStepHistoryCompletedAt), FieldDataType.DateTime, allowEmpty: true, caption: "Completed At");

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallAutomationStepDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationStepInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationStep";
        info.ClassDisplayName = "Automation Step";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationStepInfo.AutomationStepID));

        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepDisplayName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepProcessID), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepType), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepOrder), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepDefinition), FieldDataType.LongText, allowEmpty: true);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepActionID), FieldDataType.Integer, allowEmpty: true);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepPositionX), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepPositionY), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepInfo.AutomationStepLastModified), FieldDataType.DateTime, allowEmpty: false);

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallAutomationActionDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationActionInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationAction";
        info.ClassDisplayName = "Automation Action";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationActionInfo.AutomationActionID));

        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionDisplayName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionAssemblyName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionClassName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionParameters), FieldDataType.LongText, allowEmpty: true);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionDescription), FieldDataType.LongText, allowEmpty: true);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionIconClass), FieldDataType.Text, size: 100, allowEmpty: true);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionEnabled), FieldDataType.Boolean, allowEmpty: false);
        AddField(formInfo, nameof(AutomationActionInfo.AutomationActionLastModified), FieldDataType.DateTime, allowEmpty: false);

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallAutomationTransitionDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationTransitionInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationTransition";
        info.ClassDisplayName = "Automation Transition";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationTransitionInfo.AutomationTransitionID));

        AddField(formInfo, nameof(AutomationTransitionInfo.AutomationTransitionGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTransitionInfo.AutomationTransitionSourceStepID), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTransitionInfo.AutomationTransitionTargetStepID), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTransitionInfo.AutomationTransitionSourcePointGuid), FieldDataType.Guid, allowEmpty: true);
        AddField(formInfo, nameof(AutomationTransitionInfo.AutomationTransitionType), FieldDataType.Integer, allowEmpty: false);

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallAutomationTriggerDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationTriggerInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationTrigger";
        info.ClassDisplayName = "Automation Trigger";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationTriggerInfo.AutomationTriggerID));

        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerGuid), FieldDataType.Guid, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerProcessID), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerDisplayName), FieldDataType.Text, size: 200, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerType), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerObjectType), FieldDataType.Text, size: 200, allowEmpty: true);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerParameters), FieldDataType.LongText, allowEmpty: true);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerMacroCondition), FieldDataType.LongText, allowEmpty: true);
        AddField(formInfo, nameof(AutomationTriggerInfo.AutomationTriggerLastModified), FieldDataType.DateTime, allowEmpty: false);

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void InstallAutomationStepStatisticsDataClass(CMS.Modules.ResourceInfo resource)
    {
        var className = AutomationStepStatisticsInfo.TYPEINFO.ObjectClassName;
        if (DataClassInfoProvider.GetDataClassInfo(className) is not null)
        {
            return;
        }

        var info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = "Baseline_AutomationStepStatistics";
        info.ClassDisplayName = "Automation Step Statistics";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsID));

        AddField(formInfo, nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsStepID), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsContactsAtStep), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsTotalPassed), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsAvgTimeSeconds), FieldDataType.Double, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsTimeoutCount), FieldDataType.Integer, allowEmpty: false);
        AddField(formInfo, nameof(AutomationStepStatisticsInfo.AutomationStepStatisticsLastModified), FieldDataType.DateTime, allowEmpty: false);

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void AddField(
        FormInfo formInfo,
        string name,
        string dataType,
        int size = 0,
        bool allowEmpty = true,
        string? caption = null)
    {
        var field = new FormFieldInfo
        {
            Name = name,
            Visible = caption is not null,
            DataType = dataType,
            Size = size,
            Enabled = true,
            AllowEmpty = allowEmpty
        };

        if (caption is not null)
        {
            field.Caption = caption;
        }

        formInfo.AddFormItem(field);
    }
}
