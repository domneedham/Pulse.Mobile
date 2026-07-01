using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;

namespace Pulse.ViewModels;

/// <summary>"What would help right now?" — send a need phrase.</summary>
public partial class SendNeedViewModel(
    IPulseApiClient api,
    FavoritesSession favorites,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : SendPulseViewModel(api, favorites, navigationService, alerts, haptics)
{
    protected override PulseType Category => PulseType.Need;
    public override string Title => "What would help?";

    protected override Task SendToApiAsync(string text, string emoji, string? note) => Api.SendNeedAsync(text, emoji, note);
}
