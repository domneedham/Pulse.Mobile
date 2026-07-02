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
/// <para>
/// With the default <see cref="AutoPlay"/>=true, playback starts as soon as <see cref="StrokeData"/> is
/// set — right for a single standalone viewer (e.g. the pulse detail page). Set <see cref="AutoPlay"/> to
/// false for a row inside a scrolling list (see TrailView) — the owner must call <see cref="Play"/>
/// explicitly once it has independently confirmed the row is actually visible, NOT merely once
/// <see cref="StrokeData"/> is bound or the cell's Loaded fires: on iOS, CollectionView
/// materializes/binds cells ahead of the visible viewport for scroll-performance prefetching (and its
/// Scrolled event never fires on initial layout, only on a real scroll offset change, so it can't seed an
/// initial visible range either), so binding/Loaded alone fire well before — or sometimes without ever —
/// a row being on screen. Driving playback off either makes rows animate in a batch as the list populates
/// instead of as the user scrolls to them; see TrailView's delay-after-Loaded confirmation for how it
/// approximates real visibility without reaching into platform APIs.
/// </para>
/// <para>
/// Row identity across recycling is the other half of the problem: on iOS, a CollectionView with the
/// (default) RecycleElement caching strategy reuses the same physical cell/view instance across many
/// different rows as you scroll, silently swapping only its BindingContext — the view's Loaded/Unloaded
/// only fire once for that physical instance's whole lifetime, NOT once per row it ends up displaying
/// (confirmed: dotnet/maui#21331). So per-row "have I already played" state can't live in a
/// Loaded/Unloaded-keyed lookup either — it has to travel with the row's own bound data instead.
/// <see cref="HasPlayed"/> (two-way bindable, meant to be bound to a persistent field on the row's own
/// view-model — see TrailRowVm.HasPlayed) is that: whenever <see cref="StrokeData"/> changes (which DOES
/// fire on every recycle), the view checks HasPlayed for the newly-bound row and shows the finished frame
/// immediately if so, instead of the owner having to re-derive that from a separate tracking structure.
/// The drawing plays at most once per row: on <see cref="PlaybackCompleted"/>, or if it's still mid-play
/// when <see cref="StrokeData"/> changes again (the cell got recycled onto a different row before
/// finishing — treated as "seen", not resumed, since resuming through recycling churn just reads as
/// stuck/janky).
/// </para>
/// </summary>
public sealed class StrokeReplayView : ContentView
{
    public static readonly BindableProperty StrokeDataProperty = BindableProperty.Create(
        nameof(StrokeData), typeof(string), typeof(StrokeReplayView), default(string),
        propertyChanged: OnStrokeDataChanged);

    public static readonly BindableProperty AutoPlayProperty = BindableProperty.Create(
        nameof(AutoPlay), typeof(bool), typeof(StrokeReplayView), true);

    public static readonly BindableProperty HasPlayedProperty = BindableProperty.Create(
        nameof(HasPlayed), typeof(bool), typeof(StrokeReplayView), false,
        BindingMode.TwoWay, propertyChanged: OnHasPlayedChanged);

    // Simulated pacing for drawings (or individual points) with no recorded timing.
    private const int SimulatedMsPerPoint = 18;
    private const int PauseBetweenStrokesMs = 220;

    private readonly SKCanvasView _canvas;
    private IDispatcherTimer? _timer;
    private TouchDrawing? _drawing;
    private List<TimedStroke> _timeline = [];
    private long _totalDurationMs;
    private long _elapsedMs;

    // Guards against OnHasPlayedChanged reacting to our own writes back to the two-way-bound property
    // (e.g. when playback finishes and we set HasPlayed = true ourselves).
    private bool _settingHasPlayed;

    public StrokeReplayView()
    {
        _canvas = new SKCanvasView();
        _canvas.PaintSurface += OnPaintSurface;
        Content = _canvas;
    }

    /// <summary>The drawing's stroke JSON (same shape stored server-side). This is the only signal
    /// guaranteed to fire every time a recycled CollectionView cell is rebound to a different row (see
    /// class remarks) — all playback decisions are made here, in <see cref="OnStrokeDataChanged"/>.</summary>
    public string? StrokeData
    {
        get => (string?)GetValue(StrokeDataProperty);
        set => SetValue(StrokeDataProperty, value);
    }

