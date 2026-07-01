using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Pulse.Models;
using PulseModel = Pulse.Models.Pulse;

namespace Pulse.Services.Api;

public interface IPulseApiClient
{
    // Users
    Task<User> GetMeAsync(CancellationToken ct = default);
    Task<User> UpdateMeAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<User> UploadAvatarAsync(byte[] content, string contentType, string fileName, CancellationToken ct = default);
    Task<User> RemoveAvatarAsync(CancellationToken ct = default);
    Task DeleteMeAsync(CancellationToken ct = default);
    Task<UsernameAvailability> CheckUsernameAsync(string username, CancellationToken ct = default);
    /// <summary>DEV ONLY — toggle the caller's Pro flag (API rejects this outside Development).</summary>
    Task<User> SetProAsync(bool isPro, CancellationToken ct = default);

    // Connection (pairing)
    /// <summary>The caller's current connection (pending or active), or null when they have none.</summary>
    Task<Connection?> GetConnectionAsync(CancellationToken ct = default);
    Task<Connection> CreateInviteAsync(CancellationToken ct = default);
    Task<Connection> AcceptInviteAsync(string inviteCode, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);

    // Pulses
    Task<PulseModel?> GetLatestPulseAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PulseModel>> GetTimelineAsync(DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default);
    Task<PulseModel> GetPulseAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PulseModel>> GetFavoritesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PulseModel>> SearchPulsesAsync(string query, CancellationToken ct = default);
    Task<PulseModel> SendMoodAsync(string text, string? emoji, string? note = null, CancellationToken ct = default);
    Task<PulseModel> SendNeedAsync(string text, string? emoji, string? note = null, CancellationToken ct = default);
    Task<PulseModel> SendThoughtAsync(string text, string? emoji, string? note = null, CancellationToken ct = default);
    Task<PulseModel> SendTouchAsync(string strokeData, CancellationToken ct = default);
    Task<PulseModel> SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken ct = default);
    Task<PulseModel> SetReactionAsync(Guid id, string? emoji, CancellationToken ct = default);
    Task DeletePulseAsync(Guid id, CancellationToken ct = default);

    // Favourites (per-category saved phrases)
    Task<IReadOnlyList<Favorite>> GetFavoriteListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FavoriteOption>> GetFavoriteCatalogAsync(PulseType category, CancellationToken ct = default);
    Task<IReadOnlyList<Favorite>> SetFavoritesAsync(PulseType category, IReadOnlyList<FavoriteItem> items, CancellationToken ct = default);
    Task<Favorite> AddFavoriteAsync(PulseType category, string text, string? emoji, CancellationToken ct = default);
    Task DeleteFavoriteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Favorite>> EnsureDefaultFavoritesAsync(CancellationToken ct = default);

    // Moments
    Task<Moment> GetTodayMomentAsync(CancellationToken ct = default);
    /// <summary>Tomorrow's scheduled Moment (Pro "peek ahead"), or null if not scheduled yet.</summary>
    Task<Moment?> GetUpcomingMomentAsync(CancellationToken ct = default);
    Task<Moment> GetMomentAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Moment>> GetMomentsAsync(bool favoritesOnly = false, CancellationToken ct = default);
    Task<Moment> SetMomentFavoriteAsync(Guid id, bool isFavorite, CancellationToken ct = default);
    Task<IReadOnlyList<TrailItem>> GetTrailAsync(DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Pack>> GetPacksAsync(CancellationToken ct = default);
    /// <summary>Replace the connection's selected (Pro) packs. Returns the refreshed catalogue.</summary>
    Task<IReadOnlyList<Pack>> SetPacksAsync(IReadOnlyList<Guid> packIds, CancellationToken ct = default);
    Task<Moment> RespondTextAsync(Guid momentId, string text, string? emoji, CancellationToken ct = default);
    Task<Moment> RespondDrawingAsync(Guid momentId, string strokeData, CancellationToken ct = default);
    Task<Moment> RespondPhotoAsync(Guid momentId, byte[] content, string contentType, string fileName, CancellationToken ct = default);
    Task<Moment> RespondVoiceAsync(Guid momentId, byte[] content, string contentType, string fileName, CancellationToken ct = default);
    Task<Moment> RespondChoiceAsync(Guid momentId, int choiceIndex, CancellationToken ct = default);

    // Devices (push registration)
    Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken ct = default);
    Task UnregisterDeviceAsync(string fcmToken, CancellationToken ct = default);
}

