using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;
using Pulse.UI.Controls;

namespace Pulse.ViewModels;

public record MomentDetailIntent(Guid MomentId);

/// <summary>A revealed response on the detail screen: whose it is + its content (text / drawing / photo).</summary>
public record MomentResponseVm(
    string Author,
    string? AvatarUrl,
    string When,
    MomentResponseKind Kind,
    string? Text,
    string? Emoji,
    string? StrokeData,
    string? PhotoUrl,
    string? VoiceUrl = null,
    string? ChoiceLabel = null)
{
    /// <summary>A pale version of this person's colour for their response card; the full-strength avatar pops against it.</summary>
    public Color PersonTint => PersonColors.CardTint(Author);
    public Color PersonAccent => PersonColors.Foreground(Author);

    public bool IsText => Kind == MomentResponseKind.Text;
    public bool IsDrawing => Kind == MomentResponseKind.Drawing;
    public bool IsPhoto => Kind == MomentResponseKind.Photo;
    public bool IsVoice => Kind == MomentResponseKind.Voice;
    public bool IsChoice => Kind == MomentResponseKind.Choice;
    public bool HasPhoto => IsPhoto && !string.IsNullOrEmpty(PhotoUrl);
    public bool HasVoice => IsVoice && !string.IsNullOrEmpty(VoiceUrl);
}

/// <summary>
/// Moment detail — the prompt, the response action (or progress), and once both have answered the
/// revealed responses side by side ("Completed together ❤️"). The respond sheet is chosen by the
/// Moment's response kind; on return the detail reloads to reflect the new state.
/// </summary>
public partial class MomentDetailViewModel(
    IPulseApiClient api,
    ConnectionSession connectionSession,
    UserSession userSession,
    INavigationService navigationService,
    IAlertService alerts) : ObservableObject, IAppearingAware<MomentDetailIntent>, IAppearingAware
{
    private Guid _momentId;
    private Moment? _moment;

    public ObservableCollection<MomentResponseVm> Responses { get; } = [];

    [ObservableProperty]
    private PulseIcon _icon;

    [ObservableProperty]
    private string _emoji = string.Empty;

    [ObservableProperty]
    private string _categoryLabel = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private string _dateLabel = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAction))]
    private bool _canRespond;

    [ObservableProperty]
    private string _actionLabel = "Start Moment";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStatus))]
    private string _statusText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowResponses))]
    private bool _isComplete;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteGlyph))]
    private bool _isFavorite;

    /// <summary>Filled star when favourited, outline otherwise (toolbar toggle).</summary>
    public string FavoriteGlyph => IsFavorite ? Resources.MdiIcons.Star : Resources.MdiIcons.StarOutline;

    public bool ShowAction => CanRespond;
    public bool ShowStatus => !CanRespond && !IsComplete && !string.IsNullOrEmpty(StatusText);
    public bool ShowResponses => IsComplete && Responses.Count > 0;

    // The intent fires on first push; the non-generic appear fires on every return (e.g. back from the
    // respond sheet) so we reload to pick up a freshly-submitted response.
    public async ValueTask OnAppearingAsync(MomentDetailIntent intent)
    {
        _momentId = intent.MomentId;
        await LoadAsync();
    }

    public async ValueTask OnAppearingAsync()
    {
        if (_momentId != Guid.Empty)
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _moment = await api.GetMomentAsync(_momentId);
            Bind(_moment);
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
            await navigationService.GoToAsync(Navigation.Relative().Pop());
        }
    }

    private void Bind(Moment m)
    {
        Icon = MomentDisplay.CategoryIcon(m.Category);
        Emoji = m.Emoji;
        CategoryLabel = MomentDisplay.CategoryLabel(m.Category);
        Title = m.Title;
        Prompt = m.Prompt;
        DateLabel = m.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMM yyyy");

        CanRespond = !m.MyResponseSubmitted;
        ActionLabel = MomentDisplay.ActionLabel(m.ResponseKind);
        IsComplete = m.IsComplete;
        IsFavorite = m.IsFavorite;

        StatusText = m.IsComplete
            ? "Together, today"
            : m.MyResponseSubmitted
                ? "You've answered — waiting for your partner."
                : m.PartnerResponded
                    ? "Your partner has answered. Your turn — then you'll both see each other's."
                    : string.Empty;

        var partnerName = connectionSession.Partner?.DisplayName ?? "Partner";
        var partnerAvatar = connectionSession.Partner?.AvatarUrl;
        Responses.Clear();
        foreach (var r in m.Responses)
        {
            var author = r.SubmittedByMe ? "You" : partnerName;
            var avatar = r.SubmittedByMe ? userSession.AvatarUrl : partnerAvatar;
            // Resolve a choice pick to its option label from the template options.
            string? choiceLabel = null;
            if (r.Kind == MomentResponseKind.Choice && r.ChoiceIndex is { } idx
                && m.Options is { } opts && idx >= 0 && idx < opts.Count)
            {
                choiceLabel = opts[idx];
            }

            Responses.Add(new MomentResponseVm(
                author,
                avatar,
                r.CreatedAt.ToLocalTime().ToString("h:mm tt"),
                r.Kind,
                r.Text,
                r.Emoji,
                r.StrokeData,
                r.PhotoUrl,
                r.VoiceUrl,
                choiceLabel));
        }

        OnPropertyChanged(nameof(ShowAction));
        OnPropertyChanged(nameof(ShowStatus));
        OnPropertyChanged(nameof(ShowResponses));
    }

    [RelayCommand]
    private async Task ToggleFavorite()
    {
        try
        {
            var updated = await api.SetMomentFavoriteAsync(_momentId, !IsFavorite);
            IsFavorite = updated.IsFavorite;
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private Task Respond()
    {
        if (_moment is null)
        {
            return Task.CompletedTask;
        }

        var nav = Navigation.Relative();
        return _moment.ResponseKind switch
        {
            MomentResponseKind.Drawing => navigationService.GoToAsync(
                nav.Push<RespondDrawingViewModel>().WithIntent(new RespondMomentIntent(_momentId))),
            MomentResponseKind.Photo => navigationService.GoToAsync(
                nav.Push<RespondPhotoViewModel>().WithIntent(new RespondMomentIntent(_momentId))),
            MomentResponseKind.Voice => navigationService.GoToAsync(
                nav.Push<RespondVoiceViewModel>().WithIntent(new RespondMomentIntent(_momentId))),
            MomentResponseKind.Choice => navigationService.GoToAsync(
                nav.Push<RespondChoiceViewModel>().WithIntent(new RespondMomentIntent(_momentId, Title, Prompt, _moment.Options))),
            _ => navigationService.GoToAsync(
                nav.Push<RespondTextViewModel>().WithIntent(new RespondMomentIntent(_momentId, Title, Prompt)))
        };
    }
}
