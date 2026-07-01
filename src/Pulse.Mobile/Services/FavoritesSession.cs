using Pulse.Models;
using Pulse.Services.Api;

namespace Pulse.Services;

/// <summary>
/// Caches the signed-in user's favourite phrases (per category) so the send sheets can show them
/// instantly without re-fetching. Loaded after auth and refreshed whenever favourites change
/// (onboarding, settings). Registered as a singleton alongside the other session services.
/// </summary>
public class FavoritesSession(IPulseApiClient api)
{
    private IReadOnlyList<Favorite> _all = [];

    public event EventHandler? Changed;

    /// <summary>Favourites for one category, in saved order.</summary>
    public IReadOnlyList<Favorite> For(PulseType category) =>
        _all.Where(f => f.Category == category).OrderBy(f => f.SortOrder).ToList();

    public bool HasAny => _all.Count > 0;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        _all = await api.GetFavoriteListAsync(ct);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Overwrite the cache (e.g. after onboarding/settings save) so sheets reflect it at once.</summary>
    public void Set(IReadOnlyList<Favorite> all)
    {
        _all = all;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _all = [];
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
