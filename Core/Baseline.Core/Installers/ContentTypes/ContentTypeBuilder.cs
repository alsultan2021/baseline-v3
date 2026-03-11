using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Helpers;

namespace Baseline.Core.Installers.ContentTypes;

/// <summary>
/// Fluent builder for creating and updating content types (Reusable, Website, Headless).
/// Reduces boilerplate code for content type installation.
/// </summary>
public class ContentTypeBuilder
{
    private readonly string _className;
    private readonly string _displayName;
    private Guid _classGuid;
    private string _tableName;
    private string _iconClass = "xp-doc-inverted";
    private string _shortName;
    private ContentTypeType _contentTypeType = ContentTypeType.Reusable;
    private bool _hasUrl = true;
    private FormInfo? _formInfo;
    private DataClassInfo? _existingClass;
    private DataClassInfo? _dataClass;
    private readonly List<Action<FormInfo>> _fieldActions = [];
    private readonly List<Guid> _schemaGuids = [];
    private Guid? _contentItemDataIdGuid;
    private Guid? _commonDataIdGuid;
    private Guid? _dataGuidGuid;

    /// <summary>
    /// Content type types supported by Xperience.
    /// </summary>
    public enum ContentTypeType
    {
        Reusable,
        Website,
        Headless
    }

    /// <summary>
    /// Creates a new content type builder.
    /// </summary>
    /// <param name="className">Full class name (e.g., "Generic.Image")</param>
    /// <param name="displayName">Display name shown in admin UI</param>
    public ContentTypeBuilder(string className, string displayName)
    {
        _className = className;
        _displayName = displayName;
        _classGuid = Guid.NewGuid();
        _tableName = className.Replace(".", "_");
        _shortName = className.Replace(".", "");
    }

    /// <summary>
    /// Creates a new content type builder with a specific GUID.
    /// </summary>
    /// <param name="className">Full class name (e.g., "Generic.Image")</param>
    /// <param name="displayName">Display name shown in admin UI</param>
    /// <param name="classGuid">Unique GUID for the content type</param>
    public ContentTypeBuilder(string className, string displayName, Guid classGuid)
        : this(className, displayName)
    {
        _classGuid = classGuid;
    }

    /// <summary>
    /// Sets the content type GUID.
    /// </summary>
    public ContentTypeBuilder WithGuid(string guid)
    {
        _classGuid = Guid.Parse(guid);
        return this;
    }

    /// <summary>
    /// Sets the GUIDs for system fields (ContentItemDataID, CommonDataID, DataGUID).
    /// Use this to maintain stable GUIDs across environments.
    /// </summary>
    public ContentTypeBuilder WithSystemFieldGuids(
        string contentItemDataIdGuid,
        string commonDataIdGuid,
        string dataGuidGuid)
    {
        _contentItemDataIdGuid = Guid.Parse(contentItemDataIdGuid);
        _commonDataIdGuid = Guid.Parse(commonDataIdGuid);
        _dataGuidGuid = Guid.Parse(dataGuidGuid);
        return this;
    }

