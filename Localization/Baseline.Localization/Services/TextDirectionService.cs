using System.Collections.Frozen;
using Microsoft.Extensions.Options;

namespace Baseline.Localization.Services;

/// <summary>
/// Service for determining text direction (LTR/RTL) based on the current language.
/// Use in layouts to set <c>dir</c> and <c>lang</c> attributes on the <c>&lt;html&gt;</c> element.
/// </summary>
public interface ITextDirectionService
{
    /// <summary>
    /// Gets the <see cref="TextDirection"/> for the current culture.
    /// </summary>
    TextDirection GetCurrentDirection();

    /// <summary>
    /// Gets the <see cref="TextDirection"/> for a specific culture code.
    /// </summary>
    TextDirection GetDirection(string cultureCode);

    /// <summary>
    /// Gets the HTML <c>dir</c> attribute value ("ltr" or "rtl") for the current culture.
    /// </summary>
    string GetCurrentDirAttribute();

    /// <summary>
    /// Gets the HTML <c>dir</c> attribute value ("ltr" or "rtl") for a specific culture code.
    /// </summary>
    string GetDirAttribute(string cultureCode);

    /// <summary>
    /// Returns true if the current culture uses right-to-left text direction.
    /// </summary>
    bool IsCurrentRtl();
}

/// <summary>
/// Default implementation using <see cref="BaselineCultureInfo.Direction"/> from configured cultures,
/// with fallback to a built-in list of known RTL language codes.
/// </summary>
internal sealed class TextDirectionService(
    ICultureService cultureService,
    IOptions<BaselineLocalizationOptions> options) : ITextDirectionService
{
    /// <summary>
    /// ISO 639-1 codes for languages written right-to-left.
    /// </summary>
    private static readonly FrozenSet<string> KnownRtlLanguages = new[]
    {
        "ar", // Arabic
        "he", // Hebrew
        "fa", // Persian (Farsi)
        "ur", // Urdu
        "ps", // Pashto
        "sd", // Sindhi
        "ku", // Kurdish (Sorani)
        "yi", // Yiddish
        "dv", // Divehi (Maldivian)
        "ug"  // Uyghur
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public TextDirection GetCurrentDirection() =>
        GetDirection(cultureService.CurrentCulture.Code);

    /// <inheritdoc />
    public TextDirection GetDirection(string cultureCode)
    {
        // Check configured cultures first — user may have explicitly set Direction
        var configured = options.Value.SupportedCultures
            .FirstOrDefault(c => string.Equals(c.Code, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (configured is not null)
        {
            return configured.Direction;
        }

        // Fallback: check against known RTL language codes using the short code
        var shortCode = cultureCode.Length >= 2 ? cultureCode[..2] : cultureCode;
        return KnownRtlLanguages.Contains(shortCode)
            ? TextDirection.RightToLeft
            : TextDirection.LeftToRight;
    }

    /// <inheritdoc />
    public string GetCurrentDirAttribute() =>
        GetDirAttribute(cultureService.CurrentCulture.Code);

    /// <inheritdoc />
    public string GetDirAttribute(string cultureCode) =>
        GetDirection(cultureCode) == TextDirection.RightToLeft ? "rtl" : "ltr";

    /// <inheritdoc />
    public bool IsCurrentRtl() =>
        GetCurrentDirection() == TextDirection.RightToLeft;
}
