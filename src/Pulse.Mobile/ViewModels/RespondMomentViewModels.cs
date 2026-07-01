using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>Intent passed to a respond sheet — the Moment to answer (+ title/prompt/options for the sheets that need them).</summary>
public record RespondMomentIntent(Guid MomentId, string Title = "", string Prompt = "", IReadOnlyList<string>? Options = null);

/// <summary>
/// Text answer sheet — a short written response (+ optional emoji) to a reflection / love-letter / fun
/// Moment. On send it submits and pops back to the detail, which reloads to show the new state.
/// </summary>
public partial class RespondTextViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<RespondMomentIntent>
{
    private Guid _momentId;

    [ObservableProperty]
    private string _title = "Your answer";

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ValueTask OnEnteringAsync(RespondMomentIntent intent)
    {
        _momentId = intent.MomentId;
        if (!string.IsNullOrEmpty(intent.Title))
        {
            Title = intent.Title;
        }
        Prompt = intent.Prompt;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private async Task Send()
    {
        if (IsBusy)
        {
            return;
        }

        var answer = Text.Trim();
        if (answer.Length == 0)
        {
            await alerts.ShowToastAsync("Write something first ✍️");
            return;
        }

        IsBusy = true;
        try
        {
            await api.RespondTextAsync(_momentId, answer, null);
            haptics.Tap();
            await alerts.ShowToastAsync("Answer saved ✨");
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

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}

/// <summary>
/// Drawing answer sheet — reuses the SkiaSharp capture from the PulseTouch sheet. The view owns the
/// canvas and hands back a normalised <see cref="TouchDrawing"/>; this submits it as the Moment response.
/// </summary>
public partial class RespondDrawingViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<RespondMomentIntent>
{
    private Guid _momentId;

    /// <summary>Brand-aligned palette — same swatches as the PulseTouch sheet.</summary>
    public ObservableCollection<TouchColor> Colors { get; } =
    [
        new("#FF7A7A") { IsSelected = true },
        new("#A98BFF"),
        new("#07153D"),
        new("#7FAE8A"),
        new("#F2B705"),
        new("#3E7CB8"),
    ];

    [ObservableProperty]
    private bool _isBusy;

    public string SelectedColor => Colors.FirstOrDefault(c => c.IsSelected)?.Hex ?? "#FF7A7A";

    public event EventHandler? ClearRequested;

    public ValueTask OnEnteringAsync(RespondMomentIntent intent)
    {
        _momentId = intent.MomentId;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private void SelectColor(TouchColor color)
    {
        foreach (var c in Colors)
        {
            c.IsSelected = c == color;
        }

        OnPropertyChanged(nameof(SelectedColor));
    }

    [RelayCommand]
    private void Clear() => ClearRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Called by the view with the captured drawing when Send is tapped.</summary>
    public async Task SendDrawingAsync(TouchDrawing drawing)
    {
        if (IsBusy)
        {
            return;
        }

        if (drawing.Strokes.Count == 0)
        {
            await alerts.ShowToastAsync("Draw something first ✏️");
            return;
        }

        IsBusy = true;
        try
        {
            await api.RespondDrawingAsync(_momentId, drawing.ToJson());
            haptics.Tap();
            await alerts.ShowToastAsync("Drawing saved 🎨");
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

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}

/// <summary>
/// Photo answer sheet — capture or pick a photo, preview it, then upload it as the Moment response.
/// The picked photo is compressed lightly by the platform; the API caps size and validates the type.
/// </summary>
public partial class RespondPhotoViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<RespondMomentIntent>
{
    private Guid _momentId;
    private byte[]? _photoBytes;
    private string _contentType = "image/jpeg";
    private string _fileName = "photo.jpg";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    private string? _previewPath;

    [ObservableProperty]
    private bool _isBusy;

    public bool HasPhoto => _photoBytes is not null;

    public ValueTask OnEnteringAsync(RespondMomentIntent intent)
    {
        _momentId = intent.MomentId;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private Task Capture() => PickAsync(useCamera: true);

    [RelayCommand]
    private Task Choose() => PickAsync(useCamera: false);

    private async Task PickAsync(bool useCamera)
    {
        try
        {
            FileResult? result;
            if (useCamera)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await alerts.ShowToastAsync("Camera isn't available on this device.");
                    return;
                }
                result = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                var picked = await MediaPicker.Default.PickPhotosAsync();
                result = picked?.FirstOrDefault();
            }

            if (result is null)
            {
                return;
            }

            using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            // Phone photos are multi-MB; downscale + JPEG-compress so the upload stays under the API cap
            // (and keeps storage cheap). Always JPEG after this step.
            _photoBytes = ImageCompressor.CompressToJpeg(ms.ToArray());
            _contentType = "image/jpeg";
            _fileName = "photo.jpg";

            // Write to a temp file so the Image preview can load it by path.
            var temp = Path.Combine(FileSystem.CacheDirectory, $"moment-preview-{Guid.NewGuid():N}.jpg");
            await File.WriteAllBytesAsync(temp, _photoBytes);
            PreviewPath = temp;
            OnPropertyChanged(nameof(HasPhoto));
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private async Task Send()
    {
        if (IsBusy)
        {
            return;
        }

        if (_photoBytes is null)
        {
            await alerts.ShowToastAsync("Take or choose a photo first 📸");
            return;
        }

        IsBusy = true;
        try
        {
            await api.RespondPhotoAsync(_momentId, _photoBytes, _contentType, _fileName);
            haptics.Tap();
            await alerts.ShowToastAsync("Photo saved 📸");
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

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}

/// <summary>An option row in the choice sheet.</summary>
public partial class ChoiceOptionVm(int index, string label) : ObservableObject
{
    public int Index { get; } = index;
    public string Label { get; } = label;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Choice answer sheet — tap one of the template's options (this-or-that / would you rather). Submits the
/// chosen index; the reveal (who picked what) shows on the detail once both have answered.
/// </summary>
public partial class RespondChoiceViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<RespondMomentIntent>
{
    private Guid _momentId;

    public ObservableCollection<ChoiceOptionVm> Options { get; } = [];

    [ObservableProperty]
    private string _title = "Pick one";

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ValueTask OnEnteringAsync(RespondMomentIntent intent)
    {
        _momentId = intent.MomentId;
        if (!string.IsNullOrEmpty(intent.Title))
        {
            Title = intent.Title;
        }
        Prompt = intent.Prompt;

        Options.Clear();
        if (intent.Options is not null)
        {
            for (var i = 0; i < intent.Options.Count; i++)
            {
                Options.Add(new ChoiceOptionVm(i, intent.Options[i]));
            }
        }

        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private async Task Pick(ChoiceOptionVm option)
    {
        if (IsBusy)
        {
            return;
        }

        foreach (var o in Options)
        {
            o.IsSelected = o == option;
        }

        IsBusy = true;
        try
        {
            await api.RespondChoiceAsync(_momentId, option.Index);
            haptics.Tap();
            await alerts.ShowToastAsync("Choice saved ✨");
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

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}

/// <summary>
/// Voice answer sheet — the view records a short audio note (platform recorder) and hands the bytes back
/// via <see cref="SetRecording"/>; on Send they're uploaded to the voice endpoint.
/// </summary>
public partial class RespondVoiceViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<RespondMomentIntent>
{
    private Guid _momentId;
    private byte[]? _audioBytes;
    private string _contentType = "audio/mp4";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecording))]
    private bool _hasRecorded;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isBusy;

    public bool HasRecording => HasRecorded && _audioBytes is not null;

    public ValueTask OnEnteringAsync(RespondMomentIntent intent)
    {
        _momentId = intent.MomentId;
        return ValueTask.CompletedTask;
    }

    /// <summary>Set by the view when recording finishes (the view owns the platform recorder).</summary>
    public void SetRecording(byte[] bytes, string contentType)
    {
        _audioBytes = bytes;
        _contentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mp4" : contentType;
        HasRecorded = true;
        OnPropertyChanged(nameof(HasRecording));
    }

    [RelayCommand]
    private async Task Send()
    {
        if (IsBusy)
        {
            return;
        }

        if (_audioBytes is null)
        {
            await alerts.ShowToastAsync("Record something first 🎤");
            return;
        }

        IsBusy = true;
        try
        {
            var ext = _contentType.Contains("mpeg", StringComparison.OrdinalIgnoreCase) ? "mp3" : "m4a";
            await api.RespondVoiceAsync(_momentId, _audioBytes, _contentType, $"voice.{ext}");
            haptics.Tap();
            await alerts.ShowToastAsync("Voice note saved 🎤");
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

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}
