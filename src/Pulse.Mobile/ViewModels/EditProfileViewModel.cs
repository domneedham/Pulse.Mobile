using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>
/// Edit profile (storyboard 2): pick an avatar (preset tiles, or a real photo) and edit the display
/// name. The username is fixed once set at sign-up, so it's shown read-only. Reached from the Profile
/// overview. The avatar flow renders a preset to a PNG on-device and uploads it through the same
/// storage path a captured photo uses (<see cref="AvatarRenderer"/> / <see cref="AvatarImage"/>).
/// </summary>
public partial class EditProfileViewModel(
    IPulseApiClient api,
    HapticService hapticService,
    UserSession userSession,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject, IAppearingAware
{
    public IReadOnlyList<PresetAvatarVm> PresetAvatars { get; } =
        UI.PresetAvatars.All.Select(p => new PresetAvatarVm(p)).ToList();

    [ObservableProperty]
    private User? _me;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasAvatar;

    public async ValueTask OnAppearingAsync()
    {
        try
        {
            Me = await api.GetMeAsync();
            DisplayName = Me.DisplayName;
            Username = Me.Username ?? string.Empty;
            HasAvatar = !string.IsNullOrWhiteSpace(Me.AvatarUrl);
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private async Task TakePhoto()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await alerts.ShowAsync("No camera", "This device doesn't have a camera available.");
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            await UploadFromPhotoAsync(photo);
        }
        catch (PermissionException)
        {
            await alerts.ShowAsync("Camera access needed", "Allow camera access in Settings to take a photo.");
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private async Task ChoosePhoto()
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync();
            await UploadFromPhotoAsync(photos?.FirstOrDefault());
        }
        catch (PermissionException)
        {
            await alerts.ShowAsync("Photo access needed", "Allow photo access in Settings to choose a picture.");
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    private async Task UploadFromPhotoAsync(FileResult? photo)
    {
        if (photo is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await using var stream = await photo.OpenReadAsync();
            // Centre-crop + scale to a 256px JPEG off the UI thread before uploading.
            var jpeg = await AvatarImage.FromPhotoAsync(stream);
            await UploadAndApplyAsync(jpeg, "image/jpeg", "avatar.jpg");
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
            // Render the chosen preset to a PNG on-device and upload it through the same
            // storage flow a captured photo uses.
            var png = await Task.Run(() => AvatarRenderer.RenderPng(option.Preset));
            await UploadAndApplyAsync(png, "image/png", $"{option.Preset.Id}.png");
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

    private async Task UploadAndApplyAsync(byte[] content, string contentType, string fileName)
    {
        Me = await api.UploadAvatarAsync(content, contentType, fileName);
        HasAvatar = !string.IsNullOrWhiteSpace(Me.AvatarUrl);
        userSession.Set(Me); // keep the shared header avatar in sync
        hapticService.Tap();
    }

    [RelayCommand]
    private async Task RemovePhoto()
    {
        if (IsBusy || Me is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            Me = await api.RemoveAvatarAsync();
            HasAvatar = false;
            userSession.Set(Me);
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
    private async Task Save()
    {
        if (Me is null)
        {
            return;
        }

        // Username is fixed once set (at sign-up), so only the display name is editable here.
        var name = DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(name) || name == Me.DisplayName)
        {
            await navigationService.GoToAsync(Navigation.Relative().Pop());
            return;
        }

        IsBusy = true;
        try
        {
            Me = await api.UpdateMeAsync(new UpdateProfileRequest(name, Me.AvatarUrl, null));
            DisplayName = Me.DisplayName;
            userSession.Set(Me); // keep the shared header avatar/name in sync
            await navigationService.GoToAsync(Navigation.Relative().Pop());
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
