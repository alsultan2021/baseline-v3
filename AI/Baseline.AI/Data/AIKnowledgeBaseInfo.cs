using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.AI.Data.AIKnowledgeBaseInfo), Baseline.AI.Data.AIKnowledgeBaseInfo.OBJECT_TYPE)]

namespace Baseline.AI.Data;

/// <summary>
/// AI Knowledge Base configuration - stores which content to index for AI chatbot.
/// Analogous to Lucene's LuceneIndexInfo.
/// </summary>
public class AIKnowledgeBaseInfo : AbstractInfo<AIKnowledgeBaseInfo, IInfoProvider<AIKnowledgeBaseInfo>>
{
    public const string OBJECT_TYPE = "baselineai.knowledgebase";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<AIKnowledgeBaseInfo>),
        OBJECT_TYPE,
        "BaselineAI.KnowledgeBase",
        nameof(KnowledgeBaseId),
        nameof(KnowledgeBaseLastModified),
        nameof(KnowledgeBaseGuid),
        nameof(KnowledgeBaseName),
        nameof(KnowledgeBaseDisplayName),
        null, null, null)
    {
        TouchCacheDependencies = true,
        ContinuousIntegrationSettings = { Enabled = true }
    };

    public AIKnowledgeBaseInfo() : base(TYPEINFO) { }
    public AIKnowledgeBaseInfo(DataRow dr) : base(TYPEINFO, dr) { }

    protected override void SetObject()
    {
        // Ensure GUID is generated for new objects
        if (KnowledgeBaseGuid == Guid.Empty)
        {
            KnowledgeBaseGuid = Guid.NewGuid();
        }

        base.SetObject();
    }

    protected override void DeleteObject() => Provider.Delete(this);

    #region Identity

    [DatabaseField]
    public virtual int KnowledgeBaseId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(KnowledgeBaseId)), 0);
        set => SetValue(nameof(KnowledgeBaseId), value);
    }

    [DatabaseField]
    public virtual Guid KnowledgeBaseGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(KnowledgeBaseGuid)), Guid.Empty);
        set => SetValue(nameof(KnowledgeBaseGuid), value);
    }

    [DatabaseField]
    public virtual string KnowledgeBaseName
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseName)), string.Empty);
        set => SetValue(nameof(KnowledgeBaseName), value);
    }

    [DatabaseField]
    public virtual string KnowledgeBaseDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseDisplayName)), string.Empty);
        set => SetValue(nameof(KnowledgeBaseDisplayName), value);
    }

    [DatabaseField]
    public virtual DateTime KnowledgeBaseLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(KnowledgeBaseLastModified)), DateTime.MinValue);
        set => SetValue(nameof(KnowledgeBaseLastModified), value);
    }

    #endregion

    #region Strategy

    [DatabaseField]
    public virtual string KnowledgeBaseStrategyName
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseStrategyName)), string.Empty);
        set => SetValue(nameof(KnowledgeBaseStrategyName), value);
    }

    [DatabaseField]
    public virtual string? KnowledgeBaseStrategyHash
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseStrategyHash)), null);
        set => SetValue(nameof(KnowledgeBaseStrategyHash), value);
    }

    #endregion

    #region Content Selection (JSON arrays)

    [DatabaseField]
    public virtual string KnowledgeBaseLanguages
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseLanguages)), "[]");
        set => SetValue(nameof(KnowledgeBaseLanguages), value);
    }

    [DatabaseField]
    public virtual string KnowledgeBaseChannels
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseChannels)), "[]");
        set => SetValue(nameof(KnowledgeBaseChannels), value);
    }

    [DatabaseField]
    public virtual string KnowledgeBaseReusableTypes
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseReusableTypes)), "[]");
        set => SetValue(nameof(KnowledgeBaseReusableTypes), value);
    }

    #endregion

    #region Operational Status

    [DatabaseField]
    public virtual int KnowledgeBaseStatus
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(KnowledgeBaseStatus)), 0);
        set => SetValue(nameof(KnowledgeBaseStatus), value);
    }

    [DatabaseField]
    public virtual string? KnowledgeBaseLastError
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseLastError)), null);
        set => SetValue(nameof(KnowledgeBaseLastError), value);
    }

    [DatabaseField]
    public virtual DateTime? KnowledgeBaseLastRebuild
    {
        get
        {
            var val = GetValue(nameof(KnowledgeBaseLastRebuild));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(KnowledgeBaseLastRebuild), value);
    }

    [DatabaseField]
    public virtual long? KnowledgeBaseLastRunDurationMs
    {
        get
        {
            var val = GetValue(nameof(KnowledgeBaseLastRunDurationMs));
            return val is long l ? l : (val is int i ? (long)i : null);
        }
        set => SetValue(nameof(KnowledgeBaseLastRunDurationMs), value);
    }

    [DatabaseField]
    public virtual int KnowledgeBaseDocumentCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(KnowledgeBaseDocumentCount)), 0);
        set => SetValue(nameof(KnowledgeBaseDocumentCount), value);
    }

    [DatabaseField]
    public virtual int KnowledgeBaseChunkCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(KnowledgeBaseChunkCount)), 0);
        set => SetValue(nameof(KnowledgeBaseChunkCount), value);
    }

    #endregion

    #region Configuration

    [DatabaseField]
    public virtual int KnowledgeBaseSchemaVersion
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(KnowledgeBaseSchemaVersion)), 1);
        set => SetValue(nameof(KnowledgeBaseSchemaVersion), value);
    }

    [DatabaseField]
    public virtual int KnowledgeBaseDefaultTopK
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(KnowledgeBaseDefaultTopK)), 5);
        set => SetValue(nameof(KnowledgeBaseDefaultTopK), value);
    }

    [DatabaseField]
    public virtual decimal KnowledgeBaseDefaultThreshold
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(KnowledgeBaseDefaultThreshold)), 0.7m);
        set => SetValue(nameof(KnowledgeBaseDefaultThreshold), value);
    }

    [DatabaseField]
    public virtual decimal KnowledgeBaseDefaultTemperature
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(KnowledgeBaseDefaultTemperature)), 0.7m);
        set => SetValue(nameof(KnowledgeBaseDefaultTemperature), value);
    }

    #endregion

    #region Automation

    [DatabaseField]
    public virtual string? KnowledgeBaseRebuildWebhook
    {
        get => ValidationHelper.GetString(GetValue(nameof(KnowledgeBaseRebuildWebhook)), null);
        set => SetValue(nameof(KnowledgeBaseRebuildWebhook), value);
    }

    [DatabaseField]
    public virtual bool KnowledgeBaseAutoRebuildOnPublish
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(KnowledgeBaseAutoRebuildOnPublish)), true);
        set => SetValue(nameof(KnowledgeBaseAutoRebuildOnPublish), value);
    }

    #endregion
}

/// <summary>
/// Knowledge base operational status.
/// </summary>
public enum KnowledgeBaseStatus
{
    Idle = 0,
    Rebuilding = 1,
    Failed = 2,
    PartiallyBuilt = 3
}
