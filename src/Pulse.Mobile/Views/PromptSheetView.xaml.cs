using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class PromptSheetView : BottomSheetPage
{
    public PromptSheetView(PromptSheetViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
