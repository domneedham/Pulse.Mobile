using Pulse.ViewModels;

namespace Pulse.Views;

public partial class HelpSupportView : ContentPage
{
    public HelpSupportView(HelpSupportViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
