using Pulse.ViewModels;

namespace Pulse.Views;

public partial class ManagePacksView : ContentPage
{
    public ManagePacksView(ManagePacksViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
