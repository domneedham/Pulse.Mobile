using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;

namespace Pulse.ViewModels;

/// <summary>
/// Intent for the in-app prompt sheet. Carries the copy + initial value, plus a completion source
/// the presenter awaits — the sheet resolves it with the entered text (Confirm) or null
/// (Cancel / scrim dismiss), so a caller can <c>await</c> a prompt just like a native alert.
/// </summary>
public record PromptSheetIntent(
    string Title,
    string Message,
    string? Placeholder,
    string? InitialValue,
    bool IsEmail,
    TaskCompletionSource<string?> Completion);

/// <summary>
/// Backs <see cref="Views.PromptSheetView"/> — the themed bottom-sheet replacement for the native
/// input alert. Resolves its intent's completion source exactly once, on Confirm, Cancel, or when
/// the page disappears (covers a scrim-tap dismiss that bypasses the buttons).
/// </summary>
public partial class PromptSheetViewModel(INavigationService navigationService)
    : ObservableObject, IEnteringAware<PromptSheetIntent>, IDisappearingAware
{
    private TaskCompletionSource<string?>? _completion;
    private bool _resolved;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _placeholder = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private Keyboard _keyboard = Keyboard.Default;

    public ValueTask OnEnteringAsync(PromptSheetIntent intent)
    {
        Title = intent.Title;
        Message = intent.Message;
        Placeholder = intent.Placeholder ?? string.Empty;
        Value = intent.InitialValue ?? string.Empty;
        Keyboard = intent.IsEmail ? Keyboard.Email : Keyboard.Default;
        _completion = intent.Completion;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private async Task Confirm()
    {
        Resolve(string.IsNullOrWhiteSpace(Value) ? null : Value.Trim());
        await navigationService.GoToAsync(Navigation.Relative().Pop());
    }

    [RelayCommand]
    private async Task Cancel()
    {
        Resolve(null);
        await navigationService.GoToAsync(Navigation.Relative().Pop());
    }

    // A scrim tap dismisses the sheet without hitting a button; make sure the awaiter still completes.
    public ValueTask OnDisappearingAsync()
    {
        Resolve(null);
        return ValueTask.CompletedTask;
    }

    private void Resolve(string? result)
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        _completion?.TrySetResult(result);
    }
}
