using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for the Currency module installer.
/// </summary>
public interface ICurrencyModuleInstaller
{
    /// <summary>
    /// Installs the currency module, including Currency and CurrencyExchangeRate data classes.
    /// </summary>
    void Install();
}

/// <summary>
/// Installer for Currency and CurrencyExchangeRate data classes.
/// Creates the data classes and seeds default currencies.
/// </summary>
public class CurrencyModuleInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    IEventLogService eventLogService) : ICurrencyModuleInstaller
{
    private readonly IInfoProvider<ResourceInfo> _resourceInfoProvider = resourceInfoProvider;
    private readonly IEventLogService _eventLogService = eventLogService;

    /// <summary>
    /// Installs the Currency module components.
    /// </summary>
    public void Install()
    {
        try
        {
            var resourceInfo = GetOrCreateModule();

            InstallCurrencyDataClass(resourceInfo);
            InstallCurrencyExchangeRateDataClass(resourceInfo);

            TrySeedDefaultCurrencies();
        }
        catch (Exception ex)
        {
            _eventLogService.LogException(
                nameof(CurrencyModuleInstaller),
                "INSTALL_ERROR",
                ex,
                "Failed to install Currency module.");
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
    /// Creates or updates the Currency data class.
    /// </summary>
    private static void InstallCurrencyDataClass(ResourceInfo resourceInfo)
    {
        var className = CurrencyInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = CurrencyInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = CurrencyInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Currency";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(CurrencyInfo.CurrencyID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // Currency code field (ISO 4217)
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyCode),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 3,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Currency Code (ISO 4217)"
        });

