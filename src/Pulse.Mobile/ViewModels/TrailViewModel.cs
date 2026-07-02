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
/// switches the layout. Pulse fields mirror the pulse row; moment fields carry the big moment-card data
/// (the same layout for every moment regardless of day — today's incomplete moment just naturally sorts
/// to the top of the "Today" group, and moves down the trail like any other day once it's done).
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
    // Person (pulse sender) — drives the node colour so each partner reads consistently
    string PersonName = "",
    string? PersonAvatarUrl = null,
    // Optional pulse note
    string? Note = null,
    // Thumbnail (completed photo / drawing moment) and inline pulse drawing (PulseTouch)
    string? PhotoUrl = null,
    string? StrokeData = null,
    // Moment big-card fields (title/prompt reuse Text/CategoryLabel is the pill; ActionLabel/CanRespond
    // drive the action button; the two-avatar progress row + each person's own thumbnail)
    string? Prompt = null,
    string? ActionLabel = null,
    bool CanRespond = false,
    string MyName = "",
    string? MyAvatarUrl = null,
    bool MyDone = false,
    string PartnerName = "",
    string? PartnerAvatarUrl = null,
    bool PartnerDone = false,
    string? MyPhotoUrl = null,
    string? MyStrokeData = null,
    string? PartnerPhotoUrl = null,
    string? PartnerStrokeData = null)
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

/// <summary>
/// The Trail — the app's home page. Every Moment (today's and past ones alike) renders as the same big
/// card inline in the day-grouped timeline alongside pulses; today's naturally sorts to the top since
/// "Today" is always the first group, and once it's answered it's just another (still big-card) entry
/// that moves down the trail as new days are added above it. Tapping a pulse opens its detail; tapping a
/// moment opens the moment detail / respond flow.
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
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _hasLoaded;

    [ObservableProperty]
    private bool _isRefreshing;

    public bool IsEmpty => HasLoaded && Groups.Count == 0;

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

        HasLoaded = true;
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
                CategoryLabel: PulseDisplay.CategoryLabel(p.Type),
                PersonName: person, PersonAvatarUrl: avatar, Note: p.Note, StrokeData: p.StrokeData);
        }

        if (item is { Kind: TrailItemKind.Moment, Moment: { } m })
        {
            var myResponse = m.Responses.FirstOrDefault(r => r.SubmittedByMe);
            var partnerResponse = m.Responses.FirstOrDefault(r => !r.SubmittedByMe);

            return new TrailRowVm(
                TrailItemKind.Moment, m.Id, m.Emoji, MomentDisplay.CategoryIcon(m.Category), m.Title,
                When: MomentDisplay.CategoryLabel(m.Category),
                SentByMe: false, IsFavorite: false,
                CategoryLabel: MomentDisplay.CategoryLabel(m.Category),
                Prompt: m.Prompt,
                ActionLabel: MomentDisplay.ActionLabel(m.ResponseKind),
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

        return null;
    }

    private static string DayLabel(DateTime day, DateTime today, DateTime yesterday) =>
        day == today ? "Today"
        : day == yesterday ? "Yesterday"
        : day.Year == today.Year ? day.ToString("MMMM d")
        : day.ToString("MMMM d, yyyy");
}
