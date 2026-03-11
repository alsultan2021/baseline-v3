using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;

using Baseline.Localization.Events;
using Baseline.Localization.Infrastructure;
using Baseline.Localization.Modules;
using Baseline.Localization.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: RegisterModule(typeof(BaselineLocalizationModule))]

namespace Baseline.Localization.Modules;

/// <summary>
/// XbK module for Baseline.Localization — installs DB tables,
/// registers translation workflow event handlers, and seeds initial coverage data.
/// </summary>
public class BaselineLocalizationModule : Module
{
    private TranslationWorkflowEventHandler? _translationHandler;

    public BaselineLocalizationModule() : base("Baseline.Localization") { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var sp = parameters.Services;

        ApplicationEvents.Initialized.Execute += (_, _) =>
        {
            // Install translation coverage table
            try
            {
                sp.GetService<ITranslationCoverageInstaller>()?.Install();
            }
            catch { /* table may already exist */ }

            // Register translation workflow event handler
            try
            {
                var logger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger<TranslationWorkflowEventHandler>();

                _translationHandler = new TranslationWorkflowEventHandler(sp, logger);
                _translationHandler.Register();
            }
            catch { /* localization module is optional */ }

            // Seed initial coverage data after a short delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                try
                {
                    using var scope = sp.CreateScope();
                    var coverage = scope.ServiceProvider.GetService<ITranslationCoverageService>();
                    if (coverage is not null)
                    {
                        await coverage.RefreshCoverageAsync();
                    }
                }
                catch { /* non-critical */ }
            });
        };
    }
}
