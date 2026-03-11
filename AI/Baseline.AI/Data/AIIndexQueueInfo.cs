using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.AI.Data.AIIndexQueueInfo), Baseline.AI.Data.AIIndexQueueInfo.OBJECT_TYPE)]

namespace Baseline.AI.Data;

/// <summary>
/// Queue for incremental AI index updates.
/// </summary>
public class AIIndexQueueInfo : AbstractInfo<AIIndexQueueInfo, IInfoProvider<AIIndexQueueInfo>>
{
    public const string OBJECT_TYPE = "baselineai.indexqueue";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<AIIndexQueueInfo>),
        OBJECT_TYPE,
        "BaselineAI.IndexQueue",
        nameof(QueueId),
        null, // No LastModified tracking
        null, // No GUID
        null, null, null, null,
        nameof(QueueKnowledgeBaseId))
    {
        TouchCacheDependencies = false, // Queue items are transient
        ContinuousIntegrationSettings = { Enabled = false },
        DependsOn =
        [
            new ObjectDependency(nameof(QueueKnowledgeBaseId), AIKnowledgeBaseInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    public AIIndexQueueInfo() : base(TYPEINFO) { }
    public AIIndexQueueInfo(DataRow dr) : base(TYPEINFO, dr) { }

    [DatabaseField]
    public virtual int QueueId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(QueueId)), 0);
        set => SetValue(nameof(QueueId), value);
    }

    [DatabaseField]
    public virtual int QueueKnowledgeBaseId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(QueueKnowledgeBaseId)), 0);
        set => SetValue(nameof(QueueKnowledgeBaseId), value);
    }

    [DatabaseField]
    public virtual Guid QueueContentItemGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(QueueContentItemGuid)), Guid.Empty);
        set => SetValue(nameof(QueueContentItemGuid), value);
    }

    [DatabaseField]
    public virtual string QueueLanguageCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(QueueLanguageCode)), string.Empty);
        set => SetValue(nameof(QueueLanguageCode), value);
    }

    /// <summary>
    /// ChannelId: 0 = reusable, > 0 = page, null = all channels
    /// </summary>
    [DatabaseField]
    public virtual int? QueueChannelId
    {
        get
        {
            var val = GetValue(nameof(QueueChannelId));
            return val is int i ? i : null;
        }
        set => SetValue(nameof(QueueChannelId), value);
    }

    [DatabaseField]
    public virtual int QueueOperationType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(QueueOperationType)), 0);
        set => SetValue(nameof(QueueOperationType), value);
    }

    [DatabaseField]
    public virtual DateTime QueueCreated
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(QueueCreated)), DateTime.UtcNow);
        set => SetValue(nameof(QueueCreated), value);
    }

    [DatabaseField]
    public virtual int QueueRetryCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(QueueRetryCount)), 0);
        set => SetValue(nameof(QueueRetryCount), value);
    }

    [DatabaseField]
    public virtual string? QueueLastError
    {
        get => ValidationHelper.GetString(GetValue(nameof(QueueLastError)), null);
        set => SetValue(nameof(QueueLastError), value);
    }
}

/// <summary>
/// Index operation types.
/// </summary>
public enum IndexOperationType
{
    /// <summary>Content published/updated - re-check and upsert if matches</summary>
    Reconcile = 0,
    /// <summary>Content deleted - remove from index</summary>
    Delete = 1
}
