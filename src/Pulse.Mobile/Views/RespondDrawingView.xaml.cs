using System.Diagnostics;
using Pulse.UI;
using Pulse.UI.Controls;
using Pulse.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Pulse.Views;

/// <summary>
/// Moment drawing-answer sheet. Same SkiaSharp stroke capture as the PulseTouch sheet: captures finger
/// strokes, renders them live, and on Send normalises them to a <see cref="TouchDrawing"/> (points 0–1,
/// timed) for the API.
/// </summary>
public partial class RespondDrawingView : BottomSheetPage
{
    private readonly RespondDrawingViewModel _vm;

    private readonly List<DrawnStroke> _strokes = [];
    private DrawnStroke? _current;
    private SKSize _canvasSize;

    // Elapsed time since the first touch of the drawing, stamped on every point so the viewer can
    // replay the doodle at the pace it was actually drawn. Starts on the first Pressed of the session.
    private readonly Stopwatch _stopwatch = new();

    public RespondDrawingView(RespondDrawingViewModel vm)
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
        _stopwatch.Reset();
        Canvas.InvalidateSurface();
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (!_stopwatch.IsRunning)
                {
                    _stopwatch.Start();
                }

                _current = new DrawnStroke(_vm.SelectedColor);
                _current.Points.Add((e.Location, (int)_stopwatch.ElapsedMilliseconds));
                _strokes.Add(_current);
                e.Handled = true;
                break;

            case SKTouchAction.Moved when _current is not null:
                _current.Points.Add((e.Location, (int)_stopwatch.ElapsedMilliseconds));
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
            path.MoveTo(stroke.Points[0].Location);
            for (var i = 1; i < stroke.Points.Count; i++)
            {
                path.LineTo(stroke.Points[i].Location);
            }

            if (stroke.Points.Count == 1)
            {
                canvas.DrawCircle(stroke.Points[0].Location, paint.StrokeWidth / 2, new SKPaint { Color = paint.Color, IsAntialias = true });
            }
            else
            {
                canvas.DrawPath(path, paint);
            }
        }
    }

    private async void OnSendClicked(object? sender, EventArgs e) =>
        await _vm.SendDrawingAsync(BuildDrawing());

    private TouchDrawing BuildDrawing()
    {
        var w = _canvasSize.Width <= 0 ? 1 : _canvasSize.Width;
        var h = _canvasSize.Height <= 0 ? 1 : _canvasSize.Height;
        const float widthNormalised = 4f / 300f;

        var strokes = _strokes
            .Where(s => s.Points.Count > 0)
            .Select(s => new TouchStroke(
                s.Color,
                widthNormalised,
                s.Points.Select(p => new TouchPoint(p.Location.X / w, p.Location.Y / h, p.ElapsedMs)).ToList()))
            .ToList();

        return new TouchDrawing(TouchDrawing.CurrentVersion, strokes);
    }

    private static float StrokePixelWidth(int canvasWidthPx) => Math.Max(3f, canvasWidthPx * 0.012f);

    private sealed class DrawnStroke(string color)
    {
        public string Color { get; } = color;
        public List<(SKPoint Location, int ElapsedMs)> Points { get; } = [];
    }
}
