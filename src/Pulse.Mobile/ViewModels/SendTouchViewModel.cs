using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.UI;

namespace Pulse.ViewModels;

/// <summary>A pen colour swatch in the PulseTouch palette.</summary>
public partial class TouchColor(string hex) : ObservableObject
{
    public string Hex { get; } = hex;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// "Draw a touch" — the PulseTouch sheet. Owns the palette + clear/send actions; the view owns the
/// SkiaSharp canvas and, on Send, hands back the captured <see cref="TouchDrawing"/> via
/// <see cref="SendDrawingAsync"/>. Clear is signalled to the view through <see cref="ClearRequested"/>.
/// </summary>
public partial class SendTouchViewModel(
    IPulseApiClient api,
    INavigationService navigationService,
    IAlertService alerts,
    HapticService haptics) : ObservableObject
{
    /// <summary>Brand-aligned palette (coral, purple, navy, plus a few friendly accents).</summary>
    public ObservableCollection<TouchColor> Colors { get; } =
    [
        new("#FF7A7A") { IsSelected = true },
        new("#A98BFF"),
        new("#07153D"),
        new("#7FAE8A"),
        new("#F2B705"),
        new("#3E7CB8"),
    ];

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>The currently selected pen colour (hex).</summary>
    public string SelectedColor => Colors.FirstOrDefault(c => c.IsSelected)?.Hex ?? "#FF7A7A";

    /// <summary>Raised when the user taps Clear so the canvas can wipe itself.</summary>
    public event EventHandler? ClearRequested;

    [RelayCommand]
    private void SelectColor(TouchColor color)
    {
        foreach (var c in Colors)
        {
            c.IsSelected = c == color;
        }

        OnPropertyChanged(nameof(SelectedColor));
    }

    [RelayCommand]
    private void Clear() => ClearRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Called by the view with the captured drawing when Send is tapped.</summary>
    public async Task SendDrawingAsync(TouchDrawing drawing)
    {
        if (IsBusy)
        {
            return;
        }

        if (drawing.Strokes.Count == 0)
        {
            await alerts.ShowToastAsync("Draw something first ✏️");
            return;
        }

        IsBusy = true;
        try
        {
            await api.SendTouchAsync(drawing.ToJson());
            haptics.Tap();
            await alerts.ShowToastAsync("PulseTouch sent ✏️");
            await navigationService.GoToAsync(Navigation.Relative().Pop());
        }
        catch (Exception ex)
        {
            await alerts.ShowErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task Close() => navigationService.GoToAsync(Navigation.Relative().Pop());
}
