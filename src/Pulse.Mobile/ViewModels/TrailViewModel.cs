using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;
using Pulse.UI.Controls;

namespace Pulse.ViewModels;

/// <summary>
/// A row on the Trail — either a pulse or a moment. One template renders both; <see cref="IsMoment"/>
/// switches the layout. Pulse fields mirror the pulse row; moment fields carry the card data.
/// </summary>
public record TrailRowVm(
    TrailItemKind Kind,
    Guid Id,
    string Emoji,
    PulseIcon Icon,
    string Text,
    string When,
    bool SentByMe,
    bool IsFavorite,
    // Moment-only
    string CategoryLabel,
    bool MomentComplete,
    string MomentStatus,
    // Person (pulse sender) — drives the node colour so each partner reads consistently
    string PersonName = "",
    string? PersonAvatarUrl = null,
    // Optional pulse note
    string? Note = null,
    // Thumbnail (completed photo / drawing moment) and inline pulse drawing (PulseTouch)
    string? PhotoUrl = null,
    string? StrokeData = null)
{
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public bool IsMoment => Kind == TrailItemKind.Moment;
    public bool IsPulse => Kind == TrailItemKind.Pulse;

    /// <summary>Sent-by-me pulses read "You" on the row; moments have no attribution row.</summary>
    public string PersonLabel => SentByMe ? "You" : PersonName;

    /// <summary>Pulse nodes/badges take the sender's person colour; moments use the shared accent (purple).</summary>
    public Color NodeColor => IsMoment ? AccentColor : PersonColors.Foreground(PersonName);
    public Color BadgeTint => IsMoment ? AccentSoftColor : PersonColors.Background(PersonName);

    private static Color AccentColor => Resolve("Accent", Color.FromArgb("#A98BFF"));
    private static Color AccentSoftColor => Resolve("AccentSoft", Color.FromArgb("#ECE5FF"));

    private static Color Resolve(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : fallback;

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoUrl);
    public bool HasDrawing => !string.IsNullOrEmpty(StrokeData);
    public bool HasThumbnail => HasPhoto || HasDrawing;

    /// <summary>Pulse-only: a Touch pulse renders its stroke drawing inline instead of the text line.</summary>
    public bool IsPulseDrawing => IsPulse && HasDrawing;
}

/// <summary>A date-grouped section of the Trail ("Today", "Yesterday", or a date).</summary>
public class TrailGroup(string title) : ObservableCollection<TrailRowVm>
{
    public string Title { get; } = title;
}