    /// <summary>
    /// Sets the database table name (defaults to ClassName with dots replaced by underscores).
    /// </summary>
    public ContentTypeBuilder WithTableName(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    /// <summary>
    /// Sets the icon class for the content type in admin UI.
    /// </summary>
    public ContentTypeBuilder WithIcon(string iconClass)
    {
        _iconClass = iconClass;
        return this;
    }

    /// <summary>
    /// Sets the short name used for generated code.
    /// </summary>
    public ContentTypeBuilder WithShortName(string shortName)
    {
        _shortName = shortName;
        return this;
    }

    /// <summary>
    /// Sets this as a Reusable content type (default).
    /// </summary>
    public ContentTypeBuilder AsReusable()
    {
        _contentTypeType = ContentTypeType.Reusable;
        return this;
    }

    /// <summary>
    /// Sets this as a Website (page) content type.
    /// </summary>
    public ContentTypeBuilder AsWebsite()
    {
        _contentTypeType = ContentTypeType.Website;
        return this;
    }

    /// <summary>
    /// Sets this as a Headless content type.
    /// </summary>
    public ContentTypeBuilder AsHeadless()
    {
        _contentTypeType = ContentTypeType.Headless;
        return this;
    }

    /// <summary>
    /// Sets whether this content type has a URL (for website pages).
    /// </summary>
    public ContentTypeBuilder WithUrl(bool hasUrl = true)
    {
        _hasUrl = hasUrl;
        return this;
    }

    /// <summary>
    /// Adds a reusable schema reference to this content type.
    /// </summary>
    /// <param name="schemaGuid">The GUID of the reusable schema</param>
    public ContentTypeBuilder WithSchema(Guid schemaGuid)
    {
        _schemaGuids.Add(schemaGuid);
        return this;
    }

    /// <summary>
    /// Adds a text field to the content type.
    /// </summary>
    public ContentTypeBuilder WithTextField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        int size = 200,
        bool required = false,
        string componentName = "Kentico.Administration.TextInput")
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 0;
            field.Size = size;
            field.DataType = "text";
            field.Enabled = true;
            field.Visible = true;
            field.Guid = fieldGuid;
            field.SetComponentName(componentName);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a long text (textarea) field to the content type.
    /// </summary>
    public ContentTypeBuilder WithLongTextField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool required = false,
        string componentName = "Kentico.Administration.TextArea",
        int? minRows = null,
        int? maxRows = null)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 0;
            field.DataType = "longtext";
            field.Enabled = true;
            field.Visible = true;
            field.Guid = fieldGuid;
            field.SetComponentName(componentName);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (minRows.HasValue)
            {
                field.Settings["MinRowsNumber"] = minRows.Value;
            }

            if (maxRows.HasValue)
            {
                field.Settings["MaxRowsNumber"] = maxRows.Value;
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a content item asset (file upload) field to the content type.
    /// </summary>
    public ContentTypeBuilder WithAssetField(
        string name,
        Guid fieldGuid,
        string caption,
        string? allowedExtensions = null,
        bool required = false,
        string? requiredErrorMessage = null)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 0;
            field.DataType = "contentitemasset";
            field.Enabled = true;
            field.Visible = true;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.ContentItemAssetUploader");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(allowedExtensions))
            {
                field.Settings["AllowedExtensions"] = allowedExtensions;
            }

            if (required && !string.IsNullOrEmpty(requiredErrorMessage))
            {
                field.ValidationRuleConfigurationsXmlData = $@"<validationrulesdata><ValidationRuleConfiguration><ValidationRuleIdentifier>Kentico.Administration.RequiredValue</ValidationRuleIdentifier><RuleValues><ErrorMessage>{requiredErrorMessage}</ErrorMessage></RuleValues></ValidationRuleConfiguration></validationrulesdata>";
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a boolean (checkbox) field to the content type.
    /// </summary>
    public ContentTypeBuilder WithBooleanField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool defaultValue = false)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
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
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (defaultValue)
            {
                field.SetPropertyValue(FormFieldPropertyEnum.DefaultValue, "True");
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a rich text (HTML) field to the content type.
    /// </summary>
    public ContentTypeBuilder WithRichTextField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool required = false)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 0;
            field.DataType = "longtext";
            field.Enabled = true;
            field.Visible = true;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.RichTextEditor");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a content items selector field to the content type.
    /// </summary>
    public ContentTypeBuilder WithContentItemsField(
        string name,
        Guid fieldGuid,
        string caption,
        string? allowedContentTypes = null,
        int? minimumItems = null,
        int? maximumItems = null,
        string? description = null)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = true;
            field.Precision = 0;
            field.DataType = "contentitemreference";
            field.Enabled = true;
            field.Visible = true;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.ContentItemSelector");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (!string.IsNullOrEmpty(allowedContentTypes))
            {
                field.Settings["AllowedContentItemTypeIdentifiers"] = allowedContentTypes;
            }

            if (minimumItems.HasValue)
            {
                field.Settings["MinimumItems"] = minimumItems.Value;
            }

            if (maximumItems.HasValue)
            {
                field.Settings["MaximumItems"] = maximumItems.Value;
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a web pages selector field to the content type.
    /// </summary>
    public ContentTypeBuilder WithWebPagesField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        int? maximumPages = null)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
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
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (maximumPages.HasValue)
            {
                field.Settings["MaximumPages"] = maximumPages.Value;
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a taxonomy (tags) field to the content type.
    /// </summary>
    public ContentTypeBuilder WithTaxonomyField(
        string name,
        Guid fieldGuid,
        string caption,
        Guid taxonomyGuid,
        string? description = null)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = true;
            field.Precision = 0;
            field.DataType = "taxonomy";
            field.Enabled = true;
            field.Visible = true;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.TagSelector");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");
            field.Settings["TaxonomyGroup"] = taxonomyGuid.ToString();

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds an integer field to the content type.
    /// </summary>
    public ContentTypeBuilder WithIntegerField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool required = false,
        int? defaultValue = null,
        bool visible = true)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 0;
            field.DataType = "integer";
            field.Enabled = true;
            field.Visible = visible;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.NumberInput");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (defaultValue.HasValue)
            {
                field.SetPropertyValue(FormFieldPropertyEnum.DefaultValue, defaultValue.Value.ToString());
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a decimal field to the content type.
    /// </summary>
    public ContentTypeBuilder WithDecimalField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool required = false,
        int precision = 10,
        int scale = 2,
        decimal? defaultValue = null,
        bool visible = true)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = scale;
            field.Size = precision;
            field.DataType = "decimal";
            field.Enabled = true;
            field.Visible = visible;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.DecimalNumberInput");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (defaultValue.HasValue)
            {
                field.SetPropertyValue(FormFieldPropertyEnum.DefaultValue, defaultValue.Value.ToString());
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a date/time field to the content type.
    /// </summary>
    public ContentTypeBuilder WithDateTimeField(
        string name,
        Guid fieldGuid,
        string caption,
        string? description = null,
        bool required = false,
        bool visible = true)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 7;
            field.DataType = "datetime";
            field.Enabled = true;
            field.Visible = visible;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.DateTimeInput");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a dropdown selector field to the content type.
    /// </summary>
    /// <param name="options">Dropdown options in format "value;displayText\r\nvalue2;displayText2"</param>
    public ContentTypeBuilder WithDropdownField(
        string name,
        Guid fieldGuid,
        string caption,
        string options,
        string? description = null,
        bool required = false,
        int size = 200,
        string? defaultValue = null,
        bool visible = true)
    {
        _fieldActions.Add(form =>
        {
            var existingField = form.GetFormField(name);
            var field = existingField ?? new FormFieldInfo();

            field.Name = name;
            field.AllowEmpty = !required;
            field.Precision = 0;
            field.Size = size;
            field.DataType = "text";
            field.Enabled = true;
            field.Visible = visible;
            field.Guid = fieldGuid;
            field.SetComponentName("Kentico.Administration.DropDownSelector");
            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, caption);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescriptionAsHtml, "False");
            field.SetPropertyValue(FormFieldPropertyEnum.ExplanationTextAsHtml, "False");
            field.Settings["Options"] = options;

            if (!string.IsNullOrEmpty(description))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, description);
            }

            if (!string.IsNullOrEmpty(defaultValue))
            {
                field.SetPropertyValue(FormFieldPropertyEnum.DefaultValue, defaultValue);
            }

            if (existingField != null)
            {
                form.UpdateFormField(name, field);
            }
            else
            {
                form.AddFormItem(field);
            }
        });
        return this;
    }

    /// <summary>
    /// Builds and saves the content type to the database.
    /// </summary>
    /// <returns>The created or updated DataClassInfo</returns>
    public DataClassInfo Build()
    {
        // Get or create the class
        _existingClass = DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassName), _className)
            .GetEnumerableTypedResult()
            .FirstOrDefault();

        _dataClass = _existingClass ?? DataClassInfo.New(_className);
        _dataClass.ClassDisplayName = _displayName;
        _dataClass.ClassName = _className;
        _dataClass.ClassTableName = _tableName;
        _dataClass.ClassGUID = _classGuid;
        _dataClass.ClassIconClass = _iconClass;
        _dataClass.ClassHasUnmanagedDbSchema = false;
        _dataClass.ClassType = "Content";
        _dataClass.ClassContentTypeType = _contentTypeType.ToString();
        _dataClass.ClassWebPageHasUrl = _hasUrl;
        _dataClass.ClassShortName = _shortName;

        // Initialize form info
        _formInfo = _existingClass != null
            ? new FormInfo(_existingClass.ClassFormDefinition)
            : FormHelper.GetBasicFormDefinition("ContentItemDataID");

        // Add system fields
        AddSystemFields();

        // Apply field actions
        foreach (var action in _fieldActions)
        {
            action(_formInfo);
        }

        // Add schema references
        AddSchemaReferences();

        // Save the class
        _dataClass.ClassFormDefinition = _formInfo.GetXmlDefinition();
        if (_dataClass.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(_dataClass);
        }

        return _dataClass;
    }

    private void AddSystemFields()
    {
        if (_formInfo == null) return;

        // ContentItemDataID field - ensure GUID is set if specified
        var existingDataId = _formInfo.GetFormField("ContentItemDataID");
        if (existingDataId != null && _contentItemDataIdGuid.HasValue)
        {
            if (!existingDataId.Guid.Equals(_contentItemDataIdGuid.Value))
            {
                existingDataId.Guid = _contentItemDataIdGuid.Value;
                _formInfo.UpdateFormField("ContentItemDataID", existingDataId);
            }
        }

        // ContentItemDataCommonDataID field
        var existingCommonDataId = _formInfo.GetFormField("ContentItemDataCommonDataID");
        var commonDataIdField = existingCommonDataId ?? new FormFieldInfo();
        commonDataIdField.Name = "ContentItemDataCommonDataID";
        commonDataIdField.AllowEmpty = false;
        commonDataIdField.DataType = "integer";
        commonDataIdField.Enabled = true;
        commonDataIdField.Visible = false;
        commonDataIdField.ReferenceToObjectType = "cms.contentitemcommondata";
        commonDataIdField.ReferenceType = ObjectDependencyEnum.Required;
        commonDataIdField.System = true;
        commonDataIdField.Guid = _commonDataIdGuid ?? existingCommonDataId?.Guid ?? Guid.NewGuid();

        if (existingCommonDataId != null)
        {
            _formInfo.UpdateFormField("ContentItemDataCommonDataID", commonDataIdField);
        }
        else
        {
            _formInfo.AddFormItem(commonDataIdField);
        }

        // ContentItemDataGUID field
        var existingDataGuid = _formInfo.GetFormField("ContentItemDataGUID");
        var dataGuidField = existingDataGuid ?? new FormFieldInfo();
        dataGuidField.Name = "ContentItemDataGUID";
        dataGuidField.AllowEmpty = false;
        dataGuidField.DataType = "guid";
        dataGuidField.Enabled = true;
        dataGuidField.Visible = false;
        dataGuidField.System = true;
        dataGuidField.IsUnique = true;
        dataGuidField.IsDummyFieldFromMainForm = true;
        dataGuidField.Guid = _dataGuidGuid ?? existingDataGuid?.Guid ?? Guid.NewGuid();

        if (existingDataGuid != null)
        {
            _formInfo.UpdateFormField("ContentItemDataGUID", dataGuidField);
        }
        else
        {
            _formInfo.AddFormItem(dataGuidField);
        }
    }

    private void AddSchemaReferences()
    {
        if (_formInfo == null) return;

        foreach (var schemaGuid in _schemaGuids)
        {
            var schemaKey = schemaGuid.ToString().ToLower();
            if (_formInfo.GetFormSchema(schemaKey) == null)
            {
                _formInfo.ItemsList.Add(new FormSchemaInfo()
                {
                    Guid = schemaGuid,
                    Name = schemaKey
                });
            }
        }
    }
}
