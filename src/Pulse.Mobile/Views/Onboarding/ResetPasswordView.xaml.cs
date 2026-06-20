using Pulse.ViewModels;

namespace Pulse.Views.Onboarding;

public partial class ResetPasswordView : ContentPage
{
    public ResetPasswordView(ResetPasswordViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
