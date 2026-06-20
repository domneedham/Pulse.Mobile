using Nalu;
using Pulse.ViewModels;

namespace Pulse.Services;

/// <summary>
/// Shared "where do we land after authenticating" logic. After sign-in, sign-up or the post-signup
/// profile step, we load the user identity and route to the signed-in home. Centralised so every
/// auth entry point behaves identically.
/// </summary>
public static class AuthRouting
{
    public static async Task GoAfterAuthAsync(
        UserSession userSession,
        INavigationService navigationService)
    {
        try
        {
            await userSession.LoadAsync();
        }
        catch
        {
            // Offline / API down: still land on home; it can retry loading the profile.
        }

        await navigationService.GoToAsync(
            Navigation.Absolute(NavigationBehavior.Immediate).Root<HomeViewModel>());
    }
}
