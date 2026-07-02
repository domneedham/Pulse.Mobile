using System.Windows.Input;
using Microsoft.Maui.Controls;
using Pulse.Resources;

namespace Pulse.UI;

/// <summary>
/// Adds the "send a signal" nav bar toolbar item on iOS (Mail/Reminders-style compose button — a FAB
/// isn't an Apple pattern). No-op elsewhere; Android instead shows <see cref="Controls.ComposeButton"/>
/// as a floating FAB. Call from a page's constructor (after InitializeComponent), mirroring
/// <see cref="LargeTitles"/>.
/// </summary>
public static class ComposeToolbarItem
{
    public static void Add(Page page, ICommand command)
    {
#if IOS
        var icon = new FontImageSource { FontFamily = "mdi", Glyph = MdiIcons.SendOutline };
        icon.SetDynamicResource(FontImageSource.ColorProperty, "Brand");

        page.ToolbarItems.Add(new ToolbarItem
        {
            Command = command,
            Order = ToolbarItemOrder.Primary,
            Priority = 1,
            IconImageSource = icon
        });
#endif
    }
}
