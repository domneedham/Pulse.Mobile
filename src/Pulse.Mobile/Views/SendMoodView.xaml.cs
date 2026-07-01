using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class SendMoodView : BottomSheetPage
{
    public SendMoodView(SendMoodViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
