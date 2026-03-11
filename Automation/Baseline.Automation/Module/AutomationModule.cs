using CMS;
using CMS.Core;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(Baseline.Automation.Module.AutomationModule))]

namespace Baseline.Automation.Module;

/// <summary>
/// Xperience module registration for the Baseline Automation Engine.
/// Handles initialization, event binding, module lifecycle,
/// and admin client module registration for embedded React templates.
/// </summary>
public class AutomationModule : AdminModule
{
    public AutomationModule() : base(BaselineAutomationConstants.ModuleName) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("baseline", "automation");

        RegisterEventHandlers();
    }

    private void RegisterEventHandlers()
    {
        // Object event handlers can be registered here to wire up
        // automation triggers to Kentico object lifecycle events.
        // Example: Contact created → evaluate form submission triggers
    }
}
