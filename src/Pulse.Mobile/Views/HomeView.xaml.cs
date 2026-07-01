using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class HomeView : ContentPage
{
    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        LargeTitles.Enable(this);
        ComposeToolbarItem.Add(this, vm.OpenComposeCommand);
    }
}
