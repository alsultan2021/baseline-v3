namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Parameters for a batch of image editing operations applied in one server round-trip.
/// </summary>
public record ImageEditParameters
{
    public CropRegion? Crop { get; init; }
    public int RotationDegrees { get; init; }
    public bool FlipHorizontal { get; init; }
    public bool FlipVertical { get; init; }
    public float Brightness { get; init; } = 1f;
    public float Contrast { get; init; } = 1f;
    public float Saturation { get; init; } = 1f;
    public string FilterPreset { get; init; } = "none";
    public ResizeDimensions? Resize { get; init; }
    public string OutputFormat { get; init; } = "png";
    public int Quality { get; init; } = 90;
}

public record CropRegion(int X, int Y, int Width, int Height);
public record ResizeDimensions(int Width, int Height);

// ─── FormComponent command arguments / results ──────────────────────────────

public class ApplyEditsCommandArguments
{
    public string FileIdentifier { get; set; } = "";
    public string FileName { get; set; } = "";
    public ImageEditParameters Parameters { get; set; } = new();
}

public class ApplyEditsCommandResult
{
    public ImageEditorAssetClientItem? AssetMetadata { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GetImageInfoCommandArguments
{
    public string FileIdentifier { get; set; } = "";
}

public class GetImageInfoCommandResult
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string? ErrorMessage { get; set; }
}

// ─── Client item DTOs ──────────────────────────────────────────────────────

public class ImageEditorAssetClientItem
{
    public ImageEditorAssetBaseClientItem Original { get; set; } = new();
}

public class ImageEditorAssetBaseClientItem
{
    public Guid Identifier { get; set; }
    public string Name { get; set; } = "";
    public string Extension { get; set; } = "";
    public long Size { get; set; }
    public bool IsImage { get; set; } = true;
    public string Url { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

// ─── Chunked upload DTOs ───────────────────────────────────────────────────

public class UploadChunkCommandArguments
{
    public int ChunkId { get; set; }
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string? FileIdentifier { get; set; }
    public byte[]? ChunkData { get; set; }
}

public class UploadChunkCommandResult
{
    public string? Identifier { get; set; }
    public string? Name { get; set; }
    public long Size { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CompleteUploadCommandArguments
{
    public string FileName { get; set; } = "";
    public string FileIdentifier { get; set; } = "";
    public long FileSize { get; set; }
}

public class CompleteUploadCommandResult
{
    public ImageEditorAssetClientItem? AssetMetadata { get; set; }
}

public class RemoveUploadedFileCommandArguments
{
    public ImageEditorAssetClientItem? AssetMetadata { get; set; }
}
