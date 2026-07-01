using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;

namespace Pulse.ViewModels;

/// <summary>A pack row in the selection list — selectable when unlocked, lock badge + upgrade prompt otherwise.</summary>
public partial class PackVm(Pack pack) : ObservableObject
{
    public Guid Id { get; } = pack.Id;
    public string Title { get; } = pack.Title;
    public string Emoji { get; } = pack.Emoji;
    public bool IsPro { get; } = pack.IsPro;
    public bool Locked { get; } = pack.Locked;
    public bool IsCore { get; } = pack.Key == "core";
    public string Subtitle { get; } = $"{pack.TemplateCount} moments";

    /// <summary>Core is always on and can't be toggled off; locked packs can't be toggled at all.</summary>
    public bool CanToggle => !IsCore && !Locked;

    [ObservableProperty]
    private bool _selected = pack.Selected;
}

/// <summary>
/// "Moment packs" — choose which packs feed the couple's daily Moment. Core is always on. Pro packs
/// show a lock for non-Pro couples; tapping a locked pack opens the upgrade prompt. Selection is saved
/// to the connection and recomputes tomorrow's (peeked) Moment server-side.
/// </summary>
public partial class ManagePacksViewModel(
    IPulseApiClient api,
    UserSession userSession,
    IAlertService alerts,
    HapticService haptics,
    ILogger<ManagePacksViewModel> logger) : ObservableObject, IAppearingAware
{
    public ObservableCollection<PackVm> Packs { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUpgradeBanner))]
    private bool _isPro;

    /// <summary>Non-Pro couples see a banner explaining Pro packs.</summary>
    public bool ShowUpgradeBanner => !IsPro;

    public async ValueTask OnAppearingAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsPro = userSession.IsPro;
        try
        {
            var packs = await api.GetPacksAsync();
            Packs.Clear();
            foreach (var p in packs)
            {
                Packs.Add(new PackVm(p));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Packs: failed to load");
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private async Task Toggle(PackVm pack)
    {
        if (pack.Locked)
        {
            await PromptUpgradeAsync();
            return;
        }

        if (!pack.CanToggle)
        {
            return; // Core — always on.
        }

        pack.Selected = !pack.Selected;
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            // Send every selected non-Core pack id; the server treats Core as implicit.
            var ids = Packs.Where(p => p.Selected && !p.IsCore).Select(p => p.Id).ToList();
            var refreshed = await api.SetPacksAsync(ids);

            // Re-sync from the server's authoritative view (e.g. if a pack got rejected).
            Packs.Clear();
            foreach (var p in refreshed)
            {
                Packs.Add(new PackVm(p));
            }

            haptics.Tap();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Packs: failed to save selection");
            await alerts.ShowErrorAsync(ex);
            await LoadAsync(); // revert optimistic toggle
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PromptUpgradeAsync()
    {
        var message = "This is a Pulse Pro pack. Upgrade to add premium packs to your daily Moments.";
#if DEBUG
        var upgrade = await alerts.ConfirmAsync("Pulse Pro", message + "\n\n(Debug) Enable Pro now?", "Enable Pro");
        if (upgrade)
        {
            try
            {
                var user = await api.SetProAsync(true);
                userSession.Set(user);
                await LoadAsync();
                await alerts.ShowToastAsync("Pro enabled ✨");
            }
            catch (Exception ex)
            {
                await alerts.ShowErrorAsync(ex);
            }
        }
#else
        await alerts.ShowAsync("Pulse Pro", message);
#endif
    }
}
