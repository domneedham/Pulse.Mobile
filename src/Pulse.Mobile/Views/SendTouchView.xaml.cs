using Pulse.UI;
using Pulse.UI.Controls;
using Pulse.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Pulse.Views;

/// <summary>
/// PulseTouch drawing sheet. Captures finger strokes on an SKCanvasView, renders them live, and on
/// Send serialises them to a normalised <see cref="TouchDrawing"/> (points 0–1) for the API.
/// </summary>
public partial class SendTouchView : BottomSheetPage
{
    private readonly SendTouchViewModel _vm;

    // Completed strokes + the one currently being drawn. Stored in canvas pixels; normalised on send.
    private readonly List<DrawnStroke> _strokes = [];
    private DrawnStroke? _current;
    private SKSize _canvasSize;

    public SendTouchView(SendTouchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        Canvas.PaintSurface += OnPaintSurface;
        Canvas.Touch += OnTouch;
        _vm.ClearRequested += OnClearRequested;
    }

    private void OnClearRequested(object? sender, EventArgs e)
    {
        _strokes.Clear();
        _current = null;
        Canvas.InvalidateSurface();
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _current = new DrawnStroke(_vm.SelectedColor);
                _current.Points.Add(e.Location);
                _strokes.Add(_current);
                e.Handled = true;
                break;

            case SKTouchAction.Moved when _current is not null:
                _current.Points.Add(e.Location);
                e.Handled = true;
                break;

            case SKTouchAction.Released or SKTouchAction.Cancelled:
                _current = null;
                e.Handled = true;
                break;
        }

        Canvas.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        _canvasSize = e.Info.Size;
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true,
            StrokeWidth = StrokePixelWidth(e.Info.Width)
        };

        foreach (var stroke in _strokes)
        {
            if (stroke.Points.Count == 0)
            {
                continue;
            }

            paint.Color = SKColor.TryParse(stroke.Color, out var c) ? c : SKColors.Black;

            using var path = new SKPath();
            path.MoveTo(stroke.Points[0]);
            for (var i = 1; i < stroke.Points.Count; i++)
            {
                path.LineTo(stroke.Points[i]);
            }

            // A single tap (one point) draws a dot.
            if (stroke.Points.Count == 1)
            {
                canvas.DrawCircle(stroke.Points[0], paint.StrokeWidth / 2, new SKPaint { Color = paint.Color, IsAntialias = true });
            }
            else
            {
                canvas.DrawPath(path, paint);
            }
        }
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var drawing = BuildDrawing();
        await _vm.SendDrawingAsync(drawing);
    }

    private TouchDrawing BuildDrawing()
    {
        var w = _canvasSize.Width <= 0 ? 1 : _canvasSize.Width;
        var h = _canvasSize.Height <= 0 ? 1 : _canvasSize.Height;
        const float widthNormalised = 4f / 300f; // pen width relative to a ~300px reference canvas

        var strokes = _strokes
            .Where(s => s.Points.Count > 0)
            .Select(s => new TouchStroke(
                s.Color,
                widthNormalised,
                s.Points.Select(p => new TouchPoint(p.X / w, p.Y / h)).ToList()))
            .ToList();

        return new TouchDrawing(TouchDrawing.CurrentVersion, strokes);
    }

    // Pen width in canvas pixels, scaled to the canvas so it looks consistent across devices.
    private static float StrokePixelWidth(int canvasWidthPx) => Math.Max(3f, canvasWidthPx * 0.012f);

    private sealed class DrawnStroke(string color)
    {
        public string Color { get; } = color;
        public List<SKPoint> Points { get; } = [];
    }
}
