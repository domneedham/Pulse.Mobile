namespace Pulse.UI.Controls;

/// <summary>
/// Shared layout for the Mood / Need / Thought send sheets. Inherits its BindingContext from the host
/// page (a SendPulseViewModel subclass), so all three sheets share one markup.
/// </summary>
public partial class SendPulseSheetView : ContentView
{
    public SendPulseSheetView() => InitializeComponent();
}
