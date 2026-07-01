using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class AppSettingsView : ContentPage
{
    public AppSettingsView(AppSettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
    }
}
