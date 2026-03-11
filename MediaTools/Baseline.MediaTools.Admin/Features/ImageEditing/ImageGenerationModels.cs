namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Configuration for Google Gemini / Imagen AI image generation.
/// Bind to <c>GeminiImageGeneration</c> section in appsettings.
/// </summary>
public class GeminiImageOptions
{
    public const string SectionName = "GeminiImageGeneration";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-2.5-flash-image";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

public record ImageGenerationRequest
{
    public string Prompt { get; init; } = "";
    public string NegativePrompt { get; init; } = "";
    public string AspectRatio { get; init; } = "1:1";
    public int NumImages { get; init; } = 1;
    public byte[]? ReferenceImageBytes { get; init; }
    public string? ReferenceImageMimeType { get; init; }
}

public record ImageGenerationResult
{
    public byte[] ImageBytes { get; init; } = [];
    public int Width { get; init; }
    public int Height { get; init; }
    public string Prompt { get; init; } = "";
}

// ─── FormComponent command DTOs ─────────────────────────────────────────────

public class GenerateImageCommandArguments
{
    public string Prompt { get; set; } = "";
    public string NegativePrompt { get; set; } = "";
    public string AspectRatio { get; set; } = "1:1";
    public string? ReferenceImageBase64 { get; set; }
    public string? ReferenceImageMimeType { get; set; }
}

public class GenerateImageCommandResult
{
    public ImageEditorAssetClientItem? AssetMetadata { get; set; }
    public string? ErrorMessage { get; set; }
}
