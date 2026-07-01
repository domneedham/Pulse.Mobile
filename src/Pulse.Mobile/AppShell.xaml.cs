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

    // The iOS Shell tab bar already renders with the native Liquid Glass material, so we deliberately
    // do NOT override UITabBarAppearance here — a manual blur/background would replace (and downgrade)
    // the system glass. Tab colours come from the PulseTabBar style in AppShell.xaml.
}
