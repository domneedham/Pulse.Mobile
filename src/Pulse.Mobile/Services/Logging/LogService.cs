using Microsoft.Maui.ApplicationModel.Communication;

namespace Pulse.Services.Logging;

public interface ILogService
{
    /// <summary>The current log contents, newest activity at the bottom.</summary>
    string ReadLog();

    /// <summary>Open the OS share sheet with the log file attached (AirDrop, Files, Mail, etc.).</summary>
    Task ShareAsync();

    /// <summary>Compose an email with the log file attached, ready to send from the phone.</summary>
    Task EmailAsync();

    /// <summary>Compose a plain feature-request email to support (no log attachment).</summary>
    Task EmailFeatureRequestAsync();

    /// <summary>Wipe the log file.</summary>
    void Clear();
}

/// <summary>
/// User-facing access to the app log: read it on-screen, share it, or email it. Backed by the
/// single rolling file owned by <see cref="LogStore"/>. A fresh, timestamped copy of the file is
/// written for each share/email so the attachment is a stable snapshot the OS can read.
/// </summary>
public class LogService(LogStore store) : ILogService
{
    private const string SupportEmail = "domneedham81@gmail.com";

    public string ReadLog() => store.ReadAll();

    public async Task ShareAsync()
    {
        var path = await SnapshotAsync();
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Pulse app logs",
            File = new ShareFile(path),
        });
    }

    public async Task EmailAsync()
    {
        var path = await SnapshotAsync();

        var message = new EmailMessage
        {
            Subject = $"Pulse problem report — {DateTimeOffset.Now:yyyy-MM-dd HH:mm}",
            Body = "Please describe what went wrong:\n\n\n"
                + "— Logs and diagnostics are attached to help us debug. —\n\n"
                + Diagnostics(),
            To = [SupportEmail],
            Attachments = [new EmailAttachment(path)],
        };

        try
        {
            await Email.Default.ComposeAsync(message);
        }
        catch (FeatureNotSupportedException)
        {
            // No mail account configured: fall back to the generic share sheet so the log
            // can still leave the device (e.g. via Messages, Drive, or AirDrop).
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Pulse app logs",
                File = new ShareFile(path),
            });
        }
    }

    public async Task EmailFeatureRequestAsync()
    {
        var message = new EmailMessage
        {
            Subject = "Pulse feature request",
            Body = "What would you like to see in Pulse?\n\n\n"
                + Diagnostics(),
            To = [SupportEmail],
        };

        try
        {
            await Email.Default.ComposeAsync(message);
        }
        catch (FeatureNotSupportedException)
        {
            // No mail account configured: fall back to the share sheet so the request can still be sent.
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "Pulse feature request",
                Subject = "Pulse feature request",
                Text = message.Body,
            });
        }
    }

    public void Clear() => store.Clear();

    private static string Diagnostics() =>
        $"App: {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})\n"
        + $"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}\n"
        + $"OS: {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}\n";

    /// <summary>Copies the live log to a timestamped file in the cache for sharing as a snapshot.</summary>
    private async Task<string> SnapshotAsync()
    {
        var snapshotPath = Path.Combine(
            FileSystem.CacheDirectory, $"pulse-logs-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt");
        await File.WriteAllTextAsync(snapshotPath, store.ReadAll());
        return snapshotPath;
    }
}
