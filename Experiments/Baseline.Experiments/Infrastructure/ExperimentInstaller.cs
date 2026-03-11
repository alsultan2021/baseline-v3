using Baseline.Experiments.Classes;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Experiments.Infrastructure;

/// <summary>
/// Installer for the Baseline_Experiment table.
/// </summary>
public static class ExperimentInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ExperimentInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(ExperimentInfo.OBJECT_TYPE);
        }

        info!.ClassName = "Baseline.Experiment";
        info.ClassTableName = "Baseline_Experiment";
        info.ClassDisplayName = "Experiment";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(ExperimentInfo.ExperimentID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.ExperimentGUID),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.Name),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.Description),
            DataType = FieldDataType.LongText,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.ExperimentType),
            DataType = FieldDataType.Text,
            Size = 20,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "Page"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.Status),
            DataType = FieldDataType.Text,
            Size = 20,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "Draft"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.ConfidenceLevel),
            DataType = FieldDataType.Double,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.MinimumSampleSize),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.StartDateUtc),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.EndDateUtc),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.TargetPath),
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.WidgetIdentifier),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.TrafficAllocationJson),
            DataType = FieldDataType.LongText,
            Enabled = true,
            Visible = false,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.CreatedAtUtc),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = false,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.ModifiedAtUtc),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = false,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentInfo.CreatedBy),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = false,
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
