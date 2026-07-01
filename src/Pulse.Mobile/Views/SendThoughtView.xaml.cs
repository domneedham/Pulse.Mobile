using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class SendThoughtView : BottomSheetPage
{
    public SendThoughtView(SendThoughtViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
