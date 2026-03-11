using CMS.Base;
using CMS.Core;
using CMS.Helpers;

namespace Baseline.Search.Lucene.Scaling;

/// <summary>
/// Web farm task that syncs index generation metadata to other servers.
/// Fired after an index rebuild completes on the originating server.
/// </summary>
internal sealed class IndexGenerationSyncWebFarmTask : WebFarmTaskBase
{
    public string? CreatorName { get; set; }
    public string? IndexName { get; set; }
    public string? GenerationId { get; set; }
    public int DocumentCount { get; set; }
    public long IndexSizeBytes { get; set; }

    public override void ExecuteTask()
    {
        var eventLog = Service.Resolve<IEventLogService>();
        string message = $"Server {SystemContext.ServerName} received generation sync for " +
                         $"'{IndexName}' (gen {GenerationId}, {DocumentCount} docs) from '{CreatorName}'";
        eventLog.LogInformation("BaselineSearch", "GenerationSync", message);

        // Generation metadata is stored on disk — the originating server already wrote it.
        // Other servers will pick it up on next read since storage is shared (NAS / Azure Files).
        // This task exists so servers can invalidate any cached generation lists.
        CacheHelper.TouchKey($"baseline|search|generations|{IndexName}");
    }
}
