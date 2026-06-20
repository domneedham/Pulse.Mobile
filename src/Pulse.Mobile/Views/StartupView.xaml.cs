using Pulse.ViewModels;

namespace Pulse.Views;

public partial class StartupView : ContentPage
{
    public StartupView(StartupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
