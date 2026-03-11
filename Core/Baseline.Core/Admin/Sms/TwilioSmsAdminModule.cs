using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;

using Microsoft.Extensions.DependencyInjection;

using Baseline.Core.Admin.Sms;
using Baseline.Core.Admin.Sms.Modules;

[assembly: RegisterModule(typeof(TwilioSmsAdminModule))]

namespace Baseline.Core.Admin.Sms;

/// <summary>
/// Admin module for Twilio SMS notification settings.
/// </summary>
public class TwilioSmsAdminModule : Module
{
    private TwilioSmsModuleInstaller? _installer;
    private TwilioNotificationExtensionInstaller? _notificationExtensionInstaller;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwilioSmsAdminModule"/> class.
    /// </summary>
    public TwilioSmsAdminModule() : base(nameof(TwilioSmsAdminModule))
    {
    }

    /// <inheritdoc />
    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var services = parameters.Services;
        _installer = services.GetService<TwilioSmsModuleInstaller>();
        _notificationExtensionInstaller = services.GetService<TwilioNotificationExtensionInstaller>();

        // Run installers after app fully initialized (Lucene/AIUN pattern)
        ApplicationEvents.Initialized.Execute += InitializeModule;
    }

    private void InitializeModule(object? sender, EventArgs e)
    {
        try
        {
            _installer?.Install();
            _notificationExtensionInstaller?.Install();
        }
        catch (Exception ex)
        {
            var eventLogService = Service.Resolve<IEventLogService>();
            eventLogService?.LogException(
                nameof(TwilioSmsAdminModule),
                "SMS_INSTALL_ERROR",
                ex,
                additionalMessage: "Failed to install Twilio SMS module.");
        }
    }
}
