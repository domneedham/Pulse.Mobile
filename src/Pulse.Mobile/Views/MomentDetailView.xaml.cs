using Plugin.Maui.Audio;
using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class MomentDetailView : ContentPage
{
    private static readonly HttpClient Http = new();
    private IAudioPlayer? _player;

    public MomentDetailView(MomentDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        // No large title here: the mockup has a compact bar (back + star) with the hero right below.
        // Make the nav bar transparent (Liquid Glass) and let the hero scroll underneath it.
        GlassNavBar.Enable(this);
    }

    // Tap a voice response to play it: download the audio, then play from a stream. A second tap (or
    // navigating away) stops the current player.
    private async void OnPlayVoiceTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string url || string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            StopPlayer();

            var bytes = await Http.GetByteArrayAsync(url);
            _player = AudioManager.Current.CreatePlayer(new MemoryStream(bytes));
            _player.PlaybackEnded += (_, _) => StopPlayer();
            _player.Play();
        }
        catch (Exception)
        {
            // Best-effort playback; ignore transient failures.
        }
    }

    private void StopPlayer()
    {
        if (_player is null)
        {
            return;
        }

        try
        {
            _player.Stop();
            _player.Dispose();
        }
        catch (Exception)
        {
        }
        finally
        {
            _player = null;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopPlayer();
    }
}
