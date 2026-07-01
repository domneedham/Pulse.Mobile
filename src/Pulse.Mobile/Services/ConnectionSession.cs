using Pulse.Models;
using Pulse.Services.Api;

namespace Pulse.Services;

/// <summary>
/// Holds the signed-in user's current connection (pending invite or active link) so the whole app
/// can answer "am I connected, and to whom?" without each screen re-fetching. Loaded after auth and
/// refreshed whenever the pairing state changes (invite created, accepted, or disconnected).
/// Registered as a singleton alongside <see cref="UserSession"/> — together they're the session spine.
/// </summary>
public class ConnectionSession(IPulseApiClient api)
{
    /// <summary>Fires when the cached connection changes (loaded, refreshed, or cleared).</summary>
    public event EventHandler? Changed;

    public Connection? Current { get; private set; }

    /// <summary>True once both partners are linked — the app's main experience is only usable then.</summary>
    public bool IsConnected => Current?.IsActive == true;

    /// <summary>True when an invite is out but not yet accepted.</summary>
    public bool IsPending => Current?.IsPending == true;

    public Partner? Partner => Current?.PartnerUser;

    /// <summary>Fetch the current connection into the cache (null when the user has none).</summary>
    public async Task<Connection?> LoadAsync(CancellationToken ct = default)
    {
        Current = await api.GetConnectionAsync(ct);
        Changed?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    /// <summary>Overwrite the cached connection (e.g. after creating/accepting an invite).</summary>
    public void Set(Connection? connection)
    {
        Current = connection;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
