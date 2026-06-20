using Pulse.Models;
using Pulse.Services.Api;

namespace Pulse.Services;

/// <summary>
/// Holds the signed-in user's identity (display name, avatar) so screens can render "who am I"
/// without each re-fetching <c>GetMe</c>. Loaded once after auth (startup / sign-in / sign-up) and
/// cleared on sign-out. Registered as a singleton — the app's session spine.
/// </summary>
public class UserSession(IPulseApiClient api)
{
    /// <summary>Fires when the cached user changes (loaded, refreshed, or cleared).</summary>
    public event EventHandler? Changed;

    public User? Current { get; private set; }

    public string DisplayName => Current?.DisplayName ?? string.Empty;
    public string? AvatarUrl => Current?.AvatarUrl;

    /// <summary>Fetch the current user into the cache. Safe to call on every auth entry point.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        Current = await api.GetMeAsync(ct);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Overwrite the cached user (e.g. after the profile screen saves a new name/avatar).</summary>
    public void Set(User user)
    {
        Current = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (Current is null)
        {
            return;
        }

        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
