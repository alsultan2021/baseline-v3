using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Ecommerce Fulfillment Type module installer.
/// </summary>
public interface IFulfillmentTypeModuleInstaller
{
    /// <summary>
    /// Installs the FulfillmentType data class and seeds default fulfillment types.
    /// </summary>
    void Install();
}

/// <summary>
/// Installs the FulfillmentType database table and seeds default fulfillment types.
/// </summary>
public class FulfillmentTypeModuleInstaller : IFulfillmentTypeModuleInstaller
{
    private readonly IInfoProvider<FulfillmentTypeInfo> _fulfillmentTypeProvider;
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider;
    private readonly IEventLogService _eventLogService;

    public FulfillmentTypeModuleInstaller(
        IInfoProvider<FulfillmentTypeInfo> fulfillmentTypeProvider,
        IInfoProvider<ResourceInfo> resourceInfoProvider,
        IEventLogService eventLogService)
    {
        _fulfillmentTypeProvider = fulfillmentTypeProvider;
        _resourceInfoProvider = resourceInfoProvider;
        _eventLogService = eventLogService;
    }

    /// <summary>
    /// Installs the FulfillmentType table and seeds default data.
    /// </summary>
    public void Install()
    {
        InstallFulfillmentTypeClass();
        SeedDefaultFulfillmentTypes();
    }

    private void InstallFulfillmentTypeClass()
    {
        // Check using the class name (Baseline.FulfillmentType), not the object type
        const string className = "Baseline.FulfillmentType";
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassDisplayName = "Fulfillment Type";
        info.ClassTableName = "Baseline_FulfillmentType";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = GetResourceId();

        var formInfo = BuildFormDefinition();
        info.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static FormInfo BuildFormDefinition()
    {
        var formInfo = new FormInfo();

        // FulfillmentTypeID
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeID),
            DataType = FieldDataType.Integer,
            PrimaryKey = true,
            AllowEmpty = false,
        });

        // FulfillmentTypeGUID
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeGUID),
            DataType = FieldDataType.Guid,
            AllowEmpty = false,
            System = true,
        });

        // FulfillmentTypeCodeName
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeCodeName),
            DataType = FieldDataType.Text,
            Size = 100,
            AllowEmpty = false,
            Caption = "Code Name",
        });

        // FulfillmentTypeDisplayName
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeDisplayName),
            DataType = FieldDataType.Text,
            Size = 200,
            AllowEmpty = false,
            Caption = "Display Name",
        });

        // FulfillmentTypeDescription
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeDescription),
            DataType = FieldDataType.LongText,
            AllowEmpty = true,
            Caption = "Description",
        });

        // FulfillmentTypeRequiresShipping
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeRequiresShipping),
            DataType = FieldDataType.Boolean,
            AllowEmpty = false,
            DefaultValue = "false",
            Caption = "Requires Shipping",
        });

        // FulfillmentTypeRequiresBillingAddress
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeRequiresBillingAddress),
            DataType = FieldDataType.Boolean,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Requires Billing Address",
        });

        // FulfillmentTypeSupportsDeliveryOptions
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeSupportsDeliveryOptions),
            DataType = FieldDataType.Boolean,
            AllowEmpty = false,
            DefaultValue = "false",
            Caption = "Supports Delivery Options",
        });

        // FulfillmentTypeIsEnabled
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeIsEnabled),
            DataType = FieldDataType.Boolean,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Is Enabled",
        });

        // FulfillmentTypeLastModified
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(FulfillmentTypeInfo.FulfillmentTypeLastModified),
            DataType = FieldDataType.DateTime,
            AllowEmpty = false,
            System = true,
        });

        return formInfo;
    }

    private void SeedDefaultFulfillmentTypes()
    {
        // Only seed if table is empty
        var existing = _fulfillmentTypeProvider.Get().TopN(1).GetEnumerableTypedResult();
        if (existing.Any())
        {
            return;
        }

        // Seed default fulfillment types
        var defaults = new[]
        {
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "Physical",
                FulfillmentTypeDisplayName = "Physical Product",
                FulfillmentTypeDescription = "Physical products that require shipping",
                FulfillmentTypeRequiresShipping = true,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = true,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            },
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "Digital",
                FulfillmentTypeDisplayName = "Digital Product",
                FulfillmentTypeDescription = "Digital downloads (software, media, etc.) - no shipping required",
                FulfillmentTypeRequiresShipping = false,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = false,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            },
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "Ticket",
                FulfillmentTypeDisplayName = "Event Ticket",
                FulfillmentTypeDescription = "Digital tickets that are generated and downloaded",
                FulfillmentTypeRequiresShipping = false,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = false,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            },
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "Food",
                FulfillmentTypeDisplayName = "Food Order",
                FulfillmentTypeDescription = "Food orders for pickup or delivery",
                FulfillmentTypeRequiresShipping = false,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = true,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            },
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "Service",
                FulfillmentTypeDisplayName = "Service/Contract",
                FulfillmentTypeDescription = "Service contracts or deposits - no physical fulfillment",
                FulfillmentTypeRequiresShipping = false,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = false,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            },
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "GiftCard",
                FulfillmentTypeDisplayName = "Gift Card",
                FulfillmentTypeDescription = "Gift cards sent electronically or printed",
                FulfillmentTypeRequiresShipping = false,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = false,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            },
            new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeCodeName = "Subscription",
                FulfillmentTypeDisplayName = "Subscription",
                FulfillmentTypeDescription = "Subscription-based products with recurring billing",
                FulfillmentTypeRequiresShipping = false,
                FulfillmentTypeRequiresBillingAddress = true,
                FulfillmentTypeSupportsDeliveryOptions = false,
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeLastModified = DateTime.UtcNow
            }
        };

        foreach (var fulfillmentType in defaults)
        {
            _fulfillmentTypeProvider.Set(fulfillmentType);
        }
    }

    private int GetResourceId()
    {
        // Use simple Get by name - Kentico pattern
        var resource = _resourceInfoProvider.Get("CMS.Ecommerce");
        return resource?.ResourceID ?? 0;
    }
}
