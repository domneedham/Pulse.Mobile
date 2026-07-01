using Pulse.ViewModels;

namespace Pulse.Views;

public partial class ConnectView : ContentPage
{
    public ConnectView(ConnectViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
