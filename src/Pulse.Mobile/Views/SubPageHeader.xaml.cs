using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Nalu;
using Pulse.Services;

namespace Pulse.Views;

/// <summary>
/// The shared header for pushed sub-pages (Leaderboard, Recent activity, How scoring works,
/// Participants, Settings…): a back chevron + title, with an optional subtitle and a trailing
/// text action. Back defaults to a relative <c>Pop</c> so pages don't have to wire it; supply
/// <see cref="ActionCommand"/> (and <see cref="ActionText"/>) for the trailing action.
/// </summary>
public partial class SubPageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SubPageHeader), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SubPageHeader), string.Empty);

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(SubPageHeader), string.Empty);

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(SubPageHeader), null);

    public SubPageHeader() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    private IServiceProvider? Services =>
        Shell.Current?.Handler?.MauiContext?.Services;

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        if (Services?.GetService<INavigationService>() is { } nav)
        {
            await nav.GoToAsync(Nalu.Navigation.Relative().Pop());
        }
    }

    private void OnActionTapped(object? sender, EventArgs e)
    {
        if (ActionCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
