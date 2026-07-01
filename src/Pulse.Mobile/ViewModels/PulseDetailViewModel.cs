using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Models;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;
using Pulse.UI.Controls;

namespace Pulse.ViewModels;

public record PulseDetailIntent(Guid PulseId);

/// <summary>
/// Pulse detail — a single pulse shown large (icon, text, type chip, "Sent to {partner}" + date),
/// with Favorite and Delete actions. Delete is only allowed for pulses the caller sent.
/// </summary>
public partial class PulseDetailViewModel(
    IPulseApiClient api,
    UserSession userSession,
    ConnectionSession connectionSession,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject, IEnteringAware<PulseDetailIntent>
{
    private Guid _pulseId;

    [ObservableProperty]
    private string _emoji = string.Empty;

    /// <summary>The native category icon shown in the hero (denotes the signal type, not the phrase emoji).</summary>
    [ObservableProperty]
    private PulseIcon _icon;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _typeLabel = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNote))]
    private string? _note;

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    /// <summary>Whether this pulse is a PulseTouch doodle (renders the stroke replay instead of the hero text).</summary>
    [ObservableProperty]
    private bool _isTouch;

    /// <summary>The doodle's stroke JSON, fetched separately once the pulse is known to be a Touch. Null until loaded.</summary>
    [ObservableProperty]
    private string? _strokeData;

    /// <summary>"From {name}" / "Sent to {name}" — the sender attribution line.</summary>
    [ObservableProperty]
    private string _attribution = string.Empty;

    /// <summary>The avatar shown beside the attribution (the sender's).</summary>
    [ObservableProperty]
    private string? _senderName;

    [ObservableProperty]
    private string? _senderAvatarUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteLabel))]
    [NotifyPropertyChangedFor(nameof(FavoriteGlyph))]
    private bool _isFavorite;

    /// <summary>Only the sender can delete (unsend) a pulse.</summary>
    [ObservableProperty]
    private bool _canDelete;

    /// <summary>You reply to ("send one back") a partner's pulse, not your own.</summary>
    [ObservableProperty]
    private bool _isFromPartner;

    [ObservableProperty]
    private bool _isBusy;

    public string FavoriteLabel => IsFavorite ? "Favorited" : "Favorite";
    public string FavoriteGlyph => IsFavorite ? Resources.MdiIcons.Heart : Resources.MdiIcons.HeartOutline;

    public async ValueTask OnEnteringAsync(PulseDetailIntent intent)
    {
        _pulseId = intent.PulseId;
        try
        {
            var pulse = await api.GetPulseAsync(_pulseId);
            Bind(pulse);

            if (IsTouch)
            {
                var touch = await api.GetTouchAsync(_pulseId);
                StrokeData = touch.StrokeData;
            }
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
            await navigationService.GoToAsync(Navigation.Relative().Pop());
        }
    }

    private void Bind(Models.Pulse pulse)
    {
        Emoji = pulse.Emoji;
        Icon = PulseDisplay.CategoryIcon(pulse.Type);
        Text = pulse.Text;
        TypeLabel = pulse.Type.ToString();
        Note = pulse.Note;
        IsTouch = pulse.Type == PulseType.Touch;
        IsFavorite = pulse.IsFavorite;
        CanDelete = pulse.SentByMe;
        IsFromPartner = !pulse.SentByMe;

        var partner = connectionSession.Partner?.DisplayName ?? "your partner";
        var when = pulse.CreatedAt.ToLocalTime().ToString("MMM d 'at' h:mm tt");
        if (pulse.SentByMe)
        {
            SenderName = userSession.DisplayName;
            SenderAvatarUrl = userSession.AvatarUrl;
            Attribution = $"Sent to {partner} · {when}";
        }
        else
        {
            SenderName = partner;
            SenderAvatarUrl = connectionSession.Partner?.AvatarUrl;
            Attribution = $"From {partner} · {when}";
        }
    }

    [RelayCommand]
    private async Task ToggleFavorite()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var updated = await api.SetFavoriteAsync(_pulseId, !IsFavorite);
            IsFavorite = updated.IsFavorite;
            haptics.Tap();
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
    private Task SendOneBack() =>
        navigationService.GoToAsync(Navigation.Relative().Push<SendThoughtViewModel>());

    [RelayCommand]
    private async Task Delete()
    {
        var confirmed = await alerts.ConfirmAsync(
            "Delete pulse?", "This removes it from your timeline for both of you.", "Delete");
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await api.DeletePulseAsync(_pulseId);
            haptics.Tap();
            await alerts.ShowToastAsync("Pulse deleted");
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
}
