using Baseline.Experiments.Classes;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Experiments.Infrastructure;

/// <summary>
/// Installer for the Baseline_ExperimentGoal table.
/// </summary>
public static class ExperimentGoalInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ExperimentGoalInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(ExperimentGoalInfo.OBJECT_TYPE);
        }

        info!.ClassName = "Baseline.ExperimentGoal";
        info.ClassTableName = "Baseline_ExperimentGoal";
        info.ClassDisplayName = "Experiment Goal";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(ExperimentGoalInfo.ExperimentGoalID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.ExperimentGoalGUID),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        // FK to Baseline_Experiment
        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.ExperimentID),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            ReferenceToObjectType = ExperimentInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required,
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.Name),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.CodeName),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.GoalType),
            DataType = FieldDataType.Text,
            Size = 20,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "PageVisit"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.Target),
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.IsPrimary),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentGoalInfo.GoalValue),
            DataType = FieldDataType.Double,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        info!.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void EnsureField(FormInfo formInfo, FormFieldInfo field)
    {
        var existing = formInfo.GetFormField(field.Name);
        if (existing == null)
        {
            formInfo.AddFormItem(field);
        }
        else
        {
            formInfo.UpdateFormField(field.Name, field);
        }
    }
}
