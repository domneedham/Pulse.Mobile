using Microsoft.Maui.Platform;
using UIKit;

namespace Pulse.UI;

/// <summary>
/// iOS implementation of <see cref="GlassNavBar"/>: makes the page's navigation bar transparent (no
/// solid fill, no hairline shadow) and lets the page's content extend underneath it — the Photos /
/// Journal / Fitness "content behind a glass bar" effect. The appearance is applied to the page's own
/// <c>NavigationItem</c> (not the global <c>UINavigationBar.Appearance</c>) so other pages keep their
/// normal opaque bar. On iOS 26 the transparent bar renders with the system Liquid Glass material.
/// </summary>
internal static class IosGlassNavBar
{
    public static void Attach(Page page)
    {
        // The view controller / nav item only exist once the page is on screen; (re)wire on Loaded.
        page.Loaded -= OnLoaded;
        page.Loaded += OnLoaded;

        if (page.IsLoaded)
        {
            Apply(page);
        }
    }

    private static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is Page page)
        {
            Apply(page);
        }
    }

    private static void Apply(Page page)
    {
        if (page.Handler is not IPlatformViewHandler { ViewController: { } viewController })
        {
            return;
        }

        var navItem = viewController.NavigationItem;
        if (navItem is null)
        {
            return; // not inside a navigation controller (yet)
        }

        // Transparent background + no hairline. ConfigureWithTransparentBackground keeps the bar's
        // system translucency/blur (Liquid Glass on 26) while removing the solid fill, so content
        // scrolls visibly underneath it.
        var appearance = new UINavigationBarAppearance();
        appearance.ConfigureWithTransparentBackground();
        appearance.BackgroundColor = UIColor.Clear;
        appearance.ShadowColor = UIColor.Clear;

        // Apply to both states so the bar stays transparent whether or not content is scrolled to the
        // top (ScrollEdgeAppearance is what iOS uses at the top of a scroll view).
        navItem.StandardAppearance = appearance;
        navItem.ScrollEdgeAppearance = appearance;
        if (OperatingSystem.IsIOSVersionAtLeast(15))
        {
            navItem.CompactAppearance = appearance;
        }

        // Extend the content under the (now transparent) bar and into the status-bar area, so a hero can
        // bleed to Y=0 and scroll beneath the bar rather than being pushed below a safe-area inset.
        viewController.EdgesForExtendedLayout = UIRectEdge.All;
        viewController.ExtendedLayoutIncludesOpaqueBars = true;
    }
}
