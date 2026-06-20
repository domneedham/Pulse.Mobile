using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.Services.Auth;

public interface IAuthService
{
    /// <summary>The signed-in user's id (from the JWT subject), or null when signed out.</summary>
    Guid? UserId { get; }

    Task<bool> TryRestoreSessionAsync();
    Task SignUpAsync(string email, string password, string displayName, CancellationToken cancellationToken = default);
    Task SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task SignOutAsync();

    /// <summary>Returns a non-expired access token, refreshing if necessary. Throws <see cref="AuthException"/> when signed out.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public class AuthException(string message) : Exception(message);

/// <summary>
/// Talks straight to the Supabase GoTrue REST API ({SupabaseUrl}/auth/v1) and keeps
/// the session in SecureStorage. The Pulse API only needs the resulting bearer token.
/// </summary>
public class AuthService : IAuthService
{
    private const string StorageKey = "pulse.auth.session";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private StoredSession? _session;

    public AuthService()
    {
        _http = new HttpClient { BaseAddress = new Uri($"{AppConfig.SupabaseUrl}/auth/v1/") };
        _http.DefaultRequestHeaders.Add("apikey", AppConfig.SupabaseAnonKey);
    }

    public Guid? UserId => _session?.UserId;

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            _session = JsonSerializer.Deserialize<StoredSession>(json, Json);
        }
        catch (Exception)
        {
            // Corrupt or inaccessible secure storage — treat as signed out and reset.
            SecureStorage.Default.Remove(StorageKey);
            _session = null;
        }

        if (_session is null)
        {
            return false;
        }

        // Validate (and refresh) eagerly so the startup decision is trustworthy.
        try
        {
            await GetAccessTokenAsync();
            return true;
        }
        catch (AuthException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            // Backend unreachable: keep the session optimistically; calls will retry.
            return true;
        }
    }

    public async Task SignUpAsync(string email, string password, string displayName, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "signup",
            new { email, password, data = new { name = displayName } },
            Json,
            cancellationToken);

        await StoreSessionFromResponseAsync(response, cancellationToken);
    }

    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "token?grant_type=password",
            new { email, password },
            Json,
            cancellationToken);

        await StoreSessionFromResponseAsync(response, cancellationToken);
    }

    public Task SignOutAsync()
    {
        _session = null;
        SecureStorage.Default.Remove(StorageKey);
        return Task.CompletedTask;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var session = _session ?? throw new AuthException("Not signed in.");

        // 30s of slack so a token never expires mid-request.
        if (session.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return session.AccessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            session = _session ?? throw new AuthException("Not signed in.");
            if (session.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            {
                return session.AccessToken;
            }

            var response = await _http.PostAsJsonAsync(
                "token?grant_type=refresh_token",
                new { refresh_token = session.RefreshToken },
                Json,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Refresh token rejected — the session is dead.
                await SignOutAsync();
                throw new AuthException("Your session has expired. Please sign in again.");
            }

            await StoreSessionFromResponseAsync(response, cancellationToken);
            return _session!.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task StoreSessionFromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AuthException(ExtractErrorMessage(body));
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(body, Json);
        if (token?.AccessToken is null || token.RefreshToken is null || token.User?.Id is null)
        {
            throw new AuthException("Unexpected response from the authentication service.");
        }

        _session = new StoredSession(
            token.AccessToken,
            token.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
            Guid.Parse(token.User.Id));

        await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(_session, Json));
    }

    private static string ExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var key in new[] { "msg", "error_description", "message", "error" })
            {
                if (doc.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
        }

        return "Something went wrong signing you in. Please try again.";
    }

    private record StoredSession(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, Guid UserId);

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("user")]
        public TokenUser? User { get; set; }
    }

    private class TokenUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
