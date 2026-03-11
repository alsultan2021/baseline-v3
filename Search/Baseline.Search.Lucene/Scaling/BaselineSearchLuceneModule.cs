using CMS;
using CMS.Core;
using CMS.DataEngine;

using Baseline.Search.Lucene.Scaling;

[assembly: RegisterModule(typeof(BaselineSearchLuceneModule))]

namespace Baseline.Search.Lucene.Scaling;

/// <summary>
/// Module that registers Baseline Search web farm tasks for multi-server sync.
/// </summary>
internal sealed class BaselineSearchLuceneModule : Module
{
    public const string MODULE_NAME = "Baseline.Search.Lucene";

    public BaselineSearchLuceneModule() : base(MODULE_NAME) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var webFarmService = Service.Resolve<IWebFarmService>();
        webFarmService.RegisterTask<SearchCacheInvalidationWebFarmTask>();
        webFarmService.RegisterTask<IndexGenerationSyncWebFarmTask>();
    }
}
