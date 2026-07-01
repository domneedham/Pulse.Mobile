using SkiaSharp;

namespace Pulse.UI;

/// <summary>
/// Downscales + re-encodes captured photos to a sensible upload size (Moment photos). Phone cameras
/// produce multi-MB images that blow past the API's size cap and cost storage; this caps the longest
/// edge and JPEG-compresses so uploads stay small. Uses SkiaSharp (already a project dependency).
/// </summary>
public static class ImageCompressor
{
    private const int MaxEdge = 1600;
    private const int Quality = 80;

    /// <summary>
    /// Returns JPEG bytes scaled so the longest edge is ≤ <see cref="MaxEdge"/>. Falls back to the
    /// original bytes if decoding fails (e.g. an unexpected format) so the upload can still be attempted.
    /// </summary>
    public static byte[] CompressToJpeg(byte[] original)
    {
        try
        {
            using var input = SKBitmap.Decode(original);
            if (input is null)
            {
                return original;
            }

            var scale = Math.Min(1f, (float)MaxEdge / Math.Max(input.Width, input.Height));
            using var image = scale < 1f
                ? input.Resize(
                    new SKImageInfo((int)(input.Width * scale), (int)(input.Height * scale)),
                    SKSamplingOptions.Default)
                : input;

            using var encoded = SKImage.FromBitmap(image ?? input).Encode(SKEncodedImageFormat.Jpeg, Quality);
            return encoded.ToArray();
        }
        catch (Exception)
        {
            return original;
        }
    }
}
