namespace Pulse.Models;

// Mirrors Pulse.Api.ApiService.Domain enums; serialized as strings on the wire.

public enum DevicePlatform
{
    Ios,
    Android
}

public enum ConnectionStatus
{
    Pending,
    Active,
    Cancelled
}

public enum PulseType
{
    Mood = 1,
    Need = 2,
    Thought = 3,
    Touch = 4
}

public enum MomentCategory
{
    Capture,
    Draw,
    LoveLetter,
    Voice,
    Fun,
    Adventure,
    Reflection,
    Puzzle,
    Micro
}

public enum MomentResponseKind
{
    Text,
    Drawing,
    Photo,
    Voice,
    Choice
}

/// <summary>Which kind of entry a Trail item is.</summary>
public enum TrailItemKind
{
    Pulse,
    Moment
}
