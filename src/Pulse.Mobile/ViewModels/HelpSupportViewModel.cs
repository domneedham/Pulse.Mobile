using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Logging;

namespace Pulse.ViewModels;

/// <summary>
/// Help &amp; support (storyboard 7): two actions — report a problem (emails the app logs +
/// diagnostics to support so issues can be debugged) and request a feature (a plain email). Both
/// fall back to the OS share sheet when no mail account is configured, handled in
/// <see cref="ILogService"/>.
/// </summary>
public partial class HelpSupportViewModel(
    ILogService logService,
    IAlertService alerts) : ObservableObject
{
    [RelayCommand]
    private async Task ReportProblem()
    {
        try
        {
            await logService.EmailAsync();
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }

    [RelayCommand]
    private async Task RequestFeature()
    {
        try
        {
            await logService.EmailFeatureRequestAsync();
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
    }
}
