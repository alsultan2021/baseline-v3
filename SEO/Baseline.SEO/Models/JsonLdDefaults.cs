using System.Text.Json;

namespace Baseline.SEO;

/// <summary>
/// Shared JSON-LD serialization defaults for structured data models.
/// Avoids allocating new JsonSerializerOptions on every ToJsonLd() call.
/// </summary>
internal static class JsonLdDefaults
{
    /// <summary>
    /// Shared options for JSON-LD serialization (indented for readability).
    /// Thread-safe — JsonSerializerOptions is safe to share after construction.
    /// </summary>
    internal static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };
}
