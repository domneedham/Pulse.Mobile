using System.Text.Json.Serialization;

namespace Pulse.Models;

// Wire models mirroring Pulse.Api.ApiService.Contracts (camelCase JSON, enums as strings).

public record UserSummary(Guid Id, string DisplayName, string? AvatarUrl, string? Username = null);

public record User(
    Guid Id, string DisplayName, string? AvatarUrl, string Timezone, DateTimeOffset CreatedAt,
    string? Username = null, bool IsPro = false);

public record SetProRequest(bool IsPro);

public record UpdateProfileRequest(
    string DisplayName, string? AvatarUrl, string? Timezone, string? Username = null);

public record UsernameAvailability(string Username, bool Available, string? Reason);

// --- Devices (push registration) ---

public record RegisterDeviceRequest(
    string FcmToken,
    DevicePlatform Platform,
    string? DeviceModel,
    string? DeviceName,
    string? OsVersion,
    string? AppVersion);

public record DeviceDto(
    Guid Id,
    DevicePlatform Platform,
    string? DeviceModel,
    string? DeviceName,
    string? OsVersion,
    string? AppVersion,
    DateTimeOffset LastSeenAt);

// --- Connection (pairing) ---

public record Partner(Guid Id, string DisplayName, string? AvatarUrl, string? Username);

public record Connection(
    Guid Id,
    ConnectionStatus Status,
    string? InviteCode,
    [property: JsonPropertyName("partner")] Partner? PartnerUser,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConnectedAt)
{
    public bool IsActive => Status == ConnectionStatus.Active && PartnerUser is not null;
    public bool IsPending => Status == ConnectionStatus.Pending;
}

public record AcceptInviteRequest(string InviteCode);

// --- Pulses ---

/// <summary>Send a pulse in a category carrying a phrase + emoji (emoji optional → category default) + optional note.</summary>
public record SendPulseRequest(string Text, string? Emoji, string? Note = null);

/// <summary>Send a PulseTouch — vector stroke JSON for the hand-drawn doodle.</summary>
public record SendTouchRequest(string StrokeData);
public record SetFavoriteRequest(bool IsFavorite);

/// <summary>One pulse on the timeline: category + phrase + emoji. SentByMe drives direction.</summary>
public record Pulse(
    Guid Id,
    PulseType Type,
    string Text,
    string Emoji,
    bool SentByMe,
    DateTimeOffset CreatedAt,
    bool IsFavorite = false,
    string? Reaction = null,
    string? Note = null);

public record SetReactionRequest(string? Emoji);

/// <summary>The vector stroke JSON for a PulseTouch, fetched separately when the doodle is opened.</summary>
public record PulseTouch(Guid Id, string StrokeData);

// --- Favourites ---

public record Favorite(Guid Id, PulseType Category, string Text, string Emoji, int SortOrder);

public record FavoriteOption(string Text, string Emoji);

public record AddFavoriteRequest(PulseType Category, string Text, string? Emoji);

public record FavoriteItem(string Text, string? Emoji);

public record SetFavoritesRequest(PulseType Category, IReadOnlyList<FavoriteItem> Items);

// --- Moments ---

/// <summary>One partner's response to a Moment. Content fields populated by Kind; withheld until reveal.</summary>
public record MomentResponse(
    Guid Id,
    MomentResponseKind Kind,
    bool SubmittedByMe,
    DateTimeOffset CreatedAt,
    string? Text = null,
    string? Emoji = null,
    string? StrokeData = null,
    string? PhotoUrl = null,
    string? VoiceUrl = null,
    int? ChoiceIndex = null);

/// <summary>A daily shared Moment for the connection. ResponseKind selects the answer sheet.</summary>
public record Moment(
    Guid Id,
    MomentCategory Category,
    string Title,
    string Prompt,
    MomentResponseKind ResponseKind,
    string Emoji,
    DateOnly Date,
    DateTimeOffset CreatedAt,
    bool MyResponseSubmitted,
    bool PartnerResponded,
    bool IsComplete,
    IReadOnlyList<MomentResponse> Responses,
    bool IsFavorite = false,
    IReadOnlyList<string>? Options = null);

public record SetMomentFavoriteRequest(bool IsFavorite);
public record SubmitChoiceResponseRequest(int ChoiceIndex);

public record SubmitTextResponseRequest(string Text, string? Emoji);
public record SubmitDrawingResponseRequest(string StrokeData);

/// <summary>A merged Trail entry — either a pulse or a moment. Exactly one is non-null per Kind.</summary>
public record TrailItem(
    TrailItemKind Kind,
    DateTimeOffset Timestamp,
    Pulse? Pulse = null,
    Moment? Moment = null);

/// <summary>A pack in the store with the couple's selection/lock state.</summary>
public record Pack(
    Guid Id,
    string Key,
    string Title,
    string Emoji,
    bool IsPro,
    bool Unlocked,
    bool Locked,
    bool Selected,
    int TemplateCount);

public record SetConnectionPacksRequest(IReadOnlyList<Guid> PackIds);
