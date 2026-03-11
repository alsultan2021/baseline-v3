using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Wishlist module installer.
/// </summary>
public interface IWishlistModuleInstaller
{
    /// <summary>
    /// Installs the wishlist data class.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for WishlistItem data class.
/// </summary>
public class WishlistModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : IWishlistModuleInstaller
{
    /// <summary>
    /// Installs the WishlistItem module components.
    /// </summary>
    public void Install()
    {
        try
        {
            var resourceInfo = GetOrCreateModule();
            InstallWishlistItemDataClass(resourceInfo);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(
                "WishlistModuleInstaller",
                "Install",
                ex);
            throw;
        }
    }

    private ResourceInfo GetOrCreateModule()
    {
        var resourceInfo = resourceInfoProvider.Get(BaselineEcommerceConstants.ModuleName)
            ?? new ResourceInfo();

        resourceInfo.ResourceName = BaselineEcommerceConstants.ModuleName;
        resourceInfo.ResourceDisplayName = BaselineEcommerceConstants.ModuleDisplayName;
        resourceInfo.ResourceDescription = BaselineEcommerceConstants.ModuleDescription;
        resourceInfo.ResourceIsInDevelopment = false;

        if (resourceInfo.HasChanged)
        {
            resourceInfoProvider.Set(resourceInfo);
        }

        return resourceInfo;
    }

    private static void InstallWishlistItemDataClass(ResourceInfo resourceInfo)
    {
        var className = WishlistItemInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = className.Replace(".", "_");
        info.ClassDisplayName = "Wishlist Item";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(WishlistItemInfo.WishlistItemID));

        // Member ID field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(WishlistItemInfo.WishlistItemMemberID),
            Visible = false,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false
        });

        // Product content item ID field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(WishlistItemInfo.WishlistItemProductID),
            Visible = false,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false
        });

        // Item type discriminator field ("Product" or "Event")
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(WishlistItemInfo.WishlistItemType),
            Visible = false,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "Product"
        });

        // Created when field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(WishlistItemInfo.WishlistItemCreatedWhen),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        info.ClassFormDefinition = formInfo.GetXmlDefinition();

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }
}
