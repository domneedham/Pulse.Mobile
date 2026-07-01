using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;

namespace Pulse.ViewModels;

public record FavoritesOnboardingIntent;

/// <summary>A selectable phrase option during onboarding (toggle to add/remove from favourites).</summary>
public partial class SelectableOption(string text, string emoji) : ObservableObject
{
    public string Text { get; } = text;
    public string Emoji { get; } = emoji;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// The post-signup favourites picker: three steps (feelings / needs / thoughts). Each step shows the
/// catalogue with the defaults pre-selected; Continue saves that category and advances. "Skip" keeps
/// the defaults for the remaining steps. After the last step it seeds any unset defaults and enters
/// the app.
/// </summary>
public partial class FavoritesOnboardingViewModel(
    IPulseApiClient api,
    UserSession userSession,
    ConnectionSession connectionSession,
    FavoritesSession favoritesSession,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject, IAppearingAware<FavoritesOnboardingIntent>
{
    private static readonly PulseType[] Steps = [PulseType.Mood, PulseType.Need, PulseType.Thought];

    [ObservableProperty]
    private int _stepIndex;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _continueLabel = "Continue";

    public ObservableCollection<SelectableOption> Options { get; } = [];

    private PulseType CurrentCategory => Steps[StepIndex];

    public async ValueTask OnAppearingAsync(FavoritesOnboardingIntent intent) => await LoadStepAsync();

    private async Task LoadStepAsync()
    {
        IsBusy = true;
        try
        {
            (Title, Subtitle) = CurrentCategory switch
            {
                PulseType.Mood => ("What feelings do you commonly share?", "Pick a few that feel like you."),
                PulseType.Need => ("What would help when you're having a hard day?", "Pick the ones that matter."),
                _ => ("Any thoughts you always say?", "Pick your go-to phrases.")
            };
            ContinueLabel = StepIndex == Steps.Length - 1 ? "All set" : "Continue";

            Options.Clear();
            var catalog = await api.GetFavoriteCatalogAsync(CurrentCategory);
            // Pre-select the first five (the defaults), matching what a skip would choose.
            var i = 0;
            foreach (var o in catalog)
            {
                Options.Add(new SelectableOption(o.Text, o.Emoji) { IsSelected = i++ < 5 });
            }
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

    [RelayCommand]
    private void Toggle(SelectableOption option)
    {
        if (option is not null)
        {
            option.IsSelected = !option.IsSelected;
        }
    }

    [RelayCommand]
    private async Task Continue()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var items = Options.Where(o => o.IsSelected)
                .Select(o => new FavoriteItem(o.Text, o.Emoji))
                .ToList();

            // Save this category (even if empty — the user deliberately picked none).
            await api.SetFavoritesAsync(CurrentCategory, items);
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
            IsBusy = false;
            return;
        }

        if (StepIndex < Steps.Length - 1)
        {
            StepIndex++;
            await LoadStepAsync();
            return;
        }

        await FinishAsync();
    }

    /// <summary>Skip the rest — seed defaults for any category not yet set, then enter the app.</summary>
    [RelayCommand]
    private async Task Skip() => await FinishAsync(seedDefaults: true);

    private async Task FinishAsync(bool seedDefaults = false)
    {
        IsBusy = true;
        try
        {
            if (seedDefaults)
            {
                await api.EnsureDefaultFavoritesAsync();
            }

            await favoritesSession.LoadAsync();
        }
        catch
        {
            // Best-effort; the sheets can reload favourites later.
        }
        finally
        {
            IsBusy = false;
        }

        await AuthRouting.GoToRootForStateAsync(connectionSession, navigationService);
    }
}
