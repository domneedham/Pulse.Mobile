using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Models;
using Pulse.Services.Api;
using Pulse.UI;
using Pulse.UI.Controls;

namespace Pulse.ViewModels;

/// <summary>A row in the Moments list: title, when, status line, native type icon.</summary>
public record MomentRowVm(
    Guid Id,
    PulseIcon Icon,
    string Title,
    string When,
    string Status,
    bool IsComplete,
    bool IsFavorite);

/// <summary>A date-grouped section of the Moments list ("This week", "Earlier", or a month).</summary>
public class MomentGroup(string title) : ObservableCollection<MomentRowVm>
{
    public string Title { get; } = title;
}

/// <summary>
/// The Moments list — all of the couple's Moments (today + past), grouped by recency, with an
/// All / Favorites toggle. Tapping a row opens its detail. Reached from the Trail's calendar action.
/// </summary>
public partial class MomentsViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    ILogger<MomentsViewModel> logger) : ObservableObject, IAppearingAware
{
    public ObservableCollection<MomentGroup> Groups { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _hasLoaded;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFavoritesOnly))]
    [NotifyPropertyChangedFor(nameof(AllTab))]
    private bool _favoritesTab;

    public bool ShowFavoritesOnly => FavoritesTab;

    /// <summary>Inverse of <see cref="FavoritesTab"/> for the "All" pill fill (the ChipFill converter takes no param).</summary>
    public bool AllTab => !FavoritesTab;

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

    [RelayCommand]
    private async Task SelectAll()
    {
        if (!FavoritesTab)
        {
            return;
        }
        FavoritesTab = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SelectFavorites()
    {
        if (FavoritesTab)
        {
            return;
        }
        FavoritesTab = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var moments = await api.GetMomentsAsync(FavoritesTab);
            RebuildGroups(moments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Moments: failed to load");
        }
        finally
        {
            HasLoaded = true;
        }
    }

    [RelayCommand]
    private Task OpenDetail(MomentRowVm row) =>
        navigationService.GoToAsync(Navigation.Relative().Push<MomentDetailViewModel>()
            .WithIntent(new MomentDetailIntent(row.Id)));

    private void RebuildGroups(IReadOnlyList<Moment> moments)
    {
        Groups.Clear();

        var today = DateOnly.FromDateTime(DateTime.Now);
        var weekStart = today.AddDays(-6);

        foreach (var bySection in moments
            .GroupBy(m => SectionFor(m.Date, today, weekStart))
            .OrderBy(g => g.Key.Order))
        {
            var group = new MomentGroup(bySection.Key.Label);
            foreach (var m in bySection.OrderByDescending(m => m.Date))
            {
                group.Add(ToRow(m, today));
            }

            if (group.Count > 0)
            {
                Groups.Add(group);
            }
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private static MomentRowVm ToRow(Moment m, DateOnly today)
    {
        var status = m.IsComplete
            ? "Together"
            : m.MyResponseSubmitted
                ? "You completed"
                : m.PartnerResponded
                    ? "Your turn"
                    : "Not started";

        var when = m.Date == today
            ? "Today"
            : m.Date == today.AddDays(-1)
                ? "Yesterday"
                : m.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMM");

        return new MomentRowVm(m.Id, MomentDisplay.CategoryIcon(m.Category), m.Title, when, status, m.IsComplete, m.IsFavorite);
    }

    private static (int Order, string Label) SectionFor(DateOnly date, DateOnly today, DateOnly weekStart) =>
        date >= weekStart ? (0, "This week")
        : date >= today.AddMonths(-1) ? (1, "Earlier this month")
        : (2, "Earlier");
}
