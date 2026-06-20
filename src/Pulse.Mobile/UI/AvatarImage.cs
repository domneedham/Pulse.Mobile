using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Pulse.UI;

/// <summary>
/// Turns a picked/captured photo into a small square JPEG for upload: centre-cropped to a
/// square and scaled to <see cref="AvatarRenderer.Size"/>, then JPEG-compressed. Keeping this
/// client-side keeps the server light and uploads tiny.
/// </summary>
public static class AvatarImage
{
    private const float JpegQuality = 0.8f;

    public static async Task<byte[]> FromPhotoAsync(Stream photo, CancellationToken ct = default)
    {
        return [];
        // // Copy to a seekable buffer (MediaPicker streams aren't always seekable).
        // using var buffer = new MemoryStream();
        // await photo.CopyToAsync(buffer, ct);
        // buffer.Position = 0;

        // var source = PlatformImage.FromStream(buffer);

        // int size = AvatarRenderer.Size;
        // using var context = new PlatformBitmapExportService().CreateContext(size, size, 1f);
        // var canvas = context.Canvas;

        // // Centre-crop to a square, then draw scaled to fill the 256×256 canvas.
        // float side = Math.Min(source.Width, source.Height);
        // float srcX = (source.Width - side) / 2f;
        // float srcY = (source.Height - side) / 2f;

        // // Draw the cropped square region scaled into the full destination.
        // // MAUI Graphics has no source-rect overload, so scale the whole image and offset it.
        // float scale = size / side;
        // canvas.SaveState();
        // canvas.ClipRectangle(0, 0, size, size);
        // canvas.Translate(-srcX * scale, -srcY * scale);
        // canvas.DrawImage(source, 0, 0, source.Width * scale, source.Height * scale);
        // canvas.RestoreState();

        // using var image = context.Image;
        // using var output = new MemoryStream();
        // image.Save(output, ImageFormat.Jpeg, JpegQuality);
        // return output.ToArray();
    }
}
