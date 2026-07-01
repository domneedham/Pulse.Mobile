using Plugin.Maui.Audio;
using Pulse.Resources;
using Pulse.UI;
using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

/// <summary>
/// Voice answer sheet. Records a short audio note with Plugin.Maui.Audio, then hands the captured bytes
/// to the view model for upload. Tap the mic to start, tap again to stop.
/// </summary>
public partial class RespondVoiceView : BottomSheetPage
{
    private readonly RespondVoiceViewModel _vm;
    private readonly IAudioManager _audioManager = AudioManager.Current;
    private IAudioRecorder? _recorder;
    private bool _isRecording;

    public RespondVoiceView(RespondVoiceViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    private async void OnRecordTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (_isRecording)
            {
                await StopAsync();
                return;
            }

            if (!await EnsureMicPermissionAsync())
            {
                StatusLabel.Text = "Microphone permission is needed to record.";
                return;
            }

            _recorder = _audioManager.CreateRecorder();
            await _recorder.StartAsync();
            _isRecording = true;
            _vm.IsRecording = true;
            RecordGlyph.Icon = PulseIcon.Stop;
            StatusLabel.Text = "Recording… tap to stop";
        }
        catch (Exception)
        {
            StatusLabel.Text = "Couldn't start recording.";
            _isRecording = false;
            _vm.IsRecording = false;
        }
    }

    private async Task StopAsync()
    {
        _isRecording = false;
        _vm.IsRecording = false;
        RecordGlyph.Icon = PulseIcon.Voice;

        if (_recorder is null)
        {
            return;
        }

        var source = await _recorder.StopAsync();
        _recorder = null;

        using var stream = source.GetAudioStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        // Plugin.Maui.Audio records m4a/aac on device by default.
        _vm.SetRecording(ms.ToArray(), "audio/mp4");
        StatusLabel.Text = "Recorded — tap to re-record";
    }

    private static async Task<bool> EnsureMicPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        }

        return status == PermissionStatus.Granted;
    }
}
