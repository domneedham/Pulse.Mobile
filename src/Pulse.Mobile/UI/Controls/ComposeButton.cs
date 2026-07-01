using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Nalu;
using Pulse.ViewModels;

namespace Pulse.UI.Controls;

/// <summary>
/// Global "send a signal" launcher, present on every tab (Home / Trail / Profile) with no page or
/// ViewModel wiring required. Android only: a filled Material FAB floating bottom-right, just clear
/// of the tab bar — the idiomatic Android pattern for an always-available action. iOS instead uses a
/// native nav bar <c>ToolbarItem</c> on each page (Mail/Reminders-style compose button), since a FAB
/// isn't an Apple pattern; this control collapses to nothing there. Colours use
/// <see cref="BindableObject.SetDynamicResource"/> (not a one-shot resource lookup) so the button
/// repaints on a light/dark theme swap. Tapping it resolves <see cref="INavigationService"/> from its
/// own handler at tap time (same lookup <see cref="Services.AlertService"/> uses).
/// </summary>
public sealed class ComposeButton : Border
{
    public ComposeButton()
    {
#if IOS
        IsVisible = false;
        InputTransparent = true;
        return;
#else
        HeightRequest = 60;
        WidthRequest = 60;
        StrokeShape = new RoundRectangle { CornerRadius = 30 };
        StrokeThickness = 0;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(0, 0, 20, 4);

        var shadow = new Shadow { Opacity = 0.18f, Radius = 16, Offset = new Point(0, 6) };
        shadow.SetDynamicResource(Shadow.BrushProperty, "Ink");
        Shadow = shadow;

        var icon = new AppIcon { Icon = PulseIcon.Plus, Size = 26 };
        this.SetDynamicResource(BackgroundProperty, "Brand");
        icon.SetDynamicResource(AppIcon.ColorProperty, "OnBrand");

        Content = icon;

        GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OpenComposeAsync()) });
#endif
    }

    private async Task OpenComposeAsync()
    {
        var navigationService = Handler?.MauiContext?.Services?.GetService<INavigationService>();
        if (navigationService is null)
        {
            return;
        }

        await navigationService.GoToAsync(Nalu.Navigation.Relative().Push<ComposeSheetViewModel>());
    }
}
