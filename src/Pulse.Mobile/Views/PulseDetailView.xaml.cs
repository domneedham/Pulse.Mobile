using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class PulseDetailView : ContentPage
{
    public PulseDetailView(PulseDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        // Transparent glass bar with the hero scrolling underneath; compact bar (no large title).
        GlassNavBar.Enable(this);
    }
}
