using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class RespondChoiceView : BottomSheetPage
{
    public RespondChoiceView(RespondChoiceViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
