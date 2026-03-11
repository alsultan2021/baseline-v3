using CMS.FormEngine;

namespace Baseline.Core.Installers.Schemas;

/// <summary>
/// Fluent builder for creating and updating form fields in reusable schemas.
/// </summary>
public class FormFieldBuilder
{
    private readonly FormInfo _form;
    private readonly Guid _schemaGuid;

    public FormFieldBuilder(FormInfo form, Guid schemaGuid)
    {
        _form = form;
        _schemaGuid = schemaGuid;
    }

    /// <summary>
    /// Adds or updates a text field in the form.
    /// </summary>
    public FormFieldBuilder TextField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        int size = 200,
        string componentName = "Kentico.Administration.TextInput",
        bool required = false)
    {
        var existingField = _form.GetFormField(name);
        var field = existingField ?? new FormFieldInfo();

        field.Name = name;
        field.AllowEmpty = !required;
        field.Precision = 0;
        field.DataType = size > 0 ? "text" : "longtext";
        field.Enabled = true;
        field.Visible = true;
        field.Guid = fieldGuid;

        if (size > 0)
        {
            field.Size = size;
        }

        field.SetComponentName(componentName);
        field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);

        if (!string.IsNullOrEmpty(description))
        {
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
        }

        field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
        field.Properties["kxp_schema_identifier"] = _schemaGuid.ToString().ToLower();

        if (existingField != null)
        {
            _form.UpdateFormField(name, field);
        }
        else
        {
            _form.AddFormItem(field);
        }

        return this;
    }

    /// <summary>
    /// Adds or updates a long text field (textarea) in the form.
    /// </summary>
    public FormFieldBuilder LongTextField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        string componentName = "Kentico.Administration.TextArea")
    {
        return TextField(name, fieldGuid, caption, description, size: 0, componentName);
    }

    /// <summary>
    /// Adds or updates a boolean (checkbox) field in the form.
    /// </summary>
    public FormFieldBuilder BooleanField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null)
    {
        var existingField = _form.GetFormField(name);
        var field = existingField ?? new FormFieldInfo();

        field.Name = name;
        field.AllowEmpty = true;
        field.Precision = 0;
        field.DataType = "boolean";
        field.Enabled = true;
        field.Visible = true;
        field.Guid = fieldGuid;
        field.SetComponentName("Kentico.Administration.Checkbox");
        field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);

        if (!string.IsNullOrEmpty(description))
        {
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
        }

        field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");
        field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
        field.Properties["kxp_schema_identifier"] = _schemaGuid.ToString().ToLower();

        if (existingField != null)
        {
            _form.UpdateFormField(name, field);
        }
        else
        {
            _form.AddFormItem(field);
        }

        return this;
    }

    /// <summary>
    /// Adds or updates a dropdown selector field in the form.
    /// </summary>
    public FormFieldBuilder DropdownField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        int size = 200)
    {
        return TextField(name, fieldGuid, caption, description, size, "Kentico.Administration.DropDownSelector");
    }

    /// <summary>
    /// Adds or updates a web page selector field in the form.
    /// </summary>
    public FormFieldBuilder WebPageSelectorField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null)
    {
        var existingField = _form.GetFormField(name);
        var field = existingField ?? new FormFieldInfo();

        field.Name = name;
        field.AllowEmpty = true;
        field.Precision = 0;
        field.DataType = "webpages";
        field.Enabled = true;
        field.Visible = true;
        field.Guid = fieldGuid;
        field.SetComponentName("Kentico.Administration.WebPageSelector");
        field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);

        if (!string.IsNullOrEmpty(description))
        {
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
        }

        field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
        field.Properties["kxp_schema_identifier"] = _schemaGuid.ToString().ToLower();

        if (existingField != null)
        {
            _form.UpdateFormField(name, field);
        }
        else
        {
            _form.AddFormItem(field);
        }

        return this;
    }

    /// <summary>
    /// Adds or updates a decimal number field in the form.
    /// </summary>
    public FormFieldBuilder DecimalField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        int precision = 2,
        int scale = 18,
        bool required = false)
    {
        var existingField = _form.GetFormField(name);
        var field = existingField ?? new FormFieldInfo();

        field.Name = name;
        field.AllowEmpty = !required;
        field.Precision = precision;
        field.Size = scale;
        field.DataType = "decimal";
        field.Enabled = true;
        field.Visible = true;
        field.Guid = fieldGuid;
        field.SetComponentName("Kentico.Administration.DecimalNumberInput");
        field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);

        if (!string.IsNullOrEmpty(description))
        {
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
        }

        field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
        field.Properties["kxp_schema_identifier"] = _schemaGuid.ToString().ToLower();

        if (existingField != null)
        {
            _form.UpdateFormField(name, field);
        }
        else
        {
            _form.AddFormItem(field);
        }

        return this;
    }

    /// <summary>
    /// Adds or updates an integer number field in the form.
    /// </summary>
    public FormFieldBuilder IntegerField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool required = false)
    {
        var existingField = _form.GetFormField(name);
        var field = existingField ?? new FormFieldInfo();

        field.Name = name;
        field.AllowEmpty = !required;
        field.Precision = 0;
        field.DataType = "integer";
        field.Enabled = true;
        field.Visible = true;
        field.Guid = fieldGuid;
        field.SetComponentName("Kentico.Administration.NumberInput");
        field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);

        if (!string.IsNullOrEmpty(description))
        {
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
        }

        field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
        field.Properties["kxp_schema_identifier"] = _schemaGuid.ToString().ToLower();

        if (existingField != null)
        {
            _form.UpdateFormField(name, field);
        }
        else
        {
            _form.AddFormItem(field);
        }

        return this;
    }
}
