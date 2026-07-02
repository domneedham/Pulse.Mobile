using Nalu;
using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class TrailView : ContentPage
{
    public TrailView(TrailViewModel vm, INavigationService navigationService)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
        ComposeToolbarItem.Add(this, vm.OpenComposeCommand);
        ProfileToolbarItem.Add(this, navigationService);
    }
}
