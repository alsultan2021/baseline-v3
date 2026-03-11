using Baseline.AI.Data;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.AI.Admin.Installers;

/// <summary>
/// Installer for AI Knowledge Base related data classes and database tables.
/// Follows the AIUN community module installer pattern.
/// </summary>
internal static class AIKnowledgeBaseInstaller
{
    public static void Install(ResourceInfo resource)
    {
        InstallKnowledgeBase(resource);
        InstallKnowledgeBasePath(resource);
        InstallIndexQueue(resource);
        InstallContentFingerprint(resource);
        InstallRebuildJob(resource);
    }

    private static void InstallKnowledgeBase(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(AIKnowledgeBaseInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(AIKnowledgeBaseInfo.OBJECT_TYPE);

        info.ClassName = AIKnowledgeBaseInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = AIKnowledgeBaseInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "AI Knowledge Base";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AIKnowledgeBaseInfo.KnowledgeBaseId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseGuid),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Guid,
            Enabled = true,
            DefaultValue = "##NEWGUID##"
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Code name");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseDisplayName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 500,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Display name");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseStrategyName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Strategy name");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseStrategyHash),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            Size = 64,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseStatus),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseLanguages),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseChannels),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseReusableTypes),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseDocumentCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseChunkCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseLastRebuild),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseLastError),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseLastRunDurationMs),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongInteger,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseLastModified),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            DefaultValue = "##NOW##",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseSchemaVersion),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "1",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseDefaultTopK),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "5",
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.NumberInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Default top K");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseDefaultThreshold),
            AllowEmpty = false,
            Visible = true,
            Precision = 2,
            DataType = FieldDataType.Decimal,
            DefaultValue = "0.7",
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.DecimalNumberInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Default threshold");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseDefaultTemperature),
            AllowEmpty = false,
            Visible = true,
            Precision = 2,
            DataType = FieldDataType.Decimal,
            DefaultValue = "0.7",
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.DecimalNumberInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Default temperature");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseRebuildWebhook),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 500,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Rebuild webhook URL");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBaseInfo.KnowledgeBaseAutoRebuildOnPublish),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "true",
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.Checkbox");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Auto rebuild on publish");
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        EnsureHiddenFields(info,
        [
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseStatus),
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseLanguages),
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseChannels),
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseReusableTypes),
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseDocumentCount),
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseChunkCount),
            nameof(AIKnowledgeBaseInfo.KnowledgeBaseLastRebuild)
        ]);

        EnsureVisibleFieldComponents(info, new Dictionary<string, (string Component, string Caption)>
        {
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseName)] = ("Kentico.Administration.TextInput", "Code name"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseDisplayName)] = ("Kentico.Administration.TextInput", "Display name"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseStrategyName)] = ("Kentico.Administration.TextInput", "Strategy name"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseDefaultTopK)] = ("Kentico.Administration.NumberInput", "Default top K"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseDefaultThreshold)] = ("Kentico.Administration.DecimalNumberInput", "Default threshold"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseDefaultTemperature)] = ("Kentico.Administration.DecimalNumberInput", "Default temperature"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseRebuildWebhook)] = ("Kentico.Administration.TextInput", "Rebuild webhook URL"),
            [nameof(AIKnowledgeBaseInfo.KnowledgeBaseAutoRebuildOnPublish)] = ("Kentico.Administration.Checkbox", "Auto rebuild on publish")
        });

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    private static void InstallKnowledgeBasePath(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(AIKnowledgeBasePathInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(AIKnowledgeBasePathInfo.OBJECT_TYPE);

        info.ClassName = AIKnowledgeBasePathInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = AIKnowledgeBasePathInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "AI Knowledge Base Path";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AIKnowledgeBasePathInfo.PathId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathGuid),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Guid,
            DefaultValue = "##NEWGUID##",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathLastModified),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            DefaultValue = "##NOW##",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathChannelId),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathChannelName),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Channel name");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathIncludePattern),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 1000,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Include pattern");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathExcludePattern),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 1000,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Exclude pattern");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathContentTypes),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.TextArea");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Content types (JSON)");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathPriority),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.NumberInput");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Priority");
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIKnowledgeBasePathInfo.PathIncludeChildren),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "true",
            Enabled = true
        };
        formItem.SetComponentName("Kentico.Administration.Checkbox");
        formItem.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, "Include children");
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        EnsureHiddenFields(info,
        [
            nameof(AIKnowledgeBasePathInfo.PathKnowledgeBaseId),
            nameof(AIKnowledgeBasePathInfo.PathChannelId)
        ]);

        EnsureVisibleFieldComponents(info, new Dictionary<string, (string Component, string Caption)>
        {
            [nameof(AIKnowledgeBasePathInfo.PathChannelName)] = ("Kentico.Administration.TextInput", "Channel name"),
            [nameof(AIKnowledgeBasePathInfo.PathIncludePattern)] = ("Kentico.Administration.TextInput", "Include pattern"),
            [nameof(AIKnowledgeBasePathInfo.PathExcludePattern)] = ("Kentico.Administration.TextInput", "Exclude pattern"),
            [nameof(AIKnowledgeBasePathInfo.PathContentTypes)] = ("Kentico.Administration.TextArea", "Content types (JSON)"),
            [nameof(AIKnowledgeBasePathInfo.PathPriority)] = ("Kentico.Administration.NumberInput", "Priority"),
            [nameof(AIKnowledgeBasePathInfo.PathIncludeChildren)] = ("Kentico.Administration.Checkbox", "Include children")
        });

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    private static void InstallIndexQueue(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(AIIndexQueueInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(AIIndexQueueInfo.OBJECT_TYPE);

        info.ClassName = AIIndexQueueInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = AIIndexQueueInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "AI Index Queue";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AIIndexQueueInfo.QueueId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueKnowledgeBaseId),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueContentItemGuid),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Guid,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueLanguageCode),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            Size = 50,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueChannelId),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueOperationType),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueCreated),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            DefaultValue = "##NOW##",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueRetryCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIIndexQueueInfo.QueueLastError),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    private static void InstallContentFingerprint(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(AIContentFingerprintInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(AIContentFingerprintInfo.OBJECT_TYPE);

        info.ClassName = AIContentFingerprintInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = AIContentFingerprintInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "AI Content Fingerprint";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AIContentFingerprintInfo.FingerprintId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintKnowledgeBaseId),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintContentGuid),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Guid,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintLanguageCode),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            Size = 50,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintChannelId),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintHash),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            Size = 64,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintLastChecked),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            DefaultValue = "##NOW##",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIContentFingerprintInfo.FingerprintContentPath),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            Size = 1000,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    private static void InstallRebuildJob(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(AIRebuildJobInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(AIRebuildJobInfo.OBJECT_TYPE);

        info.ClassName = AIRebuildJobInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = AIRebuildJobInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "AI Rebuild Job";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(AIRebuildJobInfo.JobId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobKnowledgeBaseId),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobStatus),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobScannedCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobTotalCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobChunkCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobEmbeddedCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobFailedCount),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "0",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobStarted),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            DefaultValue = "##NOW##",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobFinished),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobLastError),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(AIRebuildJobInfo.JobFailedItems),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    /// <summary>
    /// Ensure that the form is upserted with any existing form
    /// </summary>
    private static void SetFormDefinition(DataClassInfo info, FormInfo form)
    {
        if (info.ClassID > 0)
        {
            var existingForm = new FormInfo(info.ClassFormDefinition);
            existingForm.CombineWithForm(form, new());
            info.ClassFormDefinition = existingForm.GetXmlDefinition();
        }
        else
        {
            info.ClassFormDefinition = form.GetXmlDefinition();
        }
    }

    /// <summary>
    /// Forces internal/system fields to be hidden even if a previous installer run left them visible.
    /// CombineWithForm never updates existing field properties, so stale visible flags persist.
    /// </summary>
    private static void EnsureHiddenFields(DataClassInfo info, string[] fieldNames)
    {
        if (info.ClassID == 0)
        {
            return;
        }

        var formInfo = new FormInfo(info.ClassFormDefinition);
        bool changed = false;

        foreach (string fieldName in fieldNames)
        {
            var field = formInfo.GetFormField(fieldName);
            if (field is null || !field.Visible)
            {
                continue;
            }

            field.Visible = false;
            formInfo.UpdateFormField(fieldName, field);
            changed = true;
        }

        if (changed)
        {
            info.ClassFormDefinition = formInfo.GetXmlDefinition();
        }
    }

    /// <summary>
    /// Patches existing form field definitions to ensure visible fields have component identifiers.
    /// CombineWithForm only adds new fields — it never updates existing field properties.
    /// This method fixes fields that already exist in the DB without component names.
    /// </summary>
    private static void EnsureVisibleFieldComponents(
        DataClassInfo info,
        Dictionary<string, (string Component, string Caption)> fieldComponents)
    {
        if (info.ClassID == 0)
        {
            return;
        }

        var formInfo = new FormInfo(info.ClassFormDefinition);
        bool changed = false;

        foreach (var (fieldName, (component, caption)) in fieldComponents)
        {
            var field = formInfo.GetFormField(fieldName);
            if (field is null)
            {
                continue;
            }

            field.Visible = true;
            field.SetComponentName(component);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            formInfo.UpdateFormField(fieldName, field);
            changed = true;
        }

        if (changed)
        {
            info.ClassFormDefinition = formInfo.GetXmlDefinition();
        }
    }
}
