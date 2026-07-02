using Microsoft.Maui.Controls;
using Nalu;
using Pulse.Resources;
using Pulse.ViewModels;

namespace Pulse.UI;

/// <summary>
/// Adds a nav bar "Profile" toolbar item on every tab root (Home / Trail / Together), now that Profile
/// is a pushed page rather than its own tab. Pushes <see cref="ProfileViewModel"/> via the page's own
/// navigation service. Call from a page's constructor (after InitializeComponent), mirroring
/// <see cref="ComposeToolbarItem"/>.
/// </summary>
public static class ProfileToolbarItem
{
    public static void Add(Page page, INavigationService navigationService)
    {
        var icon = new FontImageSource { FontFamily = "mdi", Glyph = MdiIcons.AccountOutline };
        icon.SetDynamicResource(FontImageSource.ColorProperty, "Brand");

        page.ToolbarItems.Add(new ToolbarItem
        {
            Command = new Command(() => navigationService.GoToAsync(Navigation.Relative().Push<ProfileViewModel>())),
            Order = ToolbarItemOrder.Primary,
            Priority = 2,
            IconImageSource = icon
        });
    }
}
