namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Abstraction for AI-powered image generation (text-to-image).
/// </summary>
public interface IImageGenerationService
{
    Task<ImageGenerationResult> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default);

    bool IsAvailable { get; }
}
