using CMS.Base;
using CMS.Core;
using CMS.Helpers;

namespace Baseline.Search.Lucene.Scaling;

/// <summary>
/// Web farm task that invalidates search-related caches on other servers.
/// Fired after index rebuilds or analytics data changes.
/// </summary>
internal sealed class SearchCacheInvalidationWebFarmTask : WebFarmTaskBase
{
    public string? CreatorName { get; set; }
    public string? CacheScope { get; set; }

    public override void ExecuteTask()
    {
        var eventLog = Service.Resolve<IEventLogService>();
        string message = $"Server {SystemContext.ServerName} invalidating search cache " +
                         $"scope '{CacheScope}' from '{CreatorName}'";
        eventLog.LogInformation("BaselineSearch", "CacheInvalidation", message);

        // Progressive cache keys follow "baseline|search|*" convention
        CacheHelper.TouchKey($"baseline|search|{CacheScope ?? "all"}");
    }
}