    /// <summary>When true (default), playback starts as soon as <see cref="StrokeData"/> is set — for a
    /// single standalone viewer. When false, playback is gated on <see cref="HasPlayed"/> instead (see
    /// class remarks) — for a row in a scrolling list.</summary>
    public bool AutoPlay
    {
        get => (bool)GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    /// <summary>Two-way: bind to persistent state on the row's own view-model (e.g. TrailRowVm.HasPlayed).
    /// False (not yet played) starts playback when <see cref="StrokeData"/> is set; true jumps straight to
    /// the finished frame. The view writes true back through this binding once the row has been seen
    /// (finished naturally, or interrupted by a rebind mid-play), so the row's own object remembers it was
    /// seen even though the physical cell view goes on to show completely different rows next.</summary>
    public bool HasPlayed
    {
        get => (bool)GetValue(HasPlayedProperty);
        set => SetValue(HasPlayedProperty, value);
    }

    /// <summary>Raised once the full drawing has finished replaying.</summary>
    public event EventHandler? PlaybackCompleted;

    private static void OnHasPlayedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Only reacts to an externally-driven change (e.g. the bound TrailRowVm.HasPlayed already being
        // true when this cell gets recycled onto that row) — our own writes in MarkPlayed() set the guard
        // first so this doesn't recurse or fight the in-progress paint state.
        var view = (StrokeReplayView)bindable;
        if (view._settingHasPlayed)
        {
            return;
        }

        if ((bool)newValue)
        {
            view.ShowFinished();
        }
    }

    private static void OnStrokeDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (StrokeReplayView)bindable;

        // Still mid-play from whatever row this physical cell was previously showing — that row is being
        // recycled away unfinished, which counts as "seen" (see class remarks on why we don't try to
        // pause/resume through recycling churn).
        if (view._timer is not null)
        {
            view.MarkPlayed();
        }

        view.Load(newValue as string);

        if (view.AutoPlay)
        {
            view.Play();
            return;
        }

        // AutoPlay=false: binding alone must NOT start playback (see class remarks — iOS prefetches
        // CollectionView cells ahead of the viewport, so that would animate rows before they're visible).
        // A previously-played row still needs to show its finished frame right away, since nothing else
        // will paint it; an unplayed row just sits blank until the owner calls Play() once it's actually
        // on screen.
        if (view.HasPlayed)
        {
            view.ShowFinished();
        }
    }

    /// <summary>Parses the drawing and resets the timeline to the start, without starting playback.</summary>
    private void Load(string? json)
    {
        StopTimer();

        _drawing = Parse(json);
        _timeline = BuildTimeline(_drawing);
        _totalDurationMs = _timeline.Count == 0 ? 0 : _timeline[^1].EndMs;
        _elapsedMs = 0;
        _canvas.InvalidateSurface();
    }

    /// <summary>Starts (or resumes) ticking the timeline forward. No-op if already finished or empty.</summary>
    public void Play()
    {
        if (_timer is not null || _timeline.Count == 0 || _elapsedMs >= _totalDurationMs)
        {
            return;
        }

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    /// <summary>Stops ticking, leaving the drawing at its current progress (e.g. a row scrolled off screen).</summary>
    public void Pause() => StopTimer();

    /// <summary>Stops ticking and resets progress to the start, so a later <see cref="Play"/> replays from
    /// the beginning (e.g. a recycled list row that scrolls back into view later).</summary>
    public void Rewind()
    {
        StopTimer();
        _elapsedMs = 0;
        _canvas.InvalidateSurface();
    }

    /// <summary>Stops ticking and jumps straight to the completed drawing, without replaying it — for a
    /// row that already played once and shouldn't animate again on a later scroll back into view. Also
    /// marks the row played (see <see cref="MarkPlayed"/>).</summary>
    public void ShowFinished()
    {
        StopTimer();
        _elapsedMs = _totalDurationMs;
        _canvas.InvalidateSurface();
        MarkPlayed();
    }

    /// <summary>Whether playback has reached the end.</summary>
    public bool IsFinished => _timeline.Count > 0 && _elapsedMs >= _totalDurationMs;

    /// <summary>Marks the row played (writing back through the two-way <see cref="HasPlayed"/> binding)
    /// without necessarily jumping the canvas to the finished frame — callers that also want the finished
    /// frame shown should call <see cref="ShowFinished"/> as well (it marks played itself, so this method
    /// exists only for the "recycled away unfinished" case which doesn't need a repaint of a cell that's
    /// about to show different data anyway).</summary>
    private void MarkPlayed()
    {
        _settingHasPlayed = true;
        HasPlayed = true;
        _settingHasPlayed = false;
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
        MarkPlayed();
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
