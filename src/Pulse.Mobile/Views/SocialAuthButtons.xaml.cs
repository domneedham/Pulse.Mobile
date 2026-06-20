using Microsoft.Extensions.DependencyInjection;
using Pulse.Services;

namespace Pulse.Views;

/// <summary>
/// The shared "Continue with Apple / Continue with Google" block used on sign-up and sign-in.
/// Social sign-in isn't backed yet, so both buttons pop a "coming soon" note instead of starting
/// an OAuth flow.
/// </summary>
public partial class SocialAuthButtons : ContentView
{
    public SocialAuthButtons() => InitializeComponent();

    private IServiceProvider? Services =>
        Shell.Current?.Handler?.MauiContext?.Services;

    private async void OnComingSoonTapped(object? sender, EventArgs e)
    {
        if (Services?.GetService<IAlertService>() is { } alerts)
        {
            await alerts.ShowAsync(
                "Coming soon",
                "Social sign-in is on the way. For now, use your email and password.");
        }
    }
}
