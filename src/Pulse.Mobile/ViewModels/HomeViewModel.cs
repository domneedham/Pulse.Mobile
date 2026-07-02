using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;
using Pulse.UI.Controls;

namespace Pulse.ViewModels;

/// <summary>The single latest pulse, rendered for the Home card. Icon = the native category icon (not the phrase emoji).</summary>
public record LatestPulseVm(Guid Id, PulseIcon Icon, string Text, string When, bool SentByMe);

/// <summary>Today's Moment, rendered for the Home "Today's Moment" card.</summary>
public record TodaysMomentVm(
    Guid Id,
    PulseIcon Icon,
    string CategoryLabel,
    string Title,
    string Prompt,
    string ActionLabel,
    bool CanRespond,
    // Two-avatar progress row
    string MyName,
    string? MyAvatarUrl,
    bool MyDone,
    string PartnerName,
    string? PartnerAvatarUrl,
    bool PartnerDone,
    // Each person's own response — the thumbnail slot always shows both (MomentThumbnailView falls
    // back to that person's initials on their colour when they haven't shared a photo/drawing yet).
    string? MyPhotoUrl,
    string? MyStrokeData,
    string? PartnerPhotoUrl,
    string? PartnerStrokeData);

/// <summary>
/// Home — quick-send (Mood / Need / Thought / Touch) up top, and below it the one latest pulse from
/// the connection (the "current signal"). When nothing's been sent yet, an empty state invites the
/// first pulse. The full history lives in the Pulses tab.
/// </summary>
public partial class HomeViewModel(
    IPulseApiClient api,
    UserSession userSession,
    ConnectionSession connectionSession,
    INavigationService navigationService,
    IAlertService alerts,
    ILogger<HomeViewModel> logger) : ObservableObject, IAppearingAware
{
    public string DisplayName => userSession.DisplayName;
    public string? AvatarUrl => userSession.AvatarUrl;

    public string PartnerName => connectionSession.Partner?.DisplayName ?? "your partner";
    public string? PartnerAvatarUrl => connectionSession.Partner?.AvatarUrl;

    /// <summary>Card header — Home shows the partner's latest signal, so it's always "Latest from {name}".</summary>
    public string LatestHeader => $"Latest from {PartnerName}";

    /// <summary>Card subtitle under the partner's name, e.g. "Last pulse received yesterday".</summary>
    public string LatestSubtitle => Latest is null
        ? "No pulses yet"
        : $"Last pulse received {Latest.When}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLatest))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(LatestSubtitle))]
    private LatestPulseVm? _latest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoment))]
    private TodaysMomentVm? _todaysMoment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasLoaded;

    [ObservableProperty]
    private bool _isRefreshing;

    public bool HasLatest => Latest is not null;
    public bool HasMoment => TodaysMoment is not null;
    public bool ShowEmptyState => HasLoaded && Latest is null;

    public async ValueTask OnAppearingAsync() => await LoadAsync();

    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var pulse = await api.GetLatestPulseAsync();
            if (pulse is null)
            {
                Latest = null;
            }
            else
            {
                Latest = new LatestPulseVm(pulse.Id, PulseDisplay.CategoryIcon(pulse.Type), pulse.Text, RelativeTime.Format(pulse.CreatedAt), pulse.SentByMe);
            }

            OnPropertyChanged(nameof(PartnerName));
            OnPropertyChanged(nameof(PartnerAvatarUrl));
            OnPropertyChanged(nameof(LatestHeader));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Home: failed to load latest pulse");
        }

        await LoadTodaysMomentAsync();

        HasLoaded = true;
    }

    private async Task LoadTodaysMomentAsync()
    {
        try
        {
            var m = await api.GetTodayMomentAsync();

            var myResponse = m.Responses.FirstOrDefault(r => r.SubmittedByMe);
            var partnerResponse = m.Responses.FirstOrDefault(r => !r.SubmittedByMe);

            TodaysMoment = new TodaysMomentVm(
                m.Id,
                MomentDisplay.CategoryIcon(m.Category),
                MomentDisplay.CategoryLabel(m.Category),
                m.Title,
                m.Prompt,
                MomentDisplay.ActionLabel(m.ResponseKind),
                CanRespond: !m.MyResponseSubmitted,
                MyName: DisplayName,
                MyAvatarUrl: AvatarUrl,
                MyDone: m.MyResponseSubmitted,
                PartnerName: PartnerName,
                PartnerAvatarUrl: PartnerAvatarUrl,
                PartnerDone: m.PartnerResponded,
                MyPhotoUrl: myResponse?.PhotoUrl,
                MyStrokeData: myResponse?.StrokeData,
                PartnerPhotoUrl: partnerResponse?.PhotoUrl,
                PartnerStrokeData: partnerResponse?.StrokeData);
        }
        catch (Exception ex)
        {
            // No connection yet, or the moments endpoint is unreachable — just hide the card.
            logger.LogError(ex, "Home: failed to load today's moment");
            TodaysMoment = null;
        }
    }

    [RelayCommand]
    private Task OpenMoment()
    {
        if (TodaysMoment is null)
        {
            return Task.CompletedTask;
        }

        return navigationService.GoToAsync(Navigation.Relative().Push<MomentDetailViewModel>()
            .WithIntent(new MomentDetailIntent(TodaysMoment.Id)));
    }

    [RelayCommand]
    private Task OpenLatest()
    {
        if (Latest is null)
        {
            return Task.CompletedTask;
        }

        return navigationService.GoToAsync(Navigation.Relative().Push<PulseDetailViewModel>()
            .WithIntent(new PulseDetailIntent(Latest.Id)));
    }

    [RelayCommand]
    private Task SeeAll() =>
        navigationService.GoToAsync(Navigation.Absolute(NavigationBehavior.Immediate).Root<TrailViewModel>());

    [RelayCommand]
    private Task OpenCompose() => navigationService.GoToAsync(Navigation.Relative().Push<ComposeSheetViewModel>());
}
