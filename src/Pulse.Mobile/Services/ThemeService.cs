using Pulse.Resources.Themes;

namespace Pulse.Services;

public enum AppThemePreference
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    AppThemePreference Preference { get; }

    /// <summary>Applies the saved preference; call once after the app's resources are loaded.</summary>
    void Initialise();

    void SetPreference(AppThemePreference preference);
}

/// <summary>
/// Swaps the LightTheme/DarkTheme resource dictionaries at runtime. All theme-aware
/// styles consume colours via DynamicResource, so a swap repaints the whole app.
/// </summary>
public class ThemeService : IThemeService
{
    private const string PreferenceKey = "themePreference";

    public AppThemePreference Preference { get; private set; }

    public void Initialise()
    {
        Preference = Enum.TryParse(
            Preferences.Default.Get(PreferenceKey, nameof(AppThemePreference.System)),
            out AppThemePreference saved)
            ? saved
            : AppThemePreference.System;

        if (Application.Current is { } app)
        {
            app.RequestedThemeChanged += (_, _) =>
            {
                if (Preference == AppThemePreference.System)
                {
                    Apply();
                }
            };
        }

        Apply();
    }

    public void SetPreference(AppThemePreference preference)
    {
        Preference = preference;
        Preferences.Default.Set(PreferenceKey, preference.ToString());
        Apply();
    }

    private void Apply()
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        bool dark = Preference == AppThemePreference.Dark
            || (Preference == AppThemePreference.System && app.RequestedTheme == AppTheme.Dark);

        var dictionaries = app.Resources.MergedDictionaries;
        var current = dictionaries.FirstOrDefault(d => d is LightTheme or DarkTheme);

        if ((dark && current is DarkTheme) || (!dark && current is LightTheme))
        {
            ApplyUserAppTheme(app, dark);
            return;
        }

        if (current is not null)
        {
            dictionaries.Remove(current);
        }

        dictionaries.Add(dark ? new DarkTheme() : new LightTheme());
        ApplyUserAppTheme(app, dark);
    }

    /// <summary>Keeps native chrome (keyboards, alerts, status bar) in step with the app theme.</summary>
    private void ApplyUserAppTheme(Application app, bool dark) =>
        app.UserAppTheme = Preference == AppThemePreference.System
            ? AppTheme.Unspecified
            : dark ? AppTheme.Dark : AppTheme.Light;
}
