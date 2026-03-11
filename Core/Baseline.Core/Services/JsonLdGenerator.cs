using System.Text.Json;

namespace Baseline.Core;

/// <summary>
/// Default implementation of <see cref="IJsonLdGenerator"/>.
/// </summary>
internal sealed class JsonLdGenerator : IJsonLdGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Generate(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonOptions);
        return $"<script type=\"application/ld+json\">{json}</script>";
    }

    public string Generate(IEnumerable<object> schemas)
    {
        var schemaList = schemas.ToList();
        if (schemaList.Count == 0)
        {
            return string.Empty;
        }

        if (schemaList.Count == 1)
        {
            return Generate(schemaList[0]);
        }

        var json = JsonSerializer.Serialize(schemaList, JsonOptions);
        return $"<script type=\"application/ld+json\">{json}</script>";
    }
}
