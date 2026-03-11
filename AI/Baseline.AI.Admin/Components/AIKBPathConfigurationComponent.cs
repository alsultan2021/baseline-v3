using Baseline.AI.Admin.Components;
using Baseline.AI.Admin.Models;

using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    identifier: AIKBPathConfigurationComponent.IDENTIFIER,
    componentType: typeof(AIKBPathConfigurationComponent),
    name: "AI KB Path Configuration")]

namespace Baseline.AI.Admin.Components;

/// <summary>
/// Custom form component for inline path management — modelled after Lucene's
/// <c>LuceneSearchIndexConfigurationComponent</c>.
/// Renders a React table with add / edit / delete for KB paths.
/// </summary>
[ComponentAttribute(typeof(AIKBPathConfigurationComponentAttribute))]
internal sealed class AIKBPathConfigurationComponent(
    IInfoProvider<ChannelInfo> channelProvider)
    : FormComponent<FormComponentProperties, AIKBPathConfigClientProperties, IEnumerable<AIKBPathConfiguration>>
{
    public const string IDENTIFIER = "baseline.ai.kb-path-configuration";

    private List<AIKBPathConfiguration>? Value { get; set; }

    public override string ClientComponentName =>
        "@baseline/ai-admin/AIKBPathConfiguration";

    public override IEnumerable<AIKBPathConfiguration> GetValue() => Value ?? [];

    public override void SetValue(IEnumerable<AIKBPathConfiguration> value) =>
        Value = value.ToList();

    // ── FormComponent commands invoked from React via executeCommand ────────

    /// <summary>Add a new path entry (in-memory only until form save).</summary>
    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> AddPath(AIKBPathConfiguration path)
    {
        Value ??= [];

        if (Value.Exists(p =>
                string.Equals(p.ChannelName, path.ChannelName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.IncludePattern, path.IncludePattern, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(ResponseFrom(new RowActionResult(false)));
        }

        Value.Add(path);
        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    /// <summary>Replace a path entry by its identifier or channel+pattern match.</summary>
    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> SavePath(AIKBPathConfiguration path)
    {
        Value ??= [];

        var existing = Value.SingleOrDefault(p => p.Identifier == path.Identifier && path.Identifier is not null)
                       ?? Value.SingleOrDefault(p =>
                           string.Equals(p.ChannelName, path.ChannelName, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(p.IncludePattern, path.IncludePattern, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            Value.Remove(existing);
        }

        Value.Add(path);
        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    /// <summary>Delete a path entry by its identifier or channel+pattern.</summary>
    [FormComponentCommand]
    public Task<ICommandResponse<RowActionResult>> DeletePath(AIKBPathConfiguration path)
    {
        Value ??= [];

        var toRemove = Value.SingleOrDefault(p => p.Identifier == path.Identifier && path.Identifier is not null)
                       ?? Value.SingleOrDefault(p =>
                           string.Equals(p.ChannelName, path.ChannelName, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(p.IncludePattern, path.IncludePattern, StringComparison.OrdinalIgnoreCase));

        if (toRemove is not null)
        {
            Value.Remove(toRemove);
        }

        return Task.FromResult(ResponseFrom(new RowActionResult(false)));
    }

    // ── Client property population ──────────────────────────────────────────

    protected override async Task ConfigureClientProperties(AIKBPathConfigClientProperties properties)
    {
        // Website channels
        var channels = await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync();

        properties.PossibleChannels = channels
            .Select(c => new AIKBChannel(c.ChannelName, c.ChannelDisplayName))
            .ToList();

        // Website + Reusable content types
        var contentTypes = DataClassInfoProvider.ProviderObject
            .Get()
            .WhereIn(nameof(DataClassInfo.ClassContentTypeType), ["Website", "Reusable"])
            .GetEnumerableTypedResult()
            .Select(c => new AIKBContentType(c.ClassName, c.ClassDisplayName))
            .ToList();

        properties.PossibleContentTypeItems = contentTypes;
        properties.Value = Value ?? [];

        await base.ConfigureClientProperties(properties);
    }
}
