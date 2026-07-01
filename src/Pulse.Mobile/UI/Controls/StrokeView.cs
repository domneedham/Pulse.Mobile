using System.Text.Json;
using Pulse.UI;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Pulse.UI.Controls;

/// <summary>
/// Renders a saved drawing (the <see cref="TouchDrawing"/> stroke JSON used by PulseTouch and Moment
/// drawing answers) on an <see cref="SKCanvasView"/>. Points are normalised 0–1, so the doodle scales to
/// whatever size the control is laid out at. Read-only playback — no touch capture.
/// </summary>
public sealed class StrokeView : ContentView
{
    public static readonly BindableProperty StrokeDataProperty = BindableProperty.Create(
        nameof(StrokeData), typeof(string), typeof(StrokeView), default(string),
        propertyChanged: OnStrokeDataChanged);

    private readonly SKCanvasView _canvas;
    private TouchDrawing? _drawing;

    public StrokeView()
    {
        _canvas = new SKCanvasView();
        _canvas.PaintSurface += OnPaintSurface;
        Content = _canvas;
    }

    /// <summary>The drawing's stroke JSON (same shape stored server-side). Setting it re-renders.</summary>
    public string? StrokeData
    {
        get => (string?)GetValue(StrokeDataProperty);
        set => SetValue(StrokeDataProperty, value);
    }

    private static void OnStrokeDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (StrokeView)bindable;
        view._drawing = Parse(newValue as string);
        view._canvas.InvalidateSurface();
    }

    private static TouchDrawing? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TouchDrawing>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        if (_drawing is null || _drawing.Strokes.Count == 0)
        {
            return;
        }

        var w = e.Info.Width;
        var h = e.Info.Height;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        foreach (var stroke in _drawing.Strokes)
        {
            if (stroke.Points.Count == 0)
            {
                continue;
            }

            paint.Color = SKColor.TryParse(stroke.Color, out var c) ? c : SKColors.Black;
            // Stroke width is normalised relative to a reference canvas; scale to this canvas's width.
            paint.StrokeWidth = Math.Max(1.5f, stroke.Width * w);

            // De-normalise 0–1 points to this canvas's pixels.
            var points = stroke.Points.Select(p => new SKPoint(p.X * w, p.Y * h)).ToList();

            if (points.Count == 1)
            {
                using var dot = new SKPaint { Color = paint.Color, IsAntialias = true };
                canvas.DrawCircle(points[0], paint.StrokeWidth / 2, dot);
                continue;
            }

            using var path = new SKPath();
            path.MoveTo(points[0]);
            for (var i = 1; i < points.Count; i++)
            {
                path.LineTo(points[i]);
            }

            canvas.DrawPath(path, paint);
        }
    }
}
