using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;

namespace Pulse.ViewModels;

/// <summary>
/// The "Forgot password?" request screen. Password reset isn't backed yet, so "Send reset link" is
/// honestly stubbed (a coming-soon note + pop) rather than faking a sent email. Reached from
/// <see cref="SignInViewModel"/>.
/// </summary>
public partial class ResetPasswordViewModel(
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _email = string.Empty;

    private bool CanSend() => !string.IsNullOrWhiteSpace(Email);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task Send()
    {
        await alerts.ShowAsync(
            "Coming soon",
            "Password reset by email is on the way. For now, contact support and we'll help you back in.");
        await navigationService.GoToAsync(Navigation.Relative().Pop());
    }

    [RelayCommand]
    private async Task GoBack() =>
        await navigationService.GoToAsync(Navigation.Relative().Pop());
}
