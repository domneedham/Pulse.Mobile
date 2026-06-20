using Pulse.ViewModels;

namespace Pulse.Views;

public partial class HomeView : ContentPage
{
    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