        // Display name field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyDisplayName),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Display Name"
        });

        // Symbol field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencySymbol),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 10,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Symbol"
        });

        // Decimal places field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyDecimalPlaces),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "2",
            Caption = "Decimal Places"
        });

        // Format pattern field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyFormatPattern),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "{0}{1}",
            Caption = "Format Pattern"
        });

        // Is default field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyIsDefault),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "false",
            Caption = "Is Default Currency"
        });

        // Enabled field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyEnabled),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Enabled"
        });

        // Order field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyOrder),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "0",
            Caption = "Display Order"
        });

        // Last modified field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyInfo.CurrencyLastModified),
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
    /// Creates or updates the CurrencyExchangeRate data class.
    /// </summary>
    private static void InstallCurrencyExchangeRateDataClass(ResourceInfo resourceInfo)
    {
        var className = CurrencyExchangeRateInfo.TYPEINFO.ObjectClassName;
        var info = DataClassInfoProvider.GetDataClassInfo(className);
        if (info != null)
        {
            return; // Already installed
        }

        info = DataClassInfo.New(className);
        info.ClassName = CurrencyExchangeRateInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = CurrencyExchangeRateInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Currency Exchange Rate";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(CurrencyExchangeRateInfo.ExchangeRateID));

        // GUID field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateGuid),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false
        });

        // From currency field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateFromCurrencyID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            Caption = "From Currency"
        });

        // To currency field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateToCurrencyID),
            Visible = true,
            DataType = FieldDataType.Integer,
            Enabled = true,
            AllowEmpty = false,
            Caption = "To Currency"
        });

        // Exchange rate value field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateValue),
            Visible = true,
            DataType = FieldDataType.Decimal,
            Precision = 10,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "1",
            Caption = "Exchange Rate"
        });

        // Valid from field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateValidFrom),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = false,
            Caption = "Valid From"
        });

        // Valid to field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateValidTo),
            Visible = true,
            DataType = FieldDataType.DateTime,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Valid To"
        });

        // Enabled field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateEnabled),
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            AllowEmpty = false,
            DefaultValue = "true",
            Caption = "Enabled"
        });

        // Source field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateSource),
            Visible = true,
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            AllowEmpty = true,
            Caption = "Source"
        });

        // Last modified field
        AddFormField(formInfo, new FormFieldInfo
        {
            Name = nameof(CurrencyExchangeRateInfo.ExchangeRateLastModified),
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
    /// Attempts to seed default currencies if the provider is available.
    /// </summary>
    private void TrySeedDefaultCurrencies()
    {
        try
        {
            var provider = Service.Resolve<IInfoProvider<CurrencyInfo>>();
            if (provider != null)
            {
                InstallDefaultCurrencies();
            }
        }
        catch (ServiceResolutionException)
        {
            _eventLogService.LogInformation(
                source: nameof(CurrencyModuleInstaller),
                eventCode: "INSTALL_INFO",
                eventDescription: "Currency module installed. Default currencies will be seeded on next application restart or can be created via Admin UI.");
        }
    }

    /// <summary>
    /// Installs default currencies.
    /// </summary>
    private static void InstallDefaultCurrencies()
    {
        // Note: CurrencyIsDefault is set to false for all currencies.
        // Default currency is now configured per-channel via CommerceChannelSettings.

        // US Dollar
        EnsureCurrency(new CurrencyInfo
        {
            CurrencyGuid = new Guid("C0000001-0000-0000-0000-000000000001"),
            CurrencyCode = "USD",
            CurrencyDisplayName = "US Dollar",
            CurrencySymbol = "$",
            CurrencyDecimalPlaces = 2,
            CurrencyFormatPattern = "{0}{1}",
            CurrencyIsDefault = false,
            CurrencyEnabled = true,
            CurrencyOrder = 1
        });

        // Euro
        EnsureCurrency(new CurrencyInfo
        {
            CurrencyGuid = new Guid("C0000001-0000-0000-0000-000000000002"),
            CurrencyCode = "EUR",
            CurrencyDisplayName = "Euro",
            CurrencySymbol = "€",
            CurrencyDecimalPlaces = 2,
            CurrencyFormatPattern = "{1} {0}",
            CurrencyIsDefault = false,
            CurrencyEnabled = true,
            CurrencyOrder = 2
        });

        // British Pound
        EnsureCurrency(new CurrencyInfo
        {
            CurrencyGuid = new Guid("C0000001-0000-0000-0000-000000000003"),
            CurrencyCode = "GBP",
            CurrencyDisplayName = "British Pound",
            CurrencySymbol = "£",
            CurrencyDecimalPlaces = 2,
            CurrencyFormatPattern = "{0}{1}",
            CurrencyIsDefault = false,
            CurrencyEnabled = true,
            CurrencyOrder = 3
        });

        // Canadian Dollar
        EnsureCurrency(new CurrencyInfo
        {
            CurrencyGuid = new Guid("C0000001-0000-0000-0000-000000000004"),
            CurrencyCode = "CAD",
            CurrencyDisplayName = "Canadian Dollar",
            CurrencySymbol = "$",
            CurrencyDecimalPlaces = 2,
            CurrencyFormatPattern = "{0}{1}",
            CurrencyIsDefault = false,
            CurrencyEnabled = true,
            CurrencyOrder = 4
        });

        // Japanese Yen
        EnsureCurrency(new CurrencyInfo
        {
            CurrencyGuid = new Guid("C0000001-0000-0000-0000-000000000005"),
            CurrencyCode = "JPY",
            CurrencyDisplayName = "Japanese Yen",
            CurrencySymbol = "¥",
            CurrencyDecimalPlaces = 0,
            CurrencyFormatPattern = "{0}{1}",
            CurrencyIsDefault = false,
            CurrencyEnabled = true,
            CurrencyOrder = 5
        });

        // Swiss Franc
        EnsureCurrency(new CurrencyInfo
        {
            CurrencyGuid = new Guid("C0000001-0000-0000-0000-000000000006"),
            CurrencyCode = "CHF",
            CurrencyDisplayName = "Swiss Franc",
            CurrencySymbol = "CHF",
            CurrencyDecimalPlaces = 2,
            CurrencyFormatPattern = "{0} {1}",
            CurrencyIsDefault = false,
            CurrencyEnabled = true,
            CurrencyOrder = 6
        });
    }

    /// <summary>
    /// Ensures a currency exists with the given configuration.
    /// </summary>
    private static void EnsureCurrency(CurrencyInfo currency)
    {
        var provider = Service.Resolve<IInfoProvider<CurrencyInfo>>();

        // Use Get by code name - Kentico recommended pattern
        var existing = provider.Get(currency.CurrencyCode);

        if (existing == null)
        {
            currency.CurrencyLastModified = DateTime.Now;
            provider.Set(currency);
        }
    }

    private static void AddFormField(FormInfo formInfo, FormFieldInfo field)
    {
        formInfo.AddFormItem(field);
    }

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
