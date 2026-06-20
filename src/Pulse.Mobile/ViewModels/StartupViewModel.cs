using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Services;
using Pulse.Services.Auth;

namespace Pulse.ViewModels;

public record StartupIntent;

public partial class StartupViewModel(
    IAuthService authService,
    UserSession userSession,
    IDispatcher dispatcher,
    INavigationService navigationService,
    ILogger<StartupViewModel> logger) : ObservableObject, IAppearingAware<StartupIntent>
{
    public ValueTask OnAppearingAsync(StartupIntent intent)
    {
        // Do the auth restore off the lifecycle hook: Nalu awaits this method, so doing slow work
        // (or awaiting navigation back into the shell) here would block and leave us stuck on the
        // splash. Fire-and-forget onto the dispatcher and return immediately.
        _ = dispatcher.DispatchAsync(RestoreAndRouteAsync);
        return ValueTask.CompletedTask;
    }

    private async Task RestoreAndRouteAsync()
    {
        bool signedIn = await authService.TryRestoreSessionAsync();

        if (!signedIn)
        {
            // Sign-up is the front door for signed-out users (it swaps to sign-in via its own link).
            await navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<SignUpViewModel>());
            return;
        }

        try
        {
            await userSession.LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup: failed to load user; continuing to home");
            // Offline / API down: still land on home, which can retry loading the profile.
        }

        await navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<HomeViewModel>());
    }
}
