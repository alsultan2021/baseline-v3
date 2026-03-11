using CMS;
using CMS.Base;
using CMS.Core;

using Baseline.Ecommerce.Installers;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(Baseline.Ecommerce.Admin.BaselineEcommerceAdminModule))]

namespace Baseline.Ecommerce.Admin;

/// <summary>
/// Admin module for Baseline Ecommerce configuration.
/// Extends the built-in Kentico Commerce configuration with Tax Class, Currency, Promotion, and Stock management.
/// </summary>
public class BaselineEcommerceAdminModule : AdminModule
{
    /// <summary>
    /// Module name identifier.
    /// </summary>
    public const string MODULE_NAME = "XperienceCommunity.Baseline.Ecommerce.Admin";

    private ITaxModuleInstaller? _taxInstaller;
    private ICurrencyModuleInstaller? _currencyInstaller;
    private IWalletModuleInstaller? _walletInstaller;
    private IGiftCardModuleInstaller? _giftCardInstaller;
    private IProductStockModuleInstaller? _stockInstaller;

    /// <summary>
    /// Creates a new instance of <see cref="BaselineEcommerceAdminModule"/>.
    /// </summary>
    public BaselineEcommerceAdminModule()
        : base(MODULE_NAME)
    {
    }

    /// <inheritdoc/>
    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        // Get installers from DI container
        var services = parameters.Services;
        _taxInstaller = services.GetService<ITaxModuleInstaller>();
        _currencyInstaller = services.GetService<ICurrencyModuleInstaller>();
        _walletInstaller = services.GetService<IWalletModuleInstaller>();
        _giftCardInstaller = services.GetService<IGiftCardModuleInstaller>();
        _stockInstaller = services.GetService<IProductStockModuleInstaller>();

        // Installers are called from StartingSiteModule.RunInstallers
        // to avoid duplicate/concurrent schema modifications.
    }
}
