using CMS.Core;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Wallet module installer.
/// </summary>
public interface IWalletModuleInstaller
{
    /// <summary>
    /// Installs the wallet module components including data classes.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for Wallet and WalletTransaction data classes.
/// Creates the data classes for member wallet/account management.
/// </summary>
public class WalletModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : IWalletModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider = resourceInfoProvider;
    private readonly IEventLogService _eventLogService = eventLogService;

    /// <summary>
    /// Installs the Wallet module components.
    /// </summary>
    public void Install()
    {
        try
        {
            // Get or create the module resource
            var resourceInfo = GetOrCreateModule();

            // Install data classes
            InstallWalletDataClass(resourceInfo);
            InstallWalletTransactionDataClass(resourceInfo);

            // Note: Wallet types can be configured via taxonomies in the Admin UI.
            // No sample data is seeded automatically.
        }
        catch (Exception ex)
        {
            _eventLogService.LogException(
                "WalletModuleInstaller",
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
    /// Creates or updates the Wallet data class.
    /// </summary>
    private static void InstallWalletDataClass(ResourceInfo resourceInfo)
    {
        var className = WalletInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = WalletInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = WalletInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Wallet";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(WalletInfo.WalletID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // Member ID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletMemberID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Member"
        });

        // Currency ID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletCurrencyID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Currency"
        });

        // Balance field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletBalance),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Balance"
        });

        // Held balance field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletHeldBalance),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Held Balance"
        });

        // Credit limit field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletCreditLimit),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Credit Limit"
        });

        // Wallet type field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletType),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = WalletTypes.StoreCredit,
            Caption = "Wallet Type"
        });

        // Enabled field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletEnabled),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Enabled"
        });

        // Frozen field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletFrozen),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false",
            Caption = "Frozen"
        });

        // Freeze reason field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletFreezeReason),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Freeze Reason"
        });

        // Expires at field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletExpiresAt),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Expires At"
        });

        // Created when field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletCreatedWhen),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        // Last modified field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletInfo.WalletLastModified),
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
    /// Creates or updates the WalletTransaction data class.
    /// </summary>
    private static void InstallWalletTransactionDataClass(ResourceInfo resourceInfo)
    {
        var className = WalletTransactionInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = WalletTransactionInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = WalletTransactionInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Wallet Transaction";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(WalletTransactionInfo.TransactionID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // Wallet ID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionWalletID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Wallet"
        });

        // Transaction type field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionType),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = WalletTransactionTypes.Deposit,
            Caption = "Type"
        });

        // Amount field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionAmount),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Amount"
        });

        // Balance after field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionBalanceAfter),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Balance After"
        });

        // Reference field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionReference),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Reference"
        });

        // Order ID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionOrderID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Order"
        });

        // Description field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionDescription),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Description"
        });

        // Status field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionStatus),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = WalletTransactionStatuses.Completed,
            Caption = "Status"
        });

        // Created by field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionCreatedBy),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Created By"
        });

        // Created when field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionCreatedWhen),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        // Idempotency key field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionIdempotencyKey),
            Visible = false,
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Idempotency Key"
        });

        // Metadata field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionMetadata),
            Visible = false,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Metadata"
        });

        // IP address field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(WalletTransactionInfo.TransactionIPAddress),
            Visible = false,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = true,
            Caption = "IP Address"
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
