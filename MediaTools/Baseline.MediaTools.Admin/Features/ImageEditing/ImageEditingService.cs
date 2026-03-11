using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Image processing implementation using SixLabors.ImageSharp.
/// Applies crop, rotate, flip, brightness/contrast/saturation, filter presets,
/// resize, and format conversion in a single pipeline.
/// </summary>
public class ImageEditingService(ILogger<ImageEditingService> logger) : IImageEditingService
{
    private static readonly ColorMatrix SepiaMatrix = new(
        0.393f, 0.349f, 0.272f, 0,
        0.769f, 0.686f, 0.534f, 0,
        0.189f, 0.168f, 0.131f, 0,
        0, 0, 0, 1,
        0, 0, 0, 0);

    private static readonly ColorMatrix WarmMatrix = new(
        1.2f, 0, 0, 0,
        0, 1.1f, 0, 0,
        0, 0, 0.9f, 0,
        0, 0, 0, 1,
        0, 0, 0, 0);

    private static readonly ColorMatrix CoolMatrix = new(
        0.9f, 0, 0, 0,
        0, 1.0f, 0, 0,
        0, 0, 1.2f, 0,
        0, 0, 0, 1,
        0, 0, 0, 0);

    public async Task<byte[]> ApplyEdits(
        Stream sourceImage,
        ImageEditParameters parameters,
        CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync<Rgba32>(sourceImage, ct);

        image.Mutate(ctx =>
        {
            if (parameters.Crop is { } crop)
            {
                var rect = new Rectangle(crop.X, crop.Y, crop.Width, crop.Height);
                rect = Rectangle.Intersect(rect, new Rectangle(0, 0, ctx.GetCurrentSize().Width, ctx.GetCurrentSize().Height));
                if (rect.Width > 0 && rect.Height > 0)
                {
                    ctx.Crop(rect);
                }
            }

            if (parameters.RotationDegrees is 90 or 180 or 270)
            {
                ctx.Rotate(parameters.RotationDegrees);
            }

            if (parameters.FlipHorizontal)
            {
                ctx.Flip(FlipMode.Horizontal);
            }
            if (parameters.FlipVertical)
            {
                ctx.Flip(FlipMode.Vertical);
            }

            if (Math.Abs(parameters.Brightness - 1f) > 0.01f)
            {
                ctx.Brightness(parameters.Brightness);
            }
            if (Math.Abs(parameters.Contrast - 1f) > 0.01f)
            {
                ctx.Contrast(parameters.Contrast);
            }
            if (Math.Abs(parameters.Saturation - 1f) > 0.01f)
            {
                ctx.Saturate(parameters.Saturation);
            }

            switch (parameters.FilterPreset?.ToLowerInvariant())
            {
                case "grayscale":
                    ctx.Grayscale();
                    break;
                case "sepia":
                    ctx.Filter(SepiaMatrix);
                    break;
                case "warm":
                    ctx.Filter(WarmMatrix);
                    break;
                case "cool":
                    ctx.Filter(CoolMatrix);
                    break;
            }

            if (parameters.Resize is { } resize && resize.Width > 0 && resize.Height > 0)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(resize.Width, resize.Height),
                    Mode = ResizeMode.Max
                });
            }
        });

        using var output = new MemoryStream();
        var format = (parameters.OutputFormat?.ToLowerInvariant()) switch
        {
            "jpeg" or "jpg" => EncodeJpeg(image, output, parameters.Quality),
            "webp" => EncodeWebp(image, output, parameters.Quality),
            _ => EncodePng(image, output)
        };

        logger.LogDebug("Applied image edits: {Width}x{Height} {Format}",
            image.Width, image.Height, parameters.OutputFormat);

        return output.ToArray();
    }

    public Task<(int Width, int Height)> GetDimensions(Stream sourceImage, CancellationToken ct = default)
    {
        var info = Image.Identify(sourceImage);
        return Task.FromResult((info.Width, info.Height));
    }

    private static object EncodeJpeg(Image image, MemoryStream output, int quality)
    {
        image.Save(output, new JpegEncoder { Quality = Math.Clamp(quality, 1, 100) });
        return DBNull.Value;
    }

    private static object EncodeWebp(Image image, MemoryStream output, int quality)
    {
        image.Save(output, new WebpEncoder { Quality = Math.Clamp(quality, 1, 100) });
        return DBNull.Value;
    }

    private static object EncodePng(Image image, MemoryStream output)
    {
        image.Save(output, new PngEncoder());
        return DBNull.Value;
    }
}
