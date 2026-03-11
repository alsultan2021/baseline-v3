using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

using Microsoft.Extensions.Logging;

namespace Baseline.Localization.Infrastructure;

/// <summary>
/// Creates the Baseline_TranslationCoverageSnapshot table on first run.
/// </summary>
public interface ITranslationCoverageInstaller
{
    void Install();
}

public sealed class TranslationCoverageInstaller(
    IInfoProvider<ResourceInfo> resourceInfoProvider,
    ILogger<TranslationCoverageInstaller> logger) : ITranslationCoverageInstaller
{
    private const string ResourceName = "Baseline.Localization";
    private const string ResourceDisplayName = "Baseline Localization";

    public void Install()
    {
        var resource = InstallResource();
        InstallSnapshotTable(resource);
    }

    private ResourceInfo InstallResource()
    {
        var resource = resourceInfoProvider.Get(ResourceName) ?? new ResourceInfo();

        resource.ResourceDisplayName = ResourceDisplayName;
        resource.ResourceName = ResourceName;
        resource.ResourceDescription = "Localization and translation coverage";
        resource.ResourceIsInDevelopment = false;

        if (resource.HasChanged)
        {
            resourceInfoProvider.Set(resource);
        }

        return resource;
    }

    private void InstallSnapshotTable(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(TranslationCoverageSnapshotInfo.OBJECT_TYPE) ??
                   DataClassInfo.New(TranslationCoverageSnapshotInfo.OBJECT_TYPE);

        info.ClassName = "Baseline.TranslationCoverageSnapshot";
        info.ClassTableName = "Baseline_TranslationCoverageSnapshot";
        info.ClassDisplayName = "Translation Coverage Snapshot";
        info.ClassResourceID = resource.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(TranslationCoverageSnapshotInfo.TranslationCoverageSnapshotID));

        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.LanguageCode), FieldDataType.Text, false, 50);
        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.LanguageDisplayName), FieldDataType.Text, false, 200);
        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.TotalContentItems), FieldDataType.Integer, false);
        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.TranslatedItems), FieldDataType.Integer, false);
        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.CoveragePercent), FieldDataType.Integer, false);
        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.IsDefault), FieldDataType.Boolean, false, defaultValue: "False");
        AddField(formInfo, nameof(TranslationCoverageSnapshotInfo.ComputedAtUtc), FieldDataType.DateTime, false);

        info.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);

        logger.LogDebug("Translation coverage snapshot table installed");
    }

    private static void AddField(FormInfo formInfo, string name, string dataType, bool allowEmpty, int size = 0, string? defaultValue = null)
    {
        var field = new FormFieldInfo
        {
            Name = name,
            Visible = false,
            DataType = dataType,
            AllowEmpty = allowEmpty,
            Enabled = true,
        };
        if (size > 0) field.Size = size;
        if (defaultValue is not null) field.DefaultValue = defaultValue;

        formInfo.AddFormItem(field);
    }
}
