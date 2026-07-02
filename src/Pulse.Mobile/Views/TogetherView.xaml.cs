using Nalu;
using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class TogetherView : ContentPage
{
    public TogetherView(TogetherViewModel vm, INavigationService navigationService)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
        ComposeToolbarItem.Add(this, vm.OpenComposeCommand);
        ProfileToolbarItem.Add(this, navigationService);
    }
}
