using Pulse.ViewModels;

namespace Pulse.Views.Onboarding;

public partial class SignUpView : ContentPage
{
    public SignUpView(SignUpViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
