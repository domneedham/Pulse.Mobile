using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Pulse.Models;

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

    // Devices

    public Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken ct = default) =>
        SendAsync<DeviceDto>(HttpMethod.Put, "devices", request, ct);

    public Task UnregisterDeviceAsync(string fcmToken, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"devices/{Uri.EscapeDataString(fcmToken)}", null, ct);

    // Plumbing

    private async Task<T> GetAsync<T>(string path, CancellationToken ct) =>
        await ReadAsync<T>(await SendAsync(HttpMethod.Get, path, body: null, content: null, ct), ct);

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
