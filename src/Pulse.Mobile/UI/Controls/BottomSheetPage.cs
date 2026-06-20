using Nalu;

namespace Pulse.UI.Controls;

/// <summary>
/// Base for popup pages that animate in as a bottom sheet (slide up from the bottom edge) rather
/// than the default scale/fade. Mirrors the in-app sheet pattern used across the app for prompts.
/// </summary>
public abstract class BottomSheetPage : PopupPageBase
{
    private const uint AnimationDuration = 300;

    public BottomSheetPage()
    {
        SafeAreaEdges = SafeAreaEdges.None;
        PopupBorder.OverlapsSafeArea = true;
    }

    protected override void PreparePopupAnimation()
    {
        if (PopupBorder is not null)
        {
            PopupBorder.TranslationY = 1000;
        }
    }

    protected override void AnimatePopup()
    {
        if (PopupBorder is not null)
        {
            PopupBorder.TranslateToAsync(0, 0, AnimationDuration, Easing.CubicOut);
        }
    }
}
