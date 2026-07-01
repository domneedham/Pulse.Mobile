using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;

namespace Pulse.ViewModels;

/// <summary>"How are you feeling?" — send a mood phrase.</summary>
public partial class SendMoodViewModel(
    IPulseApiClient api,
    FavoritesSession favorites,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : SendPulseViewModel(api, favorites, navigationService, alerts, haptics)
{
    protected override PulseType Category => PulseType.Mood;
    public override string Title => "How are you feeling?";

    protected override Task SendToApiAsync(string text, string emoji, string? note) => Api.SendMoodAsync(text, emoji, note);
}
