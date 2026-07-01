using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.Services.Auth;

namespace Pulse.ViewModels;

/// <summary>The Profile tab — identity, partner/connection status, links to settings, and sign out.</summary>
public partial class ProfileViewModel(
    IAuthService authService,
    IPulseApiClient api,
    UserSession userSession,
    ConnectionSession connectionSession,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject, IAppearingAware
{
    public string DisplayName => userSession.DisplayName;
    public string? AvatarUrl => userSession.AvatarUrl;
    public string? Username => userSession.Current?.Username is { } u ? $"@{u}" : null;

    public string PartnerName => connectionSession.Partner?.DisplayName ?? "Not connected";
    public string ConnectionStatusText => connectionSession.IsConnected ? "Connected" : "Pending";

    public ValueTask OnAppearingAsync()
    {
        // Reflect any profile/connection changes made elsewhere.
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(AvatarUrl));
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(PartnerName));
        OnPropertyChanged(nameof(ConnectionStatusText));
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private Task EditProfile() => navigationService.GoToAsync(Navigation.Relative().Push<EditProfileViewModel>());

    [RelayCommand]
    private Task AppSettings() => navigationService.GoToAsync(Navigation.Relative().Push<AppSettingsViewModel>());

    [RelayCommand]
    private Task HelpSupport() => navigationService.GoToAsync(Navigation.Relative().Push<HelpSupportViewModel>());

    [RelayCommand]
    private async Task Disconnect()
    {
        var confirmed = await alerts.ConfirmAsync(
            "Disconnect?",
            $"This ends your Pulse connection with {connectionSession.Partner?.DisplayName ?? "your partner"}.",
            "Disconnect");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await api.DisconnectAsync();
            connectionSession.Clear();
            await navigationService.GoToAsync(
                Navigation.Absolute(NavigationBehavior.Immediate).Root<ConnectViewModel>());
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await authService.SignOutAsync();
        userSession.Clear();
        connectionSession.Clear();
        await navigationService.GoToAsync(
            Navigation.Absolute(NavigationBehavior.Immediate).Root<SignUpViewModel>());
    }
}
