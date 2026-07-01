using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.UI;

/// <summary>
/// The serialisable shape of a PulseTouch drawing: a set of strokes, each a coloured polyline. Points
/// are normalised 0–1 (relative to the canvas) so the doodle re-renders at any size. Mirrors the JSON
/// the API stores in pulse_touches.stroke_data. Version 2 adds per-point timing (<see cref="TouchPoint.T"/>)
/// so a viewer can replay the doodle as it was actually drawn; version 1 drawings have no timing and fall
/// back to a simulated even pace.
/// </summary>
public record TouchDrawing(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("strokes")] IReadOnlyList<TouchStroke> Strokes)
{
    public const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string ToJson() => JsonSerializer.Serialize(this, Json);
}

public record TouchStroke(
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("width")] float Width,
    [property: JsonPropertyName("points")] IReadOnlyList<TouchPoint> Points);

/// <summary>
/// A point in a stroke's polyline. <see cref="T"/> is the elapsed time (ms) since the drawing started,
/// captured while the user draws; it's null for strokes drawn before version 2 (no timing recorded).
/// </summary>
public record TouchPoint(
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y,
    [property: JsonPropertyName("t")] int? T = null);
