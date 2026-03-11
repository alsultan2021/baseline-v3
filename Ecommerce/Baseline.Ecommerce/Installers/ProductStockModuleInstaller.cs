using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Ecommerce Product Stock module installer.
/// </summary>
public interface IProductStockModuleInstaller
{
    /// <summary>
    /// Installs the product stock module, including the ProductStock data class.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for the Ecommerce Product Stock module.
/// Creates the ProductStock data class.
/// </summary>
public class ProductStockModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : IProductStockModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider = resourceInfoProvider;
    private readonly IEventLogService _eventLogService = eventLogService;

    /// <summary>
    /// Installs the Product Stock module components.
    /// </summary>
    public void Install()
    {
        try
        {
            var resourceInfo = InstallModule();
            InstallProductStockDataClass(resourceInfo);
        }
        catch (Exception ex)
        {
            _eventLogService.LogException(
                nameof(ProductStockModuleInstaller),
                "INSTALL_ERROR",
                ex,
                "Failed to install Product Stock module.");
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
    /// Creates or updates the ProductStock data class.
    /// </summary>
    private static void InstallProductStockDataClass(ResourceInfo resourceInfo)
    {
        var className = ProductStockInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = ProductStockInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = ProductStockInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Product Stock";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ProductStockInfo.ProductStockID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // Product content item reference field
        // Uses LongText data type to store JSON array of ContentItemReference
        // The Admin UI will use ContentItemSelectorComponent for proper product selection
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockProduct),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Product"
        });

        // Available quantity field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockAvailableQuantity),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        // Reserved quantity field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockReservedQuantity),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        // Minimum threshold field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockMinimumThreshold),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = true
        });

        // Allow backorders field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockAllowBackorders),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        // Tracking enabled field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockTrackingEnabled),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true"
        });

        // Last modified field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(ProductStockInfo.ProductStockLastModified),
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

    private static void AddFormField(FormInfo formInfo, FormFieldInfo field)
    {
        if (formInfo.GetFormField(field.Name) == null)
        {
            formInfo.AddFormItem(field);
        }
        else
        {
            formInfo.UpdateFormField(field.Name, field);
        }
    }

    private static void SetFormDefinition(DataClassInfo info, FormInfo formInfo)
    {
        if (info.ClassID > 0)
        {
            var existingForm = new FormInfo(info.ClassFormDefinition);
            existingForm.CombineWithForm(formInfo, new CombineWithFormSettings
            {
                OverwriteExisting = true,
                RemoveEmptyCategories = true
            });

            info.ClassFormDefinition = existingForm.GetXmlDefinition();
        }
        else
        {
            info.ClassFormDefinition = formInfo.GetXmlDefinition();
        }
    }
}
