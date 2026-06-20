using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Auth;

namespace Pulse.ViewModels;

/// <summary>
/// "Create your account" (storyboard 5): just the credentials needed to provision the account —
/// email + password. The display name, username and avatar are collected on the next step
/// (<see cref="ProfileSetupViewModel"/>), which runs after the account exists (the username endpoint
/// requires auth) and avoids asking for the same details twice.
/// </summary>
public partial class SignUpViewModel(
    IAuthService authService,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    private bool CanCreateAccount() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Email)
        && Password.Length >= 6;

    [RelayCommand(CanExecute = nameof(CanCreateAccount))]
    private async Task CreateAccount()
    {
        IsBusy = true;
        try
        {
            // SignUp needs a display name; use the email's local part as a placeholder that the
            // profile step immediately overwrites with the real name.
            var placeholderName = Email.Trim().Split('@')[0];
            await authService.SignUpAsync(Email.Trim(), Password, placeholderName);

            // Hand off to "Tell us about yourself" (name + username + avatar) before landing in the
            // app. That step claims the username and runs the shared post-auth routing.
            await navigationService.GoToAsync(Navigation.Relative().Push<ProfileSetupViewModel>());
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>"Already have an account? Log in" — swap this root page for sign-in.</summary>
    [RelayCommand]
    private async Task SignIn() =>
        await navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<SignInViewModel>());
}
