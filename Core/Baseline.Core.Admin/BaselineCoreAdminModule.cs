using CMS;
using CMS.Core;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(Baseline.Core.Admin.BaselineCoreAdminModule))]

namespace Baseline.Core.Admin;

/// <summary>
/// Admin module for Baseline Core — registers the client module
/// so the embedded React templates (automation builder, statistics)
/// can be resolved at runtime.
/// </summary>
internal class BaselineCoreAdminModule : AdminModule
{
    public BaselineCoreAdminModule()
        : base(nameof(BaselineCoreAdminModule))
    {
    }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("baseline", "core-admin");
    }
}
