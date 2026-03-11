using CMS.Base;
using CMS.Localization;

[assembly: RegisterLocalizationResource(
    typeof(Baseline.Localization.Admin.BaselineLocalizationResource),
    SystemContext.SYSTEM_CULTURE_NAME)]

namespace Baseline.Localization.Admin;

/// <summary>
/// Resource class for Baseline Localization admin strings.
/// Per Kentico docs, assembly attribute registers .resx for admin UI localization.
/// </summary>
public sealed class BaselineLocalizationResource
{
}
