using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class TrailView : ContentPage
{
    public TrailView(TrailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
        ComposeToolbarItem.Add(this, vm.OpenComposeCommand);
    }
}
