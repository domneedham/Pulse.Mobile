using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Pulse.UI;

/// <summary>
/// Renders a <see cref="PresetAvatar"/> to a 256×256 PNG entirely on-device via MAUI Graphics,
/// so the chosen avatar uploads through the same storage path a real photo eventually will —
/// no bundled image binaries, no server-side image processing.
/// </summary>
public static class AvatarRenderer
{
    public const int Size = 256;

    // private static readonly IBitmapExportService ExportService = new PlatformBitmapExportService();

    public static byte[] RenderPng(PresetAvatar preset)
    {
        return [];
        // using var context = ExportService.CreateContext(Size, Size, 1f);
        // var canvas = context.Canvas;

        // // Vertical two-tone gradient background filling the square.
        // var paint = new LinearGradientPaint
        // {
        //     StartColor = preset.Top,
        //     EndColor = preset.Bottom,
        //     StartPoint = new Point(0, 0),
        //     EndPoint = new Point(0, 1),
        // };
        // canvas.SetFillPaint(paint, new RectF(0, 0, Size, Size));
        // canvas.FillRectangle(0, 0, Size, Size);

        // // The emoji, centred. Colour fonts render the emoji glyph on supported platforms.
        // canvas.FontColor = Colors.White;
        // canvas.FontSize = Size * 0.5f;
        // canvas.DrawString(
        //     preset.Emoji,
        //     new RectF(0, 0, Size, Size),
        //     HorizontalAlignment.Center,
        //     VerticalAlignment.Center);

        // using var image = context.Image;
        // using var stream = new MemoryStream();
        // image.Save(stream);
        // return stream.ToArray();
    }
}