/// <summary>Today's Moment, pinned above the Trail timeline — the one thing that needs completing.</summary>
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
/// The Trail — the app's home page. Today's Moment is pinned at the top (today naturally sorts first
/// in the day-grouped timeline anyway); pulses and daily Moments already sent/completed scroll below,
/// interleaved into one chronological, day-grouped timeline. Tapping a pulse opens its detail; tapping
/// a moment opens the moment detail / respond flow.
/// </summary>
public partial class TrailViewModel(
    IPulseApiClient api,
    UserSession userSession,
    ConnectionSession connectionSession,
    INavigationService navigationService,
    ILogger<TrailViewModel> logger) : ObservableObject, IAppearingAware
{
    public string DisplayName => userSession.DisplayName;
    public string? AvatarUrl => userSession.AvatarUrl;
    public string PartnerName => connectionSession.Partner?.DisplayName ?? "your partner";
    public string? PartnerAvatarUrl => connectionSession.Partner?.AvatarUrl;

    public ObservableCollection<TrailGroup> Groups { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoment))]
    private TodaysMomentVm? _todaysMoment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _hasLoaded;

    [ObservableProperty]
    private bool _isRefreshing;

    public bool HasMoment => TodaysMoment is not null;
    public bool IsEmpty => HasLoaded && Groups.Count == 0 && TodaysMoment is null;

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
            var items = await api.GetTrailAsync(limit: 200);
            RebuildGroups(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trail: failed to load");
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
            logger.LogError(ex, "Trail: failed to load today's moment");
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
    private Task OpenMoments() =>
        navigationService.GoToAsync(Navigation.Relative().Push<MomentsViewModel>());

    [RelayCommand]
    private Task OpenCompose() =>
        navigationService.GoToAsync(Navigation.Relative().Push<ComposeSheetViewModel>());

    [RelayCommand]
    private Task OpenItem(TrailRowVm row) => row.Kind switch
    {
        TrailItemKind.Moment => navigationService.GoToAsync(Navigation.Relative().Push<MomentDetailViewModel>()
            .WithIntent(new MomentDetailIntent(row.Id))),
        _ => navigationService.GoToAsync(Navigation.Relative().Push<PulseDetailViewModel>()
            .WithIntent(new PulseDetailIntent(row.Id)))
    };

    private void RebuildGroups(IReadOnlyList<TrailItem> items)
    {
        Groups.Clear();

        var today = DateTimeOffset.Now.Date;
        var yesterday = today.AddDays(-1);

        foreach (var byDay in items
            .GroupBy(i => i.Timestamp.ToLocalTime().Date)
            .OrderByDescending(g => g.Key))
        {
            var group = new TrailGroup(DayLabel(byDay.Key, today, yesterday));
            foreach (var item in byDay.OrderByDescending(i => i.Timestamp))
            {
                if (ToRow(item) is { } row)
                {
                    group.Add(row);
                }
            }

            if (group.Count > 0)
            {
                Groups.Add(group);
            }
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private TrailRowVm? ToRow(TrailItem item)
    {
        var when = item.Timestamp.ToLocalTime().ToString("h:mm tt");

        if (item is { Kind: TrailItemKind.Pulse, Pulse: { } p })
        {
            var person = p.SentByMe
                ? userSession.DisplayName
                : connectionSession.Partner?.DisplayName ?? "Partner";
            var avatar = p.SentByMe ? AvatarUrl : PartnerAvatarUrl;
            return new TrailRowVm(
                TrailItemKind.Pulse, p.Id, p.Emoji, PulseDisplay.CategoryIcon(p.Type), p.Text, when, p.SentByMe, p.IsFavorite,
                CategoryLabel: PulseDisplay.CategoryLabel(p.Type), MomentComplete: false, MomentStatus: string.Empty,
                PersonName: person, PersonAvatarUrl: avatar, Note: p.Note, StrokeData: p.StrokeData);
        }

        if (item is { Kind: TrailItemKind.Moment, Moment: { } m })
        {
            var status = m.IsComplete
                ? "Together, today"
                : m.MyResponseSubmitted
                    ? "Waiting for your partner"
                    : m.PartnerResponded
                        ? "Your partner answered — your turn"
                        : "Tap to start";

            // Thumbnail comes from a revealed response (only present once completed). Prefer a photo.
            var photo = m.Responses.FirstOrDefault(r => !string.IsNullOrEmpty(r.PhotoUrl))?.PhotoUrl;
            var drawing = photo is null
                ? m.Responses.FirstOrDefault(r => !string.IsNullOrEmpty(r.StrokeData))?.StrokeData
                : null;

            return new TrailRowVm(
                TrailItemKind.Moment, m.Id, m.Emoji, MomentDisplay.CategoryIcon(m.Category), m.Title,
                When: MomentDisplay.CategoryLabel(m.Category),
                SentByMe: false, IsFavorite: false,
                CategoryLabel: MomentDisplay.CategoryLabel(m.Category),
                MomentComplete: m.IsComplete,
                MomentStatus: status,
                PhotoUrl: photo,
                StrokeData: drawing);
        }

        return null;
    }

    private static string DayLabel(DateTime day, DateTime today, DateTime yesterday) =>
        day == today ? "Today"
        : day == yesterday ? "Yesterday"
        : day.Year == today.Year ? day.ToString("MMMM d")
        : day.ToString("MMMM d, yyyy");
}
