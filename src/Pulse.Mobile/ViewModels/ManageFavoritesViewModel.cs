using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

public record ManageFavoritesIntent(PulseType Category);

/// <summary>
/// Manage the favourites for one category: see them in order, remove, or add (a catalogue option or a
/// custom phrase). Changes save immediately and refresh the shared <see cref="FavoritesSession"/>.
/// </summary>
public partial class ManageFavoritesViewModel(
    IPulseApiClient api,
    FavoritesSession favoritesSession,
    IAlertService alerts) : ObservableObject, IEnteringAware<ManageFavoritesIntent>
{
    private PulseType _category;

    [ObservableProperty]
    private string _title = "Favourites";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomCommand))]
    private string _newText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<Favorite> Items { get; } = [];

    public async ValueTask OnEnteringAsync(ManageFavoritesIntent intent)
    {
        _category = intent.Category;
        Title = $"Favourite {PulseDisplay.CategoryLabel(_category)}s";
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            await favoritesSession.LoadAsync();
        }
        catch
        {
            // keep whatever's cached
        }

        Items.Clear();
        foreach (var f in favoritesSession.For(_category))
        {
            Items.Add(f);
        }
    }

    [RelayCommand]
    private async Task Remove(Favorite favorite)
    {
        if (favorite is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await api.DeleteFavoriteAsync(favorite.Id);
            Items.Remove(favorite);
            favoritesSession.Set(await api.GetFavoriteListAsync());
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAddCustom() => !IsBusy && !string.IsNullOrWhiteSpace(NewText);

    [RelayCommand(CanExecute = nameof(CanAddCustom))]
    private async Task AddCustom()
    {
        IsBusy = true;
        try
        {
            var added = await api.AddFavoriteAsync(_category, NewText.Trim(), PulseDisplay.DefaultEmoji(_category));
            Items.Add(added);
            NewText = string.Empty;
            favoritesSession.Set(await api.GetFavoriteListAsync());
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
