using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Ecommerce Subscription module installer.
/// </summary>
public interface ISubscriptionModuleInstaller
{
    /// <summary>
    /// Installs the subscription module, including the SubscriptionPlan and CustomerSubscription data classes.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for the Ecommerce Subscription module.
/// Creates the SubscriptionPlan and CustomerSubscription data classes.
/// </summary>
public class SubscriptionModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : ISubscriptionModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider = resourceInfoProvider;
    private readonly IEventLogService _eventLogService = eventLogService;

    /// <summary>
    /// Installs the Subscription module components.
    /// </summary>
    public void Install()
    {
        try
        {
            var resourceInfo = InstallModule();
            InstallSubscriptionPlanDataClass(resourceInfo);
            InstallCustomerSubscriptionDataClass(resourceInfo);
        }
        catch (Exception ex)
        {
            _eventLogService.LogException(
                nameof(SubscriptionModuleInstaller),
                "INSTALL_ERROR",
                ex,
                "Failed to install Subscription module.");
            throw;
        }
    }

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
    /// Creates or updates the SubscriptionPlan data class.
    /// </summary>
    private static void InstallSubscriptionPlanDataClass(ResourceInfo resourceInfo)
    {
        var className = SubscriptionPlanInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = className.Replace(".", "_");
        info.ClassDisplayName = "Subscription Plan";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(SubscriptionPlanInfo.SubscriptionPlanInfoID));

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.SubscriptionPlanGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.PlanCode),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.Name),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.Description),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.Price),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 4,
            Size = 18,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.Currency),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 10,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "USD"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.BillingInterval),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "Monthly"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.IntervalCount),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "1"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.TrialPeriodDays),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.TierLevel),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.IsFeatured),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.IsActive),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(SubscriptionPlanInfo.ExternalPlanId),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            AllowEmpty = true
        });

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    /// <summary>
    /// Creates or updates the CustomerSubscription data class.
    /// </summary>
    private static void InstallCustomerSubscriptionDataClass(ResourceInfo resourceInfo)
    {
        var className = CustomerSubscriptionInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = className;
        info.ClassTableName = className.Replace(".", "_");
        info.ClassDisplayName = "Customer Subscription";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID));

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.SubscriptionGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CustomerId),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.PlanId),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.Status),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "Active"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.StartDate),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CurrentPeriodEnd),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.TrialEnd),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CancelledAt),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CancelAt),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CancelAtPeriodEnd),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CancellationReason),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.PausedAt),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.ResumeAt),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.PauseReason),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.ExternalSubscriptionId),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CouponCode),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            AllowEmpty = true
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.CreatedOn),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false
        });

        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CustomerSubscriptionInfo.ModifiedOn),
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true
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
