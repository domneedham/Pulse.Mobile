using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Auth;

namespace Pulse.ViewModels;

/// <summary>
/// Placeholder signed-in home. The real app's main experience replaces this; for now it confirms
/// the user is authenticated and offers a sign-out so the auth scaffold is exercisable end to end.
/// </summary>
public partial class HomeViewModel(
    IAuthService authService,
    UserSession userSession,
    INavigationService navigationService) : ObservableObject
{
    public string DisplayName => userSession.DisplayName;

    [RelayCommand]
    private async Task SignOut()
    {
        await authService.SignOutAsync();
        userSession.Clear();
        await navigationService.GoToAsync(
            Navigation.Absolute(NavigationBehavior.Immediate).Root<SignUpViewModel>());
    }
}
