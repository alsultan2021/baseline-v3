using Baseline.Experiments.Classes;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Experiments.Infrastructure;

/// <summary>
/// Installer for the Baseline_ExperimentVariant table.
/// </summary>
public static class ExperimentVariantInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(ExperimentVariantInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(ExperimentVariantInfo.OBJECT_TYPE);
        }

        info!.ClassName = "Baseline.ExperimentVariant";
        info.ClassTableName = "Baseline_ExperimentVariant";
        info.ClassDisplayName = "Experiment Variant";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(ExperimentVariantInfo.ExperimentVariantID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.ExperimentVariantGUID),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        // FK to Baseline_Experiment
        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.ExperimentID),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            ReferenceToObjectType = ExperimentInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required,
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.Name),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.Description),
            DataType = FieldDataType.LongText,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.IsControl),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.Weight),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.Configuration),
            DataType = FieldDataType.LongText,
            Enabled = true,
            Visible = false,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.ContentPath),
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(ExperimentVariantInfo.WidgetConfiguration),
            DataType = FieldDataType.LongText,
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
