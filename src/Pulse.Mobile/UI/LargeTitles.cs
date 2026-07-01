#if IOS
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
#endif

namespace Pulse.UI;

/// <summary>
/// Opt a page into Apple's large-title nav bar on iOS. No-op elsewhere. Call from a page's constructor
/// (after InitializeComponent) so the whole app uses one consistent large-title setup without sprinkling
/// platform-specific XAML namespaces across every view.
/// </summary>
public static class LargeTitles
{
    public static void Enable(Microsoft.Maui.Controls.Page page)
    {
#if IOS
        page.On<iOS>().SetLargeTitleDisplay(LargeTitleDisplayMode.Always);
        // Every large-title page also gets the translucent (Liquid Glass) bar with content scrolling
        // underneath it — the Settings / Music / Photos treatment, applied app-wide and consistently.
        GlassNavBar.Enable(page);
#endif
    }
}
