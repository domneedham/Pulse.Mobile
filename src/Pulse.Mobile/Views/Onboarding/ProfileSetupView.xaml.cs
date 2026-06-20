using Pulse.ViewModels;

namespace Pulse.Views.Onboarding;

public partial class ProfileSetupView : ContentPage
{
    public ProfileSetupView(ProfileSetupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
