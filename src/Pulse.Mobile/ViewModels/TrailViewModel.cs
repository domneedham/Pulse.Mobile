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
    // Optional pulse note
    string? Note = null,
    // Thumbnail (completed photo / drawing moment)
    string? PhotoUrl = null,
    string? StrokeData = null)
{
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public bool IsMoment => Kind == TrailItemKind.Moment;
    public bool IsPulse => Kind == TrailItemKind.Pulse;

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
}

/// <summary>A date-grouped section of the Trail ("Today", "Yesterday", or a date).</summary>
public class TrailGroup(string title) : ObservableCollection<TrailRowVm>
{
    public string Title { get; } = title;
}

/// <summary>
/// The Trail tab — the couple's shared story: pulses and daily Moments interleaved into one
/// chronological, day-grouped timeline (the storyboard's "Your Trail"). Tapping a pulse opens its
/// detail; tapping a moment opens the moment detail / respond flow.
/// </summary>
public partial class TrailViewModel(
    IPulseApiClient api,
    UserSession userSession,
    ConnectionSession connectionSession,
    INavigationService navigationService,
    ILogger<TrailViewModel> logger) : ObservableObject, IAppearingAware
{
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
        finally
        {
            HasLoaded = true;
        }
    }

    [RelayCommand]
    private Task OpenMoments() =>
        navigationService.GoToAsync(Navigation.Relative().Push<MomentsViewModel>());

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
            return new TrailRowVm(
                TrailItemKind.Pulse, p.Id, p.Emoji, PulseDisplay.CategoryIcon(p.Type), p.Text, when, p.SentByMe, p.IsFavorite,
                CategoryLabel: string.Empty, MomentComplete: false, MomentStatus: string.Empty,
                PersonName: person, Note: p.Note);
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
