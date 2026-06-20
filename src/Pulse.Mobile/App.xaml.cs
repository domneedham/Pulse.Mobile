using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Services;

namespace Pulse;

public partial class App : Application
{
    private readonly INavigationService _navigationService;

    public App(INavigationService navigationService, IThemeService themeService, ILogger<App> logger)
    {
        _navigationService = navigationService;

        InitializeComponent();

        // Funnel otherwise-silent crashes into the log file so they're visible after the fact.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            logger.LogCritical(e.ExceptionObject as Exception, "Unhandled domain exception (terminating: {Terminating})", e.IsTerminating);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        // Apply the saved theme once resources are loaded.
        themeService.Initialise();
    }

#if ANDROID
    private Window? _window;
#endif

    protected override Window CreateWindow(IActivationState? activationState)
    {
        AppShell shell = new(_navigationService);

#if ANDROID
        return _window ??= new Window(shell);
#else
        return new Window(shell);
#endif
    }
}
