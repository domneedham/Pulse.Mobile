using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class ComposeSheetView : BottomSheetPage
{
    public ComposeSheetView(ComposeSheetViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