/// <summary>Raised for non-success API responses, carrying the ProblemDetails detail when present.</summary>
public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class PulseApiClient : IPulseApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly ILogger<PulseApiClient> _logger;

    // HttpClient is created and configured by IHttpClientFactory (base address + AuthHandler are set
    // in DI), so this just records what's injected. Auth header and 401-refresh-retry live in AuthHandler.
    public PulseApiClient(HttpClient http, ILogger<PulseApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Users

    public Task<User> GetMeAsync(CancellationToken ct = default) =>
        GetAsync<User>("users/me", ct);

    public Task<User> UpdateMeAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
        SendAsync<User>(HttpMethod.Put, "users/me", request, ct);

    public async Task<User> UploadAvatarAsync(byte[] content, string contentType, string fileName, CancellationToken ct = default)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        // AuthHandler buffers the body so it can replay on a 401 retry.
        return await ReadAsync<User>(
            await SendAsync(HttpMethod.Post, "users/me/avatar", body: null, content: form, ct), ct);
    }

    public Task<User> RemoveAvatarAsync(CancellationToken ct = default) =>
        SendAsync<User>(HttpMethod.Delete, "users/me/avatar", null, ct);

    public Task DeleteMeAsync(CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, "users/me", null, ct);

    public Task<UsernameAvailability> CheckUsernameAsync(string username, CancellationToken ct = default) =>
        GetAsync<UsernameAvailability>($"users/username-available?username={Uri.EscapeDataString(username)}", ct);

    public Task<User> SetProAsync(bool isPro, CancellationToken ct = default) =>
        SendAsync<User>(HttpMethod.Post, "users/me/pro", new SetProRequest(isPro), ct);

    // Connection

    public Task<Connection?> GetConnectionAsync(CancellationToken ct = default) =>
        GetOrNullAsync<Connection>("connection", ct);

    public Task<Connection> CreateInviteAsync(CancellationToken ct = default) =>
        SendAsync<Connection>(HttpMethod.Post, "connection/invite", null, ct);

    public Task<Connection> AcceptInviteAsync(string inviteCode, CancellationToken ct = default) =>
        SendAsync<Connection>(HttpMethod.Post, "connection/accept", new AcceptInviteRequest(inviteCode), ct);

    public Task DisconnectAsync(CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, "connection", null, ct);

    // Pulses

    public Task<PulseModel?> GetLatestPulseAsync(CancellationToken ct = default) =>
        GetOrNullAsync<PulseModel>("pulses/latest", ct);

    public Task<IReadOnlyList<PulseModel>> GetTimelineAsync(
        DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default)
    {
        var path = $"pulses?limit={limit}";
        if (before is not null)
        {
            path += $"&before={Uri.EscapeDataString(before.Value.UtcDateTime.ToString("O"))}";
        }

        return GetAsync<IReadOnlyList<PulseModel>>(path, ct);
    }

    public Task<PulseModel> GetPulseAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PulseModel>($"pulses/{id}", ct);

    public Task<IReadOnlyList<PulseModel>> GetFavoritesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PulseModel>>("pulses/favorites", ct);

    public Task<IReadOnlyList<PulseModel>> SearchPulsesAsync(string query, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PulseModel>>($"pulses/search?q={Uri.EscapeDataString(query)}", ct);

    public Task<PulseModel> SendMoodAsync(string text, string? emoji, string? note = null, CancellationToken ct = default) =>
        SendAsync<PulseModel>(HttpMethod.Post, "pulses/mood", new SendPulseRequest(text, emoji, note), ct);

    public Task<PulseModel> SendNeedAsync(string text, string? emoji, string? note = null, CancellationToken ct = default) =>
        SendAsync<PulseModel>(HttpMethod.Post, "pulses/need", new SendPulseRequest(text, emoji, note), ct);

    public Task<PulseModel> SendThoughtAsync(string text, string? emoji, string? note = null, CancellationToken ct = default) =>
        SendAsync<PulseModel>(HttpMethod.Post, "pulses/thought", new SendPulseRequest(text, emoji, note), ct);

    public Task<PulseModel> SendTouchAsync(string strokeData, CancellationToken ct = default) =>
        SendAsync<PulseModel>(HttpMethod.Post, "pulses/touch", new SendTouchRequest(strokeData), ct);

    public Task<PulseModel> SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken ct = default) =>
        SendAsync<PulseModel>(HttpMethod.Put, $"pulses/{id}/favorite", new SetFavoriteRequest(isFavorite), ct);

    public Task<PulseModel> SetReactionAsync(Guid id, string? emoji, CancellationToken ct = default) =>
        SendAsync<PulseModel>(HttpMethod.Put, $"pulses/{id}/reaction", new SetReactionRequest(emoji), ct);

    public Task DeletePulseAsync(Guid id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"pulses/{id}", null, ct);

    // Favourites

    public Task<IReadOnlyList<Favorite>> GetFavoriteListAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Favorite>>("favorites", ct);

    public Task<IReadOnlyList<FavoriteOption>> GetFavoriteCatalogAsync(PulseType category, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<FavoriteOption>>($"favorites/catalog/{category}", ct);

    public Task<IReadOnlyList<Favorite>> SetFavoritesAsync(PulseType category, IReadOnlyList<FavoriteItem> items, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Favorite>>(HttpMethod.Put, "favorites", new SetFavoritesRequest(category, items), ct);

    public Task<Favorite> AddFavoriteAsync(PulseType category, string text, string? emoji, CancellationToken ct = default) =>
        SendAsync<Favorite>(HttpMethod.Post, "favorites", new AddFavoriteRequest(category, text, emoji), ct);

    public Task DeleteFavoriteAsync(Guid id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"favorites/{id}", null, ct);

    public Task<IReadOnlyList<Favorite>> EnsureDefaultFavoritesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Favorite>>(HttpMethod.Post, "favorites/defaults", null, ct);

    // Moments

    public Task<Moment> GetTodayMomentAsync(CancellationToken ct = default) =>
        GetAsync<Moment>("moments/today", ct);

    public Task<Moment?> GetUpcomingMomentAsync(CancellationToken ct = default) =>
        GetOrNullAsync<Moment>("moments/upcoming", ct);

    public Task<Moment> GetMomentAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<Moment>($"moments/{id}", ct);

    public Task<IReadOnlyList<Moment>> GetMomentsAsync(bool favoritesOnly = false, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Moment>>($"moments?favorites={favoritesOnly.ToString().ToLowerInvariant()}", ct);

    public Task<Moment> SetMomentFavoriteAsync(Guid id, bool isFavorite, CancellationToken ct = default) =>
        SendAsync<Moment>(HttpMethod.Put, $"moments/{id}/favorite", new SetMomentFavoriteRequest(isFavorite), ct);

    public Task<IReadOnlyList<TrailItem>> GetTrailAsync(
        DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default)
    {
        var path = $"trail?limit={limit}";
        if (before is not null)
        {
            path += $"&before={Uri.EscapeDataString(before.Value.UtcDateTime.ToString("O"))}";
        }

        return GetAsync<IReadOnlyList<TrailItem>>(path, ct);
    }

    public Task<IReadOnlyList<Pack>> GetPacksAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Pack>>("packs", ct);

    public Task<IReadOnlyList<Pack>> SetPacksAsync(IReadOnlyList<Guid> packIds, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Pack>>(HttpMethod.Put, "connection/packs", new SetConnectionPacksRequest(packIds), ct);

    public Task<Moment> RespondTextAsync(Guid momentId, string text, string? emoji, CancellationToken ct = default) =>
        SendAsync<Moment>(HttpMethod.Post, $"moments/{momentId}/respond/text", new SubmitTextResponseRequest(text, emoji), ct);

    public Task<Moment> RespondDrawingAsync(Guid momentId, string strokeData, CancellationToken ct = default) =>
        SendAsync<Moment>(HttpMethod.Post, $"moments/{momentId}/respond/drawing", new SubmitDrawingResponseRequest(strokeData), ct);

    public async Task<Moment> RespondPhotoAsync(
        Guid momentId, byte[] content, string contentType, string fileName, CancellationToken ct = default)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        return await ReadAsync<Moment>(
            await SendAsync(HttpMethod.Post, $"moments/{momentId}/respond/photo", body: null, content: form, ct), ct);
    }

    public async Task<Moment> RespondVoiceAsync(
        Guid momentId, byte[] content, string contentType, string fileName, CancellationToken ct = default)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        return await ReadAsync<Moment>(
            await SendAsync(HttpMethod.Post, $"moments/{momentId}/respond/voice", body: null, content: form, ct), ct);
    }

    public Task<Moment> RespondChoiceAsync(Guid momentId, int choiceIndex, CancellationToken ct = default) =>
        SendAsync<Moment>(HttpMethod.Post, $"moments/{momentId}/respond/choice", new SubmitChoiceResponseRequest(choiceIndex), ct);

    // Devices

    public Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken ct = default) =>
        SendAsync<DeviceDto>(HttpMethod.Put, "devices", request, ct);

    public Task UnregisterDeviceAsync(string fcmToken, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"devices/{Uri.EscapeDataString(fcmToken)}", null, ct);

    // Plumbing

    private async Task<T> GetAsync<T>(string path, CancellationToken ct) =>
        await ReadAsync<T>(await SendAsync(HttpMethod.Get, path, body: null, content: null, ct), ct);

    /// <summary>GET that treats 204 No Content as a null result (e.g. "no current connection / pulse yet").</summary>
    private async Task<T?> GetOrNullAsync<T>(string path, CancellationToken ct) where T : class
    {
        var response = await SendAsync(HttpMethod.Get, path, body: null, content: null, ct);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            response.Dispose();
            return null;
        }

        return await ReadAsync<T>(response, ct);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct) =>
        await ReadAsync<T>(await SendAsync(method, path, body, content: null, ct), ct);

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct) =>
        (await SendAsync(method, path, body, content: null, ct)).Dispose();

    /// <summary>
    /// The single send path. The bearer token and 401-refresh-retry are added by <see cref="AuthHandler"/>;
    /// this layer owns request construction, error translation (<see cref="ApiException"/>) and logging.
    /// Pass <paramref name="content"/> for a non-JSON body (e.g. a multipart upload); otherwise
    /// <paramref name="body"/> is serialized as JSON.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (content is not null)
        {
            request.Content = content;
        }
        else if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var message = await ExtractProblemDetailAsync(response, ct);
        _logger.LogWarning("API {Method} {Path} -> {Status}: {Detail}",
            method, path, (int)response.StatusCode, message);
        response.Dispose();
        throw new ApiException(response.StatusCode, message);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var result = await response.Content.ReadFromJsonAsync<T>(Json, ct);
        response.Dispose();
        return result ?? throw new ApiException(response.StatusCode, "Empty response from the server.");
    }

    private static async Task<string> ExtractProblemDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            foreach (var key in new[] { "detail", "title" })
            {
                if (doc.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString()!;
                }
            }
        }
        catch (Exception)
        {
        }

        return $"The request failed ({(int)response.StatusCode}).";
    }
}
