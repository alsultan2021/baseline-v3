using Kentico.Xperience.Admin.Base;

namespace Baseline.MediaTools.Admin.Features.DesignBuilder;

/// <summary>
/// Client properties passed to the DesignBuilder React template.
/// </summary>
public class DesignBuilderClientProperties : TemplateClientProperties
{
    public IEnumerable<DesignSummary> Designs { get; set; } = [];
    public string? CurrentDesignJson { get; set; }
    public int CurrentDesignId { get; set; }
    public IEnumerable<DesignTemplate> Templates { get; set; } = [];
    public IEnumerable<MediaAsset> MediaAssets { get; set; } = [];
    public IEnumerable<CanvasSizePreset> SizePresets { get; set; } = [];
}

public record DesignSummary(int Id, string Name, string PreviewUrl, string LastModified, int Width, int Height);
public record DesignTemplate(string Id, string Name, string Category, string PreviewUrl, string DesignJson);
public record MediaAsset(string Id, string Name, string Url, string Type, int Width, int Height);
public record CanvasSizePreset(string Name, string Category, int Width, int Height);
