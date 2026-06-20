namespace Pulse.Services;

/// <summary>Centralised, user-toggleable haptic feedback.</summary>
public class HapticService
{
    private const string PreferenceKey = "hapticsEnabled";

    public bool IsEnabled { get; private set; } = Preferences.Default.Get(PreferenceKey, true);

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        Preferences.Default.Set(PreferenceKey, enabled);
    }

    public void Tap()
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }
}
