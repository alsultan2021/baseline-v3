using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.AI.Data.AIContentFingerprintInfo), Baseline.AI.Data.AIContentFingerprintInfo.OBJECT_TYPE)]

namespace Baseline.AI.Data;

/// <summary>
/// Content fingerprint for incremental no-op optimization.
/// Stores hash of extracted content to skip re-embedding when content unchanged.
/// </summary>
public class AIContentFingerprintInfo : AbstractInfo<AIContentFingerprintInfo, IInfoProvider<AIContentFingerprintInfo>>
{
    public const string OBJECT_TYPE = "baselineai.contentfingerprint";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<AIContentFingerprintInfo>),
        OBJECT_TYPE,
        "BaselineAI.ContentFingerprint",
        nameof(FingerprintId),
        nameof(FingerprintLastChecked),
        null, null, null, null, null,
        nameof(FingerprintKnowledgeBaseId))
    {
        TouchCacheDependencies = false,
        ContinuousIntegrationSettings = { Enabled = false }, // Runtime data
        DependsOn =
        [
            new ObjectDependency(nameof(FingerprintKnowledgeBaseId), AIKnowledgeBaseInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    public AIContentFingerprintInfo() : base(TYPEINFO) { }
    public AIContentFingerprintInfo(DataRow dr) : base(TYPEINFO, dr) { }

    [DatabaseField]
    public virtual int FingerprintId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(FingerprintId)), 0);
        set => SetValue(nameof(FingerprintId), value);
    }

    [DatabaseField]
    public virtual int FingerprintKnowledgeBaseId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(FingerprintKnowledgeBaseId)), 0);
        set => SetValue(nameof(FingerprintKnowledgeBaseId), value);
    }

    [DatabaseField]
    public virtual Guid FingerprintContentGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(FingerprintContentGuid)), Guid.Empty);
        set => SetValue(nameof(FingerprintContentGuid), value);
    }

    [DatabaseField]
    public virtual string FingerprintHash
    {
        get => ValidationHelper.GetString(GetValue(nameof(FingerprintHash)), string.Empty);
        set => SetValue(nameof(FingerprintHash), value);
    }

    [DatabaseField]
    public virtual DateTime FingerprintLastChecked
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(FingerprintLastChecked)), DateTime.MinValue);
        set => SetValue(nameof(FingerprintLastChecked), value);
    }

    [DatabaseField]
    public virtual int FingerprintChannelId
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(FingerprintChannelId)), 0);
        set => SetValue(nameof(FingerprintChannelId), value);
    }

    [DatabaseField]
    public virtual string FingerprintLanguageCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(FingerprintLanguageCode)), string.Empty);
        set => SetValue(nameof(FingerprintLanguageCode), value);
    }

    [DatabaseField]
    public virtual string? FingerprintContentPath
    {
        get => ValidationHelper.GetString(GetValue(nameof(FingerprintContentPath)), null);
        set => SetValue(nameof(FingerprintContentPath), value, string.Empty);
    }
}
