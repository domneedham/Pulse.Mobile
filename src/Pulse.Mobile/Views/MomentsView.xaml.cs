using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class MomentsView : ContentPage
{
    public MomentsView(MomentsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
    }
}
