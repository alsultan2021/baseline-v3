using CMS;
using CMS.Base;
using CMS.Core;

using Baseline.AI.Admin.Installers;
using Baseline.AI.Events;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(Baseline.AI.Admin.BaselineAIAdminModule))]

namespace Baseline.AI.Admin;

/// <summary>
/// Admin module for Baseline AI configuration.
/// Follows Lucene's LuceneAdminModule pattern.
/// </summary>
internal class BaselineAIAdminModule : AdminModule
{
    private BaselineAIModuleInstaller installer = null!;
    private AIContentEventHandler? eventHandler;

    public BaselineAIAdminModule()
        : base(nameof(BaselineAIAdminModule))
    {
    }

    /// <inheritdoc/>
    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("baseline", "ai-admin");

        var services = parameters.Services;

        installer = services.GetRequiredService<BaselineAIModuleInstaller>();
        eventHandler = services.GetService<AIContentEventHandler>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
    }

    private void InitializeModule(object? sender, EventArgs e)
    {
        installer.Install();
        eventHandler?.Register();
    }
}
