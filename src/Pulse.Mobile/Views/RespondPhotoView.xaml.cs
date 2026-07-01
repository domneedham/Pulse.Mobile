using Pulse.UI.Controls;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class RespondPhotoView : BottomSheetPage
{
    public RespondPhotoView(RespondPhotoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
