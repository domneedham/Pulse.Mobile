using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class SendNeedView : BottomSheetPage
{
    public SendNeedView(SendNeedViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
