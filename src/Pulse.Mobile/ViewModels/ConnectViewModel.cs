using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;

namespace Pulse.ViewModels;

public record ConnectIntent;

/// <summary>
/// "Connect your Pulse" — the pairing screen for users who aren't linked yet. One partner taps
/// "Create invite" to get a code to share; the other enters that code under "I have a code". When an
/// invite is already outstanding it shows the pending code; once accepted, routing moves on to Home.
/// </summary>
public partial class ConnectViewModel(
    IPulseApiClient api,
    ConnectionSession connectionSession,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IAppearingAware<ConnectIntent>
{
    /// <summary>The code this user is sharing (set after creating an invite / when one is pending).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInviteCode))]
    private string? _myInviteCode;

    public bool HasInviteCode => !string.IsNullOrEmpty(MyInviteCode);

    /// <summary>The partner's code being entered.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptInviteCommand))]
    private string _enteredCode = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public async ValueTask OnAppearingAsync(ConnectIntent intent)
    {
        // Re-check state on appear: an invite created earlier should still show its code, and if the
        // partner accepted while we were away, move straight on.
        try
        {
            var connection = await connectionSession.LoadAsync();
            if (connectionSession.IsConnected)
            {
                await AuthRouting.GoToRootForStateAsync(connectionSession, navigationService);
                return;
            }

            if (connection?.IsPending == true)
            {
                MyInviteCode = connection.InviteCode;
            }
        }
        catch
        {
            // Best-effort; the buttons can retry.
        }
    }

    [RelayCommand]
    private async Task CreateInvite()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var connection = await api.CreateInviteAsync();
            connectionSession.Set(connection);
            MyInviteCode = connection.InviteCode;
            haptics.Tap();
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
    private async Task CopyCode()
    {
        if (!string.IsNullOrEmpty(MyInviteCode))
        {
            await Clipboard.SetTextAsync(MyInviteCode);
            haptics.Tap();
            await alerts.ShowToastAsync("Code copied");
        }
    }

    [RelayCommand]
    private async Task ShareCode()
    {
        if (string.IsNullOrEmpty(MyInviteCode))
        {
            return;
        }

        await Share.RequestAsync(new ShareTextRequest
        {
            Title = "Connect on Pulse",
            Text = $"Join me on Pulse 💗 — enter my invite code: {MyInviteCode}"
        });
    }

    private bool CanAcceptInvite() => !IsBusy && EnteredCode.Trim().Length >= 4;

    [RelayCommand(CanExecute = nameof(CanAcceptInvite))]
    private async Task AcceptInvite()
    {
        IsBusy = true;
        try
        {
            var connection = await api.AcceptInviteAsync(EnteredCode.Trim());
            connectionSession.Set(connection);
            haptics.Tap();
            await AuthRouting.GoToRootForStateAsync(connectionSession, navigationService);
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
}
