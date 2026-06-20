using Nalu;
using Pulse.ViewModels;
using Pulse.Views;

namespace Pulse;

public partial class AppShell : NaluShell
{
    public AppShell(INavigationService navigationService)
        : base(navigationService, typeof(StartupView), new StartupIntent())
    {
        InitializeComponent();
    }
}
