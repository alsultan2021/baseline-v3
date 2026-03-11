using CMS.Core;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Gift Card module installer.
/// </summary>
public interface IGiftCardModuleInstaller
{
    /// <summary>
    /// Installs the gift card module components including data classes.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for GiftCard data class.
/// Creates the data class for gift card management.
/// </summary>
public class GiftCardModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : IGiftCardModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider = resourceInfoProvider;
    private readonly IEventLogService _eventLogService = eventLogService;

    /// <summary>
    /// Installs the GiftCard module components.
    /// </summary>
    public void Install()
    {
        try
        {
            // Get or create the module resource
            var resourceInfo = GetOrCreateModule();

            // Install data class
            InstallGiftCardDataClass(resourceInfo);
        }
        catch (Exception ex)
        {
            _eventLogService.LogException(
                "GiftCardModuleInstaller",
                "Install",
                ex);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates the Baseline Ecommerce module resource.
    /// </summary>
    private ResourceInfo GetOrCreateModule()
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
    /// Creates or updates the GiftCard data class.
    /// </summary>
    private static void InstallGiftCardDataClass(ResourceInfo resourceInfo)
    {
        var className = GiftCardInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = GiftCardInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = GiftCardInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Gift Card";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(GiftCardInfo.GiftCardID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // Code field - unique redemption code
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardCode),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Gift Card Code"
        });

        // Initial amount field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardInitialAmount),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Initial Amount"
        });

        // Remaining balance field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardRemainingBalance),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Remaining Balance"
        });

        // Currency ID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardCurrencyID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Currency"
        });

        // Recipient Member ID field (optional)
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardRecipientMemberID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Recipient Member"
        });

        // Redeemed by Member ID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardRedeemedByMemberID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Redeemed By Member"
        });

        // Status field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardStatus),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = GiftCardStatuses.Active,
            Caption = "Status"
        });

        // Expires at field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardExpiresAt),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Expires At"
        });

        // Redeemed when field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardRedeemedWhen),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Redeemed When"
        });

        // Enabled field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardEnabled),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Enabled"
        });

        // Created when field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardCreatedWhen),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        // Last modified field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardLastModified),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        // Notes field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(GiftCardInfo.GiftCardNotes),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Admin Notes"
        });

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    private static void AddFormField(FormInfo formInfo, FormFieldInfo field)
    {
        formInfo.AddFormItem(field);
    }

    private static void SetFormDefinition(DataClassInfo info, FormInfo form)
    {
        info.ClassFormDefinition = form.GetXmlDefinition();
    }
}
