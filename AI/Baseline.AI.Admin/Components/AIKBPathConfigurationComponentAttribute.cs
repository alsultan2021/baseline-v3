using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.AI.Admin.Components;

/// <summary>
/// Marks a form property as rendered by the AI KB path configuration React component.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class AIKBPathConfigurationComponentAttribute : FormComponentAttribute
{
}
