namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Server-side image processing operations using SixLabors.ImageSharp.
/// </summary>
public interface IImageEditingService
{
    /// <summary>
    /// Applies all edit parameters to the source image in a single pass and
    /// returns the processed bytes in the requested output format.
    /// </summary>
    Task<byte[]> ApplyEdits(Stream sourceImage, ImageEditParameters parameters, CancellationToken ct = default);

    /// <summary>
    /// Returns the pixel dimensions of the source image without loading
    /// the full bitmap into memory.
    /// </summary>
    Task<(int Width, int Height)> GetDimensions(Stream sourceImage, CancellationToken ct = default);
}
