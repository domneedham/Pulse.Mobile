using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;

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
    INavigationService navigationService,
    UserSession userSession,
    IPulseApiClient api,
    IAlertService alerts) : ObservableObject, IAppearingAware
{
    public List<ThemeOption> ThemeOptions { get; } =
    [
        new(AppThemePreference.System, "System"),
        new(AppThemePreference.Light, "Light"),
        new(AppThemePreference.Dark, "Dark"),
    ];

    [ObservableProperty]
    private bool _isHapticsEnabled;

    [ObservableProperty]
    private bool _isPro;

    /// <summary>The debug Pro toggle is compiled in only in DEBUG builds.</summary>
    public bool ShowDebugPro
#if DEBUG
        => true;
#else
        => false;
#endif

    public ValueTask OnAppearingAsync()
    {
        foreach (var option in ThemeOptions)
        {
            option.IsSelected = option.Preference == themeService.Preference;
        }

        IsHapticsEnabled = hapticService.IsEnabled;
        _isPro = userSession.IsPro;
        OnPropertyChanged(nameof(IsPro));
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

    // Manage favourites per category. The param is the PulseType name ("Mood"/"Need"/"Thought").
    [RelayCommand]
    private Task ManageFavorites(string category) =>
        navigationService.GoToAsync(Navigation.Relative().Push<ManageFavoritesViewModel>()
            .WithIntent(new ManageFavoritesIntent(Enum.Parse<Models.PulseType>(category))));

    [RelayCommand]
    private Task ManagePacks() =>
        navigationService.GoToAsync(Navigation.Relative().Push<ManagePacksViewModel>());

    // DEBUG-only Pro toggle. Calls the dev endpoint and refreshes the cached user so gating updates.
    partial void OnIsProChanged(bool value)
    {
        if (value == userSession.IsPro)
        {
            return; // programmatic sync from OnAppearing — not a user toggle.
        }

        _ = SetProAsync(value);
    }

    private async Task SetProAsync(bool value)
    {
        try
        {
            var user = await api.SetProAsync(value);
            userSession.Set(user);
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
            _isPro = userSession.IsPro;
            OnPropertyChanged(nameof(IsPro));
        }
    }
}
