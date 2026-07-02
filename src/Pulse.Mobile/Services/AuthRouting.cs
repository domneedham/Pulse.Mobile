using Nalu;
using Pulse.ViewModels;

namespace Pulse.Services;

/// <summary>
/// Shared "where do we land after authenticating" logic. Loads the user identity + current
/// connection, then routes to the right root: the signed-in app (Trail) when the pair is linked,
/// otherwise the pairing flow (Connect). Centralised so every auth entry point behaves identically.
/// </summary>
public static class AuthRouting
{
    public static async Task GoAfterAuthAsync(
        UserSession userSession,
        ConnectionSession connectionSession,
        FavoritesSession favoritesSession,
        INavigationService navigationService)
    {
        try
        {
            var userTask = userSession.LoadAsync();
            var favTask = favoritesSession.LoadAsync();
            await connectionSession.LoadAsync();
            await userTask;
            await favTask;
        }
        catch
        {
            // Offline / API down: fall through to routing; the destination screens can retry.
        }

        await GoToRootForStateAsync(connectionSession, navigationService);
    }

    /// <summary>Routes to Trail when connected, otherwise the Connect (pairing) screen.</summary>
    public static Task GoToRootForStateAsync(
        ConnectionSession connectionSession, INavigationService navigationService) =>
        connectionSession.IsConnected
            ? navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<TrailViewModel>())
            : navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<ConnectViewModel>());
}
