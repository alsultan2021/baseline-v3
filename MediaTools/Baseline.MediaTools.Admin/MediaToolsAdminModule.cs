using Baseline.MediaTools.Admin;
using CMS;
using CMS.Core;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(MediaToolsAdminModule))]

namespace Baseline.MediaTools.Admin;

public class MediaToolsAdminModule : AdminModule
{
    public const string MODULE_NAME = "Baseline.MediaTools.Admin";

    public MediaToolsAdminModule() : base(MODULE_NAME) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit();
        RegisterClientModule("baseline", "media-tools");
    }
}
