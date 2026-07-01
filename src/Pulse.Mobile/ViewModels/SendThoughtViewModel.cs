using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;

namespace Pulse.ViewModels;

/// <summary>"Share a thought" — send a thought phrase.</summary>
public partial class SendThoughtViewModel(
    IPulseApiClient api,
    FavoritesSession favorites,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : SendPulseViewModel(api, favorites, navigationService, alerts, haptics)
{
    protected override PulseType Category => PulseType.Thought;
    public override string Title => "Share a thought";

    protected override Task SendToApiAsync(string text, string emoji, string? note) => Api.SendThoughtAsync(text, emoji, note);
}
