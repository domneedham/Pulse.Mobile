using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class RespondTextView : BottomSheetPage
{
    public RespondTextView(RespondTextViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
