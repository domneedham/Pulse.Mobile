using Pulse.ViewModels;

namespace Pulse.Views.Onboarding;

public partial class SignInView : ContentPage
{
    public SignInView(SignInViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
