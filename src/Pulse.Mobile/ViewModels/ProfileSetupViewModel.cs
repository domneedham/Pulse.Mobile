using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>
/// The post-signup "Tell us about yourself" step: set the display name + username (how friends find
/// you) and optionally pick an avatar before landing in the app. This is where the profile details
/// are collected — the create-account screen only takes email + password — so nothing is asked twice.
/// Picking a preset renders it to a PNG on-device and uploads it through the same flow Profile uses
/// (<see cref="AvatarRenderer.RenderPng"/> + <see cref="IPulseApiClient.UploadAvatarAsync"/>), so the
/// avatar is real. The avatar is optional (Skip), but a display name + valid username are required to
/// continue.
/// </summary>
public partial class ProfileSetupViewModel(
    IPulseApiClient api,
    UserSession userSession,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject
{
    public IReadOnlyList<PresetAvatarVm> PresetAvatars { get; } =
        UI.PresetAvatars.All.Select(p => new PresetAvatarVm(p)).ToList();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task PickAvatar(PresetAvatarVm option)
    {
        if (IsBusy || option.IsUploading)
        {
            return;
        }

        option.IsUploading = true;
        IsBusy = true;
        try
        {
            // Render the chosen preset to a PNG on-device and upload it through the same storage
            // flow a captured photo uses.
            var png = await Task.Run(() => AvatarRenderer.RenderPng(option.Preset));
            var me = await api.UploadAvatarAsync(png, "image/png", $"{option.Preset.Id}.png");
            AvatarUrl = me.AvatarUrl;
            HasAvatar = !string.IsNullOrWhiteSpace(me.AvatarUrl);
            userSession.Set(me); // keep the shared header avatar in sync
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
        finally
        {
            option.IsUploading = false;
            IsBusy = false;
        }
    }

    private bool CanContinue() =>
        !IsBusy && !string.IsNullOrWhiteSpace(DisplayName) && IsUsernameWellFormed(Username);

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private async Task Continue()
    {
        IsBusy = true;
        try
        {
            // Set the display name and claim the username (the avatar, if any, was already uploaded
            // on pick). A taken/invalid username is rejected by the API — surface it and stay on the
            // page so they can pick another, rather than letting them through with no username set.
            var username = Username.Trim().ToLowerInvariant();
            try
            {
                var me = await api.UpdateMeAsync(new UpdateProfileRequest(DisplayName.Trim(), null, null, username));
                userSession.Set(me);
            }
            catch (ApiException ex)
            {
                await alerts.ShowAsync("Username unavailable",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "That username is already taken. Try another."
                        : ex.Message);
                return; // do not progress — let them choose a different username
            }
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
            return;
        }
        finally
        {
            IsBusy = false;
        }

        await AuthRouting.GoAfterAuthAsync(userSession, navigationService);
    }

    private static bool IsUsernameWellFormed(string username)
    {
        var u = username.Trim();
        return u.Length is >= 3 and <= 30
            && System.Text.RegularExpressions.Regex.IsMatch(u, "^[A-Za-z][A-Za-z0-9_]*$");
    }
}
