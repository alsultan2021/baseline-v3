using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.Modules;
using CMS.Notifications;
using Microsoft.Extensions.Logging;

using Baseline.Account;
using Baseline.Account.Modules;

[assembly: RegisterModule(typeof(AccountModule))]

namespace Baseline.Account.Modules;

/// <summary>
/// Module for Account-related initialization including notification placeholder registration.
/// </summary>
public class AccountModule : Module
{
    public const string MODULE_NAME = "Baseline.Account";
    private const string RESOURCE_NAME = "Baseline.Account";
    private const string RESOURCE_DISPLAY_NAME = "Baseline Account";
    private const string RESOURCE_DESCRIPTION = "Authentication, security auditing, and user management for Xperience by Kentico";

    public AccountModule() : base(MODULE_NAME) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        // Register MFA verification code notification placeholders
        NotificationEmailPlaceholderConfigurationStore.Instance.TryAdd(new MfaVerificationCodePlaceholders());

        // Install database tables on application init
        ApplicationEvents.Initialized.Execute += (_, _) => InstallDatabaseClasses();
    }

    private static void InstallDatabaseClasses()
    {
        var logger = Service.Resolve<ILogger<AccountModule>>();
        try
        {
            logger.LogDebug("AccountModule: Installing database classes...");
            var resourceInfoProvider = Service.Resolve<IInfoProvider<ResourceInfo>>();
            var resourceInfo = EnsureResource(resourceInfoProvider, logger);
            LoginAuditLogInstaller.Install(resourceInfo);
            DeviceAuthorizationInstaller.Install(resourceInfo);
            TrustedDeviceInstaller.Install(resourceInfo);
            PasskeyCredentialInstaller.Install(resourceInfo);
            logger.LogDebug("AccountModule: Database classes installed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AccountModule: Failed to install database classes");
        }
    }

    private static ResourceInfo EnsureResource(IInfoProvider<ResourceInfo> resourceInfoProvider, ILogger logger)
    {
        var resource = resourceInfoProvider.Get(RESOURCE_NAME) ?? new ResourceInfo();

        resource.ResourceName = RESOURCE_NAME;
        resource.ResourceDisplayName = RESOURCE_DISPLAY_NAME;
        resource.ResourceDescription = RESOURCE_DESCRIPTION;
        resource.ResourceIsInDevelopment = false;

        if (resource.HasChanged)
        {
            logger.LogInformation("AccountModule: Creating/updating resource {ResourceName}", RESOURCE_NAME);
            resourceInfoProvider.Set(resource);
            logger.LogInformation("AccountModule: Resource created with ID {ResourceID}", resource.ResourceID);
        }

        return resource;
    }
}
