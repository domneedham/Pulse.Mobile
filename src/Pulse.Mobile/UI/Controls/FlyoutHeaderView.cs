using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Nalu;
using Pulse.Services;
using Pulse.ViewModels;

namespace Pulse.UI.Controls;

/// <summary>
/// Fixed header at the top of the Shell flyout: both partners' avatar pills, "You &amp; {Partner}", and
/// how long you've been connected. Resolves <see cref="UserSession"/>/<see cref="ConnectionSession"/>
/// from the handler's service provider at load time (same lookup pattern as <see cref="ComposeButton"/>)
/// since the Shell itself isn't DI-constructed with a page-scoped ViewModel. Refreshes on
/// <see cref="UserSession.Changed"/>/<see cref="ConnectionSession.Changed"/> so a later profile edit or
/// pairing change is reflected without reopening the flyout.
/// </summary>
public sealed class FlyoutHeaderView : ContentView
{
    private readonly AvatarView _myAvatar;
    private readonly AvatarView _partnerAvatar;
    private readonly Label _namesLabel;
    private readonly Label _connectedLabel;

    private UserSession? _userSession;
    private ConnectionSession? _connectionSession;
    private INavigationService? _navigationService;

    public ICommand OpenProfileCommand { get; }

    public FlyoutHeaderView()
    {
        OpenProfileCommand = new Command(async () => await OpenProfileAsync());

        _myAvatar = new AvatarView { Size = 40 };
        _partnerAvatar = new AvatarView { Size = 40 };

        var avatars = new HorizontalStackLayout { Spacing = -10, Children = { _myAvatar, _partnerAvatar } };

        _namesLabel = new Label { FontFamily = "SansBold", FontSize = 16 };
        _namesLabel.SetDynamicResource(Label.TextColorProperty, "Ink");

        _connectedLabel = new Label { FontFamily = "SansMedium", FontSize = 12 };
        _connectedLabel.SetDynamicResource(Label.TextColorProperty, "InkLight");

        var names = new VerticalStackLayout { Spacing = 3, Children = { _namesLabel, _connectedLabel } };

        var row = new HorizontalStackLayout { Spacing = 12, Children = { avatars, names } };

        var border = new Border
        {
            Padding = new Thickness(16, 18),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 0 },
            Content = row
        };
        border.SetDynamicResource(BackgroundProperty, "PageBackground");

        Content = border;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        var services = Handler?.MauiContext?.Services;
        if (services is null)
        {
            return;
        }

        _userSession = services.GetService<UserSession>();
        _connectionSession = services.GetService<ConnectionSession>();
        _navigationService = services.GetService<INavigationService>();

        if (_userSession is not null)
        {
            _userSession.Changed += OnSessionChanged;
        }

        if (_connectionSession is not null)
        {
            _connectionSession.Changed += OnSessionChanged;
        }

        Render();
    }

    private void OnSessionChanged(object? sender, EventArgs e) =>
        Dispatcher.Dispatch(Render);

    private void Render()
    {
        var myName = _userSession?.DisplayName ?? string.Empty;
        var partnerName = _connectionSession?.Partner?.DisplayName;

        _myAvatar.DisplayName = myName;
        _myAvatar.AvatarUrl = _userSession?.AvatarUrl;

        _partnerAvatar.DisplayName = partnerName ?? "?";
        _partnerAvatar.AvatarUrl = _connectionSession?.Partner?.AvatarUrl;
        _partnerAvatar.IsVisible = partnerName is not null;

        _namesLabel.Text = partnerName is null ? myName : $"You & {partnerName}";

        var connectedAt = _connectionSession?.Current?.ConnectedAt;
        _connectedLabel.Text = connectedAt is { } at
            ? $"connected · {DaysSince(at)} days"
            : string.Empty;
        _connectedLabel.IsVisible = connectedAt is not null;
    }

    private static int DaysSince(DateTimeOffset connectedAt) =>
        Math.Max(0, (DateTimeOffset.Now.Date - connectedAt.ToLocalTime().Date).Days);

    private async Task OpenProfileAsync()
    {
        if (_navigationService is null)
        {
            return;
        }

        await _navigationService.GoToAsync(Nalu.Navigation.Relative().Push<ProfileViewModel>());
    }
}
