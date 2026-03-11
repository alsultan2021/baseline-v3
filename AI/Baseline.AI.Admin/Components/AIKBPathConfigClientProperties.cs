using Baseline.AI.Admin.Models;

using Kentico.Xperience.Admin.Base.Forms;

namespace Baseline.AI.Admin.Components;

/// <summary>
/// Client properties sent to the React AIKBPathConfiguration form component.
/// </summary>
internal sealed class AIKBPathConfigClientProperties
    : FormComponentClientProperties<IEnumerable<AIKBPathConfiguration>>
{
    /// <summary>All website content types available for selection.</summary>
    public IEnumerable<AIKBContentType>? PossibleContentTypeItems { get; set; }

    /// <summary>All website channels available for selection.</summary>
    public IEnumerable<AIKBChannel>? PossibleChannels { get; set; }
}
