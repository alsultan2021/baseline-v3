using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Ecommerce Tax module installer.
/// </summary>
public interface ITaxModuleInstaller
{
    /// <summary>
    /// Installs the tax module, including the TaxClass data class and default tax classes.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for the Ecommerce Tax module.
/// Creates the TaxClass data class and seeds default tax classes.
/// </summary>
public class TaxModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : ITaxModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider = resourceInfoProvider;
    private readonly IEventLogService _eventLogService = eventLogService;

    /// <summary>
    /// Installs the Tax module components.
    /// </summary>
    public void Install()
    {
        try
        {
            var resourceInfo = InstallModule();
            InstallTaxClassDataClass(resourceInfo);

            // Note: Tax classes should be created via the Commerce Configuration UI.
            // No sample data is seeded automatically.
        }
        catch (Exception ex)
        {
            _eventLogService.LogException(
                nameof(TaxModuleInstaller),
                "INSTALL_ERROR",
                ex,
                "Failed to install Tax module.");
            throw;
        }
    }

    /// <summary>
    /// Gets or creates the Baseline Ecommerce module resource.
    /// </summary>
    private ResourceInfo InstallModule()
    {
        var resourceInfo = _resourceInfoProvider.Get(BaselineEcommerceConstants.ModuleName)
            ?? new ResourceInfo();

        resourceInfo.ResourceName = BaselineEcommerceConstants.ModuleName;
        resourceInfo.ResourceDisplayName = BaselineEcommerceConstants.ModuleDisplayName;
        resourceInfo.ResourceDescription = BaselineEcommerceConstants.ModuleDescription;
        resourceInfo.ResourceIsInDevelopment = false;

        if (resourceInfo.HasChanged)
        {
            _resourceInfoProvider.Set(resourceInfo);
        }

        return resourceInfo;
    }

    /// <summary>
    /// Creates or updates the TaxClass data class.
    /// </summary>
    private static void InstallTaxClassDataClass(ResourceInfo resourceInfo)
    {
        // Use ObjectClassName consistently - this is "Baseline.TaxClass"
        var className = TaxClassInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = TaxClassInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = TaxClassInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Tax Class";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(TaxClassInfo.TaxClassID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // Code name field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassName),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Code Name"
        });

        // Display name field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassDisplayName),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Display Name"
        });

        // Description field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassDescription),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Description"
        });

        // Default rate field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassDefaultRate),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Default Rate (%)"
        });

        // Is default field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassIsDefault),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false",
            Caption = "Is Default"
        });

        // Is exempt field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassIsExempt),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false",
            Caption = "Tax Exempt"
        });

        // Order field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassOrder),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Display Order"
        });

        // Enabled field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassEnabled),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Enabled"
        });

        // Last modified field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(TaxClassInfo.TaxClassLastModified),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    /// <summary>
    /// Adds a form field to the form info.
    /// </summary>
    private static void AddFormField(FormInfo formInfo, FormFieldInfo field)
    {
        formInfo.AddFormItem(field);
    }

    /// <summary>
    /// Sets the form definition, merging with existing if updating.
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
