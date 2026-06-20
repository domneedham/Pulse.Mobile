namespace Pulse.Models;

// Wire models mirroring Pulse.Api.ApiService.Contracts (camelCase JSON, enums as strings).

public record UserSummary(Guid Id, string DisplayName, string? AvatarUrl, string? Username = null);

public record User(
    Guid Id, string DisplayName, string? AvatarUrl, string Timezone, DateTimeOffset CreatedAt, string? Username = null);

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
