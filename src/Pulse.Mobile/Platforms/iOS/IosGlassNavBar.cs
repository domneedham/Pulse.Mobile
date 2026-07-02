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
        // Applying this only on Loaded (after the page has already laid out once under the default
        // opaque bar) is what caused the visible "jump": first paint shows the normal bar + safe-area
        // inset, then a frame later the appearance flips to transparent and EdgesForExtendedLayout
        // kicks in, forcing a relayout the user can see. HandlerChanged fires as soon as the native
        // UIViewController exists — before the page's first layout pass — so applying there instead
        // means the very first frame already renders with the final, transparent-bar layout.
        page.HandlerChanged -= OnHandlerChanged;
        page.HandlerChanged += OnHandlerChanged;

        // Loaded stays as a fallback for the rare case where NavigationController isn't attached yet
        // at HandlerChanged time (e.g. the page isn't on a nav stack until it's actually pushed).
        page.Loaded -= OnLoaded;
        page.Loaded += OnLoaded;

        if (page.Handler is not null)
        {
            Apply(page);
        }
    }

    private static void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is Page page)
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

        // UINavigationBarAppearance.ShadowColor only affects a plain UIKit UINavigationController.
        // MAUI Shell renders through its own compatibility nav bar (MauiNavigationBar), which still
        // draws a private _UIBarBackgroundShadowView regardless of the appearance object — confirmed
        // via Xcode's view debugger. Hide that view directly since there's no public API for it.
        if (viewController.NavigationController?.NavigationBar is { } navigationBar)
        {
            HideBarBackgroundShadow(navigationBar);
        }
    }

    private static void HideBarBackgroundShadow(UIView root)
    {
        foreach (var subview in root.Subviews)
        {
            if (subview.Class.Name == "_UIBarBackgroundShadowView")
            {
                subview.Hidden = true;
                continue;
            }

            HideBarBackgroundShadow(subview);
        }
    }
}
