using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>A selectable phrase row in the compose sheet (favourite or catalogue option).</summary>
public record PhraseRow(string Text);

/// <summary>Presets the compose sheet to open straight at a category's picker step (e.g. a quick
/// "reply in kind" from a pulse's detail page), skipping the category-icon grid.</summary>
public record ComposeSheetIntent(PulseType? PresetCategory = null);

/// <summary>Which screen the compose sheet is currently showing.</summary>
public enum ComposeStep
{
    /// <summary>Icon grid: Mood / Thought / Need / Touch.</summary>
    Categories,

    /// <summary>Favourites + catalogue + custom entry for the chosen category.</summary>
    Picker,

    /// <summary>Optional note before sending the chosen phrase.</summary>
    Note
}

/// <summary>
/// The global "send a signal" sheet: one continuous popup that walks Categories → Picker → Note,
/// with a back arrow between steps, rather than closing and reopening a separate page per category
/// (that felt like two popups). Touch keeps its own dedicated screen (a drawing canvas, not a phrase
/// list) and is still pushed as a separate page from the Categories step.
/// </summary>
public partial class ComposeSheetViewModel(
    IPulseApiClient api,
    FavoritesSession favorites,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<ComposeSheetIntent>
{
    public const int NoteMaxLength = 80;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategoriesStep))]
    [NotifyPropertyChangedFor(nameof(IsPickerStep))]
    [NotifyPropertyChangedFor(nameof(IsNoteStep))]
    private ComposeStep _step = ComposeStep.Categories;

    public bool IsCategoriesStep => Step == ComposeStep.Categories;
    public bool IsPickerStep => Step == ComposeStep.Picker;
    public bool IsNoteStep => Step == ComposeStep.Note;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PickerTitle))]
    [NotifyPropertyChangedFor(nameof(PickerIcon))]
    private PulseType _category = PulseType.Mood;

    public ObservableCollection<PhraseRow> Favorites { get; } = [];
    public ObservableCollection<PhraseRow> MoreOptions { get; } = [];

    public bool HasMoreOptions => MoreOptions.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueWithCustomCommand))]
    private string _customText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _selectedText = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    /// <summary>Picker-step header — kept short so it stays on one line like the other steps.</summary>
    public string PickerTitle => Category switch
    {
        PulseType.Mood => "Your mood?",
        PulseType.Thought => "Share a thought",
        PulseType.Need => "What would help?",
        _ => "Send a signal"
    };

    public PulseIcon PickerIcon => PulseDisplay.CategoryIcon(Category);

    public async ValueTask OnEnteringAsync(ComposeSheetIntent intent)
    {
        if (intent.PresetCategory is { } preset)
        {
            await SelectCategoryAsync(preset);
        }
    }

    // --- Step 1: category grid ---

    [RelayCommand]
    private Task PickMood() => SelectCategoryAsync(PulseType.Mood);

    [RelayCommand]
    private Task PickThought() => SelectCategoryAsync(PulseType.Thought);

    [RelayCommand]
    private Task PickNeed() => SelectCategoryAsync(PulseType.Need);

    [RelayCommand]
    private Task PickTouch() => navigationService.GoToAsync(Navigation.Relative().Push<SendTouchViewModel>());

    private async Task SelectCategoryAsync(PulseType category)
    {
        Category = category;
        CustomText = string.Empty;
        await LoadPhrasesAsync();
        Step = ComposeStep.Picker;
    }

    private async Task LoadPhrasesAsync()
    {
        Favorites.Clear();
        foreach (var f in favorites.For(Category))
        {
            Favorites.Add(new PhraseRow(f.Text));
        }

        MoreOptions.Clear();
        try
        {
            var catalog = await api.GetFavoriteCatalogAsync(Category);
            var favTexts = Favorites.Select(f => f.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var o in catalog.Where(o => !favTexts.Contains(o.Text)))
            {
                MoreOptions.Add(new PhraseRow(o.Text));
            }
        }
        catch
        {
            // Catalogue is best-effort; favourites alone are enough to send.
        }

        OnPropertyChanged(nameof(HasMoreOptions));
    }

    // --- Step 2: pick a phrase → move to the note step ---

    [RelayCommand]
    private void Pick(PhraseRow row)
    {
        if (row is not null)
        {
            EnterNoteStep(row.Text);
        }
    }

    private bool CanContinueWithCustom() => !string.IsNullOrWhiteSpace(CustomText);

    [RelayCommand(CanExecute = nameof(CanContinueWithCustom))]
    private void ContinueWithCustom() => EnterNoteStep(CustomText.Trim());

    private void EnterNoteStep(string text)
    {
        SelectedText = text;
        Note = string.Empty;
        Step = ComposeStep.Note;
    }

    [RelayCommand]
    private void BackToPicker() => Step = ComposeStep.Picker;

    [RelayCommand]
    private void BackToCategories() => Step = ComposeStep.Categories;

    // --- Step 3: send with the optional note ---

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
            var emoji = PulseDisplay.DefaultEmoji(Category);
            await (Category switch
            {
                PulseType.Mood => api.SendMoodAsync(SelectedText, emoji, note),
                PulseType.Need => api.SendNeedAsync(SelectedText, emoji, note),
                _ => api.SendThoughtAsync(SelectedText, emoji, note)
            });
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

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}
