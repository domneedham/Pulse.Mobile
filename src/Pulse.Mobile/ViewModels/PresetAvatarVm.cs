using CommunityToolkit.Mvvm.ComponentModel;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>
/// A single preset-avatar tile in the picker (Edit profile / Profile setup). Wraps a
/// <see cref="PresetAvatar"/> for binding and tracks its own upload spinner so tapping one tile
/// shows progress without blocking the others.
/// </summary>
public partial class PresetAvatarVm(PresetAvatar preset) : ObservableObject
{
    public PresetAvatar Preset { get; } = preset;

    public string Emoji => Preset.Emoji;
    public Color Top => Preset.Top;
    public Color Bottom => Preset.Bottom;

    [ObservableProperty]
    private bool _isUploading;
}
