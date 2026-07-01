using CommunityToolkit.Maui.Alerts;
using Microsoft.Extensions.DependencyInjection;
using Nalu;
using Pulse.ViewModels;

namespace Pulse.Services;

public interface IAlertService
{
    Task ShowAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Cancel");
    Task<string?> PromptAsync(string title, string message, string? placeholder = null, Keyboard? keyboard = null);

    /// <summary>Presents an action sheet; returns the chosen option, or null if cancelled.</summary>
    Task<string?> ChooseAsync(string title, string cancel, params string[] options);

    /// <summary>Brief, non-blocking confirmation (e.g. "Code copied").</summary>
    Task ShowToastAsync(string message);

    /// <summary>Maps exceptions to a friendly alert; returns the message shown.</summary>
    Task ShowErrorAsync(Exception exception);
}

public class AlertService : IAlertService
{
    public Task ShowAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, "OK");

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Cancel") =>
        Shell.Current.DisplayAlertAsync(title, message, accept, cancel);

    public async Task<string?> PromptAsync(string title, string message, string? placeholder = null, Keyboard? keyboard = null)
    {
        // Present the in-theme prompt sheet instead of the native input alert. The sheet resolves
        // this completion source on Confirm (text) or Cancel (null).
        var completion = new TaskCompletionSource<string?>();
        var intent = new PromptSheetIntent(
            title,
            message,
            placeholder,
            InitialValue: null,
            IsEmail: keyboard == Keyboard.Email,
            Completion: completion);

        // INavigationService is navigation-scoped, so resolve it from the active page's scope at
        // call time rather than capturing it in this singleton (which would be a captive dependency).
        var navigationService = ResolveNavigationService();
        if (navigationService is null)
        {
            // No active navigation scope — fall back to the native prompt so the caller still works.
            return await Shell.Current.DisplayPromptAsync(
                title, message, placeholder: placeholder, keyboard: keyboard ?? Keyboard.Default);
        }

        await navigationService.GoToAsync(Navigation.Relative().Push<PromptSheetViewModel>().WithIntent(intent));
        return await completion.Task;
    }

    private static INavigationService? ResolveNavigationService() =>
        Shell.Current?.CurrentPage?.Handler?.MauiContext?.Services?.GetService<INavigationService>()
        ?? Shell.Current?.Handler?.MauiContext?.Services?.GetService<INavigationService>();

    public async Task<string?> ChooseAsync(string title, string cancel, params string[] options)
    {
        var choice = await Shell.Current.DisplayActionSheet(title, cancel, null, options);
        return choice == cancel ? null : choice;
    }

    public Task ShowToastAsync(string message) =>
        Toast.Make(message).Show();

    public Task ShowErrorAsync(Exception exception)
    {
        var message = exception switch
        {
            Auth.AuthException or Api.ApiException => exception.Message,
            HttpRequestException => "Can't reach Pulse right now. Check that the API is running and try again.",
            TaskCanceledException => "The request timed out. Please try again.",
            _ => "Something unexpected went wrong. Please try again.",
        };

        return ShowAsync("Oops", message);
    }
}
