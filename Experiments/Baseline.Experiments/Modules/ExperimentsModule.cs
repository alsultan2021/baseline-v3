using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using Baseline.Experiments.Modules;
using Microsoft.Extensions.DependencyInjection;
using Baseline.Experiments.Infrastructure;

[assembly: RegisterModule(typeof(ExperimentsModule))]

namespace Baseline.Experiments.Modules;

/// <summary>
/// Module registration for Baseline.Experiments — creates DB tables on first run.
/// </summary>
public class ExperimentsModule : Module
{
    private IExperimentsModuleInstaller? _installer;

    public ExperimentsModule() : base("Baseline.Experiments") { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        _installer = parameters.Services.GetService<IExperimentsModuleInstaller>();

        ApplicationEvents.Initialized.Execute += (_, _) =>
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                try { _installer?.Install(); }
                catch { /* logged via SQL errors if any */ }
            });
        };
    }
}
