using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Pulse.Services.Logging;

/// <summary>
/// A minimal file-backed <see cref="ILoggerProvider"/>: every log line is appended to a single
/// rolling file under the app's data directory. Writes are serialised through a background queue
/// so logging never blocks the UI thread, and the file is trimmed when it grows past a cap so it
/// can't fill the device. Read it back / share it via <see cref="LogStore"/>.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly LogStore _store;
    private readonly LogLevel _minLevel;

    public FileLoggerProvider(LogStore store, LogLevel minLevel = LogLevel.Information)
    {
        _store = store;
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _store, _minLevel);

    public void Dispose() => _store.Flush();

    private sealed class FileLogger(string category, LogStore store, LogLevel minLevel) : ILogger
    {
        // Trim the category to its last segment ("Pulse.ViewModels.LeaderboardViewModel" -> "LeaderboardViewModel").
        private readonly string _shortCategory = category.Contains('.')
            ? category[(category.LastIndexOf('.') + 1)..]
            : category;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel && logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append(' ')
                .Append(Abbrev(logLevel))
                .Append(" [").Append(_shortCategory).Append("] ")
                .Append(formatter(state, exception));

            if (exception is not null)
            {
                sb.AppendLine().Append(exception);
            }

            store.Append(sb.ToString());
        }

        private static string Abbrev(LogLevel level) => level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "?",
        };
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// Owns the log file and the background writer. Lines are enqueued and drained by a single task,
/// so callers never touch the file directly and never block. Shared so a UI action can read/share it.
/// </summary>
public sealed class LogStore
{
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB cap; trimmed to half when exceeded.

    private readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>());
    private readonly string _filePath;
    private readonly object _fileLock = new();

    public LogStore()
    {
        FilePath = Path.Combine(FileSystem.AppDataDirectory, "pulse-app.log");
        _filePath = FilePath;

        var thread = new Thread(DrainLoop)
        {
            IsBackground = true,
            Name = "pulse-file-logger",
        };
        thread.Start();
    }

    /// <summary>Absolute path to the current log file.</summary>
    public string FilePath { get; }

    public void Append(string line)
    {
        // Never throw from logging; if the queue is completed (shutdown) just drop the line.
        try
        {
            _queue.Add(line);
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <summary>The current log contents (newest activity at the bottom), or a placeholder if empty.</summary>
    public string ReadAll()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                return "(no logs yet)";
            }

            try
            {
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"(failed to read log: {ex.Message})";
            }
        }
    }

    public void Clear()
    {
        lock (_fileLock)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.WriteAllText(_filePath, string.Empty);
                }
            }
            catch
            {
                // Best effort.
            }
        }
    }

    /// <summary>Drains anything still queued (best effort, on shutdown).</summary>
    public void Flush()
    {
        while (_queue.TryTake(out var line))
        {
            WriteLine(line);
        }
    }

    private void DrainLoop()
    {
        foreach (var line in _queue.GetConsumingEnumerable())
        {
            WriteLine(line);
        }
    }

    private void WriteLine(string line)
    {
        lock (_fileLock)
        {
            try
            {
                TrimIfNeeded();
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never crash the app; drop on failure.
            }
        }
    }

    private void TrimIfNeeded()
    {
        try
        {
            var info = new FileInfo(_filePath);
            if (!info.Exists || info.Length < MaxBytes)
            {
                return;
            }

            // Keep the most recent half of the file so we don't grow without bound.
            var all = File.ReadAllLines(_filePath);
            var kept = all.Skip(all.Length / 2).ToArray();
            File.WriteAllLines(_filePath, kept);
        }
        catch
        {
            // If trimming fails, leave the file as-is.
        }
    }
}
