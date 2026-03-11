using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.AI.Admin.Installers;

/// <summary>
/// Installer for the BaselineAI.Settings data class and database table.
/// </summary>
internal static class BaselineAISettingsInstaller
{
    public static void Install(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(BaselineAISettingsInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(BaselineAISettingsInfo.OBJECT_TYPE);

        info.ClassName = BaselineAISettingsInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = BaselineAISettingsInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Baseline AI Settings";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(BaselineAISettingsInfo.BaselineAISettingsID));

        var formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.BaselineAISettingsGuid),
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
            Name = nameof(BaselineAISettingsInfo.BaselineAISettingsName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.BaselineAISettingsDisplayName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.BaselineAISettingsLastModified),
            AllowEmpty = false,
            Visible = false,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            DefaultValue = "##NOW##"
        };
        formInfo.AddFormItem(formItem);

        // Feature toggle fields
        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.EnableVectorSearch),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "true",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.EnableChatbot),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "true",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.EnableAutoTagging),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "true",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.EnableSearchSuggestions),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "true",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        // Chatbot branding fields
        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.ChatbotTitle),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = FieldDataType.Text,
            DefaultValue = "AI Assistant",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.ChatbotPlaceholder),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 200,
            DataType = FieldDataType.Text,
            DefaultValue = "Ask me anything...",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.ChatbotWelcomeMessage),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 500,
            DataType = FieldDataType.Text,
            DefaultValue = "Hello! How can I help you today?",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.ChatbotThemeColor),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 50,
            DataType = FieldDataType.Text,
            DefaultValue = "#0d6efd",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.ChatbotPosition),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 50,
            DataType = FieldDataType.Text,
            DefaultValue = "bottom-right",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.ChatbotSystemPrompt),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        // Auto-tagging fields
        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingMinConfidence),
            AllowEmpty = false,
            Visible = true,
            Precision = 2,
            DataType = FieldDataType.Decimal,
            DefaultValue = "0.7",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingMaxTagsPerTaxonomy),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "5",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingUseLLM),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "false",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingAutoApply),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Boolean,
            DefaultValue = "false",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingAutoApplyThreshold),
            AllowEmpty = false,
            Visible = true,
            Precision = 2,
            DataType = FieldDataType.Decimal,
            DefaultValue = "0.9",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingEnabledTaxonomies),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            DefaultValue = "[]",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingEligibleContentTypes),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            DefaultValue = "[]",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingAnalyzedFields),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            DefaultValue = "[\"Title\", \"Description\", \"Content\"]",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.AutoTaggingLLMPrompt),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        // RAG configuration fields
        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.RAGTopK),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "5",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.RAGSimilarityThreshold),
            AllowEmpty = false,
            Visible = true,
            Precision = 2,
            DataType = FieldDataType.Decimal,
            DefaultValue = "0.7",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.RAGSystemPrompt),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.LongText,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(BaselineAISettingsInfo.RAGMaxContextTokens),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Integer,
            DefaultValue = "4000",
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
}
