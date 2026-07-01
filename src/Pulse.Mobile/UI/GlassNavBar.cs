namespace Pulse.UI;

/// <summary>
/// Opt a page into Apple's "content scrolls under a translucent nav bar" treatment on iOS (Photos /
/// Journal / Fitness style, adopting the Liquid Glass material on iOS 26). The native back button and
/// toolbar items are kept; the bar just goes transparent with no hairline, and the page extends its
/// content under the bar + into the status-area so a hero can bleed to the very top.
///
/// No-op on other platforms. Call from a page's constructor (after InitializeComponent), mirroring
/// <see cref="LargeTitles"/>. Pair it with <c>SafeAreaEdges</c> handling in the view so the first
/// scrollable element starts at Y=0; the bar then floats above it.
/// </summary>
public static class GlassNavBar
{
    public static void Enable(Microsoft.Maui.Controls.Page page)
    {
#if IOS
        IosGlassNavBar.Attach(page);
#endif
    }
}
