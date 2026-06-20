using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Auth;

namespace Pulse.ViewModels;

public partial class SignInViewModel(
    IAuthService authService,
    UserSession userSession,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    private bool CanSignIn() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Email) && Password.Length > 0;

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignIn()
    {
        IsBusy = true;
        try
        {
            await authService.SignInAsync(Email.Trim(), Password);
            await AuthRouting.GoAfterAuthAsync(userSession, navigationService);
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

    [RelayCommand]
    private async Task ForgotPassword() =>
        await navigationService.GoToAsync(Navigation.Relative().Push<ResetPasswordViewModel>());

    /// <summary>"Don't have an account? Sign up" — swap this root page for sign-up.</summary>
    [RelayCommand]
    private async Task SignUp() =>
        await navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<SignUpViewModel>());
}
