using System.Text.Json;
using Pulse.UI;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Pulse.UI.Controls;

/// <summary>
/// Replays a saved <see cref="TouchDrawing"/> stroke-by-stroke, as it was drawn, on an
/// <see cref="SKCanvasView"/>. Version 2+ drawings carry a per-point elapsed-ms timestamp
/// (<see cref="TouchPoint.T"/>) and are replayed at that real pace; older drawings (or points missing a
/// timestamp) fall back to a simulated constant pace per stroke, with a short pause between strokes.
/// Starts automatically whenever <see cref="StrokeData"/> is set to a non-null value.
/// </summary>
public sealed class StrokeReplayView : ContentView
{
    public static readonly BindableProperty StrokeDataProperty = BindableProperty.Create(
        nameof(StrokeData), typeof(string), typeof(StrokeReplayView), default(string),
        propertyChanged: OnStrokeDataChanged);

    // Simulated pacing for drawings (or individual points) with no recorded timing.
    private const int SimulatedMsPerPoint = 18;
    private const int PauseBetweenStrokesMs = 220;

    private readonly SKCanvasView _canvas;
    private IDispatcherTimer? _timer;
    private TouchDrawing? _drawing;
    private List<TimedStroke> _timeline = [];
    private long _totalDurationMs;
    private long _elapsedMs;

    public StrokeReplayView()
    {
        _canvas = new SKCanvasView();
        _canvas.PaintSurface += OnPaintSurface;
        Content = _canvas;
    }

    /// <summary>The drawing's stroke JSON (same shape stored server-side). Setting it (re)starts playback.</summary>
    public string? StrokeData
    {
        get => (string?)GetValue(StrokeDataProperty);
        set => SetValue(StrokeDataProperty, value);
    }

    /// <summary>Raised once the full drawing has finished replaying.</summary>
    public event EventHandler? PlaybackCompleted;

    private static void OnStrokeDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (StrokeReplayView)bindable;
        view.Restart(newValue as string);
    }

    private void Restart(string? json)
    {
        StopTimer();

        _drawing = Parse(json);
        _timeline = BuildTimeline(_drawing);
        _totalDurationMs = _timeline.Count == 0 ? 0 : _timeline[^1].EndMs;
        _elapsedMs = 0;
        _canvas.InvalidateSurface();

        if (_timeline.Count == 0)
        {
            return;
        }

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _elapsedMs += 16;
        _canvas.InvalidateSurface();

        if (_elapsedMs < _totalDurationMs)
        {
            return;
        }

        _elapsedMs = _totalDurationMs;
        StopTimer();
        _canvas.InvalidateSurface();
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void StopTimer()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;
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

    /// <summary>
    /// Assigns each stroke a start/end time on a single shared timeline. Points with a recorded
    /// <see cref="TouchPoint.T"/> use their real (stroke-relative) offset; strokes missing timing (or any
    /// point within them) are paced evenly instead. A short pause separates consecutive strokes so the
    /// replay reads as distinct pen strokes rather than one continuous scribble.
    /// </summary>
    private static List<TimedStroke> BuildTimeline(TouchDrawing? drawing)
    {
        var result = new List<TimedStroke>();
        if (drawing is null)
        {
            return result;
        }

        long cursor = 0;
        foreach (var stroke in drawing.Strokes)
        {
            if (stroke.Points.Count == 0)
            {
                continue;
            }

            var offsets = TimedOffsetsFor(stroke);
            var strokeDuration = offsets.Count == 0 ? 0 : offsets[^1];

            result.Add(new TimedStroke(stroke, cursor, offsets));
            cursor += strokeDuration + PauseBetweenStrokesMs;
        }

        return result;
    }

    /// <summary>Per-point offsets (ms) from the start of a single stroke, real if fully timed, simulated otherwise.</summary>
    private static List<long> TimedOffsetsFor(TouchStroke stroke)
    {
        var points = stroke.Points;
        var hasFullTiming = points.All(p => p.T is not null);

        if (!hasFullTiming || points.Count == 1)
        {
            return [.. Enumerable.Range(0, points.Count).Select(i => (long)i * SimulatedMsPerPoint)];
        }

        var t0 = points[0].T!.Value;
        return [.. points.Select(p => (long)Math.Max(0, p.T!.Value - t0))];
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        if (_timeline.Count == 0)
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

        foreach (var timed in _timeline)
        {
            if (_elapsedMs <= timed.StartMs)
            {
                break;
            }

            DrawStrokeUpTo(canvas, paint, timed, w, h, _elapsedMs - timed.StartMs);
        }
    }

    private static void DrawStrokeUpTo(
        SKCanvas canvas, SKPaint paint, TimedStroke timed, int w, int h, long elapsedInStroke)
    {
        var stroke = timed.Stroke;
        var offsets = timed.Offsets;

        // How many points are "revealed" so far, plus a fractional interpolation into the next segment
        // so the line grows smoothly rather than jumping point-to-point.
        var revealed = 0;
        while (revealed < offsets.Count && offsets[revealed] <= elapsedInStroke)
        {
            revealed++;
        }

        if (revealed == 0)
        {
            return;
        }

        paint.Color = SKColor.TryParse(stroke.Color, out var c) ? c : SKColors.Black;
        paint.StrokeWidth = Math.Max(1.5f, stroke.Width * w);

        var points = stroke.Points;
        var head = new SKPoint(points[0].X * w, points[0].Y * h);

        // A single-point stroke (a tap) draws as a dot once its one point is revealed.
        if (points.Count == 1)
        {
            using var dot = new SKPaint { Color = paint.Color, IsAntialias = true };
            canvas.DrawCircle(head, paint.StrokeWidth / 2, dot);
            return;
        }

        using var path = new SKPath();
        path.MoveTo(head);
        for (var i = 1; i < revealed; i++)
        {
            path.LineTo(points[i].X * w, points[i].Y * h);
        }

        // Interpolate into the next not-yet-fully-revealed segment for smooth motion between ticks.
        if (revealed < points.Count)
        {
            var segStart = offsets[revealed - 1];
            var segEnd = offsets[revealed];
            var t = segEnd > segStart ? (float)(elapsedInStroke - segStart) / (segEnd - segStart) : 1f;
            t = Math.Clamp(t, 0f, 1f);

            var from = points[revealed - 1];
            var to = points[revealed];
            var x = from.X + (to.X - from.X) * t;
            var y = from.Y + (to.Y - from.Y) * t;
            path.LineTo(x * w, y * h);
        }

        canvas.DrawPath(path, paint);
    }

    private sealed record TimedStroke(TouchStroke Stroke, long StartMs, List<long> Offsets)
    {
        public long EndMs => StartMs + (Offsets.Count == 0 ? 0 : Offsets[^1]);
    }
}
