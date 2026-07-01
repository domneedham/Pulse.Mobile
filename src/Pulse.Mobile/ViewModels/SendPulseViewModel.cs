using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>A selectable phrase row in a send sheet (favourite or catalogue option).</summary>
public record PhraseRow(string Text, string Emoji);

/// <summary>
/// Shared logic for the Mood / Need / Thought send sheets. Two steps: first pick a phrase (favourite,
/// catalogue option, or custom), then an optional note step (≤ 80 chars) before sending. Picking a phrase
/// moves to the note step; Send posts the pulse (with the optional note) and closes the sheet.
/// </summary>
public abstract partial class SendPulseViewModel(
    IPulseApiClient api,
    FavoritesSession favorites,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IAppearingAware
{
    /// <summary>Max length of the optional note attached to a signal (matches the API cap).</summary>
    public const int NoteMaxLength = 80;

    protected abstract PulseType Category { get; }
    public abstract string Title { get; }

    /// <summary>Whether this category offers a free-text custom entry (all do for now).</summary>
    public virtual bool AllowsCustom => true;

    public ObservableCollection<PhraseRow> Favorites { get; } = [];
    public ObservableCollection<PhraseRow> MoreOptions { get; } = [];

    public bool HasMoreOptions => MoreOptions.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueWithCustomCommand))]
    private string _customText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    // --- Note step ---
    // When a phrase is picked the sheet switches from the picker to the note step.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPicker))]
    private bool _isNoteStep;

    [ObservableProperty]
    private string _selectedEmoji = string.Empty;

    [ObservableProperty]
    private string _selectedText = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    /// <summary>The picker shows while not on the note step.</summary>
    public bool ShowPicker => !IsNoteStep;

    public async ValueTask OnAppearingAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        Favorites.Clear();
        foreach (var f in favorites.For(Category))
        {
            Favorites.Add(new PhraseRow(f.Text, f.Emoji));
        }

        MoreOptions.Clear();
        try
        {
            var catalog = await api.GetFavoriteCatalogAsync(Category);
            var favTexts = Favorites.Select(f => f.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var o in catalog.Where(o => !favTexts.Contains(o.Text)))
            {
                MoreOptions.Add(new PhraseRow(o.Text, o.Emoji));
            }
        }
        catch
        {
            // Catalogue is best-effort; favourites alone are enough to send.
        }

        OnPropertyChanged(nameof(HasMoreOptions));
    }

    // --- Step 1: pick a phrase → move to the note step ---

    [RelayCommand]
    private void Pick(PhraseRow row)
    {
        if (row is not null)
        {
            EnterNoteStep(row.Text, row.Emoji);
        }
    }

    private bool CanContinueWithCustom() => !string.IsNullOrWhiteSpace(CustomText);

    [RelayCommand(CanExecute = nameof(CanContinueWithCustom))]
    private void ContinueWithCustom() =>
        EnterNoteStep(CustomText.Trim(), PulseDisplay.DefaultEmoji(Category));

    private void EnterNoteStep(string text, string emoji)
    {
        SelectedText = text;
        SelectedEmoji = emoji;
        Note = string.Empty;
        IsNoteStep = true;
    }

    [RelayCommand]
    private void BackToPicker() => IsNoteStep = false;

    // --- Step 2: send with the optional note ---

    [RelayCommand]
    private async Task Send()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(SelectedText))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            await SendToApiAsync(SelectedText, SelectedEmoji, note);
            haptics.Tap();
            await alerts.ShowToastAsync("Pulse sent 💗");
            await navigationService.GoToAsync(Navigation.Relative().Pop());
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

    /// <summary>Post the pulse to the category-specific endpoint, with an optional note.</summary>
    protected abstract Task SendToApiAsync(string text, string emoji, string? note);

    // Exposed to subclasses for the concrete send calls.
    protected IPulseApiClient Api => api;

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}
