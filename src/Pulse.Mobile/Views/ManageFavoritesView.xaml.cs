using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class ManageFavoritesView : ContentPage
{
    public ManageFavoritesView(ManageFavoritesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
    }
}
