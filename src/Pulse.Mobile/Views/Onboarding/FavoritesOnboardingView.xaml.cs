using Pulse.ViewModels;

namespace Pulse.Views.Onboarding;

public partial class FavoritesOnboardingView : ContentPage
{
    public FavoritesOnboardingView(FavoritesOnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
