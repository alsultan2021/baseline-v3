using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.AI.Data.AIKnowledgeBasePathInfo), Baseline.AI.Data.AIKnowledgeBasePathInfo.OBJECT_TYPE)]

namespace Baseline.AI.Data;

/// <summary>
/// Path configuration for AI Knowledge Base - defines which paths to include/exclude per channel.
/// </summary>
public class AIKnowledgeBasePathInfo : AbstractInfo<AIKnowledgeBasePathInfo, IInfoProvider<AIKnowledgeBasePathInfo>>
{
    public const string OBJECT_TYPE = "baselineai.knowledgebasepath";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<AIKnowledgeBasePathInfo>),
        OBJECT_TYPE,
        "BaselineAI.KnowledgeBasePath",
        nameof(PathId),
        nameof(PathLastModified),
        nameof(PathGuid),
        null,
        nameof(PathIncludePattern),
        null,
        nameof(PathKnowledgeBaseId),
        AIKnowledgeBaseInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = true,
        ContinuousIntegrationSettings = { Enabled = true },
        DependsOn =
        [
            new ObjectDependency(nameof(PathKnowledgeBaseId), AIKnowledgeBaseInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    public AIKnowledgeBasePathInfo() : base(TYPEINFO) { }
    public AIKnowledgeBasePathInfo(DataRow dr) : base(TYPEINFO, dr) { }

    protected override void SetObject()
    {
        // Ensure GUID is generated for new objects
        if (PathGuid == Guid.Empty)
        {
            PathGuid = Guid.NewGuid();
        }

        base.SetObject();
    }

    protected override void DeleteObject() => Provider.Delete(this);

    #region Identity

    [DatabaseField]
    public virtual int PathId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PathId)), 0);
        set => SetValue(nameof(PathId), value);
    }

    [DatabaseField]
    public virtual Guid PathGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(PathGuid)), Guid.Empty);
        set => SetValue(nameof(PathGuid), value);
    }

    [DatabaseField]
    public virtual DateTime PathLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(PathLastModified)), DateTime.MinValue);
        set => SetValue(nameof(PathLastModified), value);
    }

    [DatabaseField]
    public virtual int PathKnowledgeBaseId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PathKnowledgeBaseId)), 0);
        set => SetValue(nameof(PathKnowledgeBaseId), value);
    }

    #endregion

    #region Channel

    [DatabaseField]
    public virtual int PathChannelId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PathChannelId)), 0);
        set => SetValue(nameof(PathChannelId), value);
    }

    [DatabaseField]
    public virtual string? PathChannelName
    {
        get => ValidationHelper.GetString(GetValue(nameof(PathChannelName)), null);
        set => SetValue(nameof(PathChannelName), value);
    }

    #endregion

    #region Path Patterns

    [DatabaseField]
    public virtual string PathIncludePattern
    {
        get => ValidationHelper.GetString(GetValue(nameof(PathIncludePattern)), string.Empty);
        set => SetValue(nameof(PathIncludePattern), value);
    }

    [DatabaseField]
    public virtual string? PathExcludePattern
    {
        get => ValidationHelper.GetString(GetValue(nameof(PathExcludePattern)), null);
        set => SetValue(nameof(PathExcludePattern), value);
    }

    [DatabaseField]
    public virtual int PathPriority
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PathPriority)), 0);
        set => SetValue(nameof(PathPriority), value);
    }

    [DatabaseField]
    public virtual bool PathIncludeChildren
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(PathIncludeChildren)), true);
        set => SetValue(nameof(PathIncludeChildren), value);
    }

    #endregion

    #region Content Types

    [DatabaseField]
    public virtual string PathContentTypes
    {
        get => ValidationHelper.GetString(GetValue(nameof(PathContentTypes)), "[]");
        set => SetValue(nameof(PathContentTypes), value);
    }

    #endregion
}
