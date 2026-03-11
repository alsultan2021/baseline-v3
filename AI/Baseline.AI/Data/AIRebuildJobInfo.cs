using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.AI.Data.AIRebuildJobInfo), Baseline.AI.Data.AIRebuildJobInfo.OBJECT_TYPE)]

namespace Baseline.AI.Data;

/// <summary>
/// Rebuild job progress tracking for background rebuild operations.
/// </summary>
public class AIRebuildJobInfo : AbstractInfo<AIRebuildJobInfo, IInfoProvider<AIRebuildJobInfo>>
{
    public const string OBJECT_TYPE = "baselineai.rebuildjob";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<AIRebuildJobInfo>),
        OBJECT_TYPE,
        "BaselineAI.RebuildJob",
        nameof(JobId),
        null, null, null, null, null, null,
        nameof(JobKnowledgeBaseId))
    {
        TouchCacheDependencies = false,
        ContinuousIntegrationSettings = { Enabled = false }, // Runtime data
        DependsOn =
        [
            new ObjectDependency(nameof(JobKnowledgeBaseId), AIKnowledgeBaseInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    public AIRebuildJobInfo() : base(TYPEINFO) { }
    public AIRebuildJobInfo(DataRow dr) : base(TYPEINFO, dr) { }

    [DatabaseField]
    public virtual int JobId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobId)), 0);
        set => SetValue(nameof(JobId), value);
    }

    [DatabaseField]
    public virtual int JobKnowledgeBaseId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobKnowledgeBaseId)), 0);
        set => SetValue(nameof(JobKnowledgeBaseId), value);
    }

    [DatabaseField]
    public virtual int JobStatus
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobStatus)), 0);
        set => SetValue(nameof(JobStatus), value);
    }

    #region Progress Counters

    [DatabaseField]
    public virtual int JobScannedCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobScannedCount)), 0);
        set => SetValue(nameof(JobScannedCount), value);
    }

    [DatabaseField]
    public virtual int JobTotalCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobTotalCount)), 0);
        set => SetValue(nameof(JobTotalCount), value);
    }

    [DatabaseField]
    public virtual int JobChunkCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobChunkCount)), 0);
        set => SetValue(nameof(JobChunkCount), value);
    }

    [DatabaseField]
    public virtual int JobEmbeddedCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobEmbeddedCount)), 0);
        set => SetValue(nameof(JobEmbeddedCount), value);
    }

    [DatabaseField]
    public virtual int JobFailedCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(JobFailedCount)), 0);
        set => SetValue(nameof(JobFailedCount), value);
    }

    #endregion

    #region Timing

    [DatabaseField]
    public virtual DateTime JobStarted
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(JobStarted)), DateTime.UtcNow);
        set => SetValue(nameof(JobStarted), value);
    }

    [DatabaseField]
    public virtual DateTime? JobFinished
    {
        get
        {
            var val = GetValue(nameof(JobFinished));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(JobFinished), value);
    }

    #endregion

    #region Error Tracking

    [DatabaseField]
    public virtual string? JobLastError
    {
        get => ValidationHelper.GetString(GetValue(nameof(JobLastError)), null);
        set => SetValue(nameof(JobLastError), value);
    }

    /// <summary>
    /// JSON array of failed content item GUIDs.
    /// </summary>
    [DatabaseField]
    public virtual string? JobFailedItems
    {
        get => ValidationHelper.GetString(GetValue(nameof(JobFailedItems)), null);
        set => SetValue(nameof(JobFailedItems), value);
    }

    #endregion
}

/// <summary>
/// Rebuild job status.
/// </summary>
public enum RebuildJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
