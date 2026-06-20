using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;

namespace Pulse.ViewModels;

public partial class ThemeOption(AppThemePreference preference, string label) : ObservableObject
{
    public AppThemePreference Preference { get; } = preference;
    public string Label { get; } = label;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// App settings: appearance (theme) and haptics. Split out of the Profile overview so the overview
/// stays a clean header + links page (storyboard). Both settings are device-local (theme via
/// <see cref="IThemeService"/>, haptics via <see cref="HapticService"/>).
/// </summary>
public partial class AppSettingsViewModel(
    IThemeService themeService,
    HapticService hapticService,
    INavigationService navigationService) : ObservableObject, IAppearingAware
{
    public List<ThemeOption> ThemeOptions { get; } =
    [
        new(AppThemePreference.System, "System"),
        new(AppThemePreference.Light, "Light"),
        new(AppThemePreference.Dark, "Dark"),
    ];

    [ObservableProperty]
    private bool _isHapticsEnabled;

    public ValueTask OnAppearingAsync()
    {
        foreach (var option in ThemeOptions)
        {
            option.IsSelected = option.Preference == themeService.Preference;
        }

        IsHapticsEnabled = hapticService.IsEnabled;
        return ValueTask.CompletedTask;
    }

    partial void OnIsHapticsEnabledChanged(bool value) =>
        hapticService.SetEnabled(value);

    [RelayCommand]
    private void SelectTheme(ThemeOption option)
    {
        themeService.SetPreference(option.Preference);
        foreach (var other in ThemeOptions)
        {
            other.IsSelected = other == option;
        }
    }

    [RelayCommand]
    private async Task GoBack() =>
        await navigationService.GoToAsync(Navigation.Relative().Pop());
}
