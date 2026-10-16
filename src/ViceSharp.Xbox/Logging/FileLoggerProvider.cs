// PLAN-XBOXUWP (area diagnostics): a portable file-backed ILogger for the Xbox UWP head.
//
// The deployed UWP head boots to a black screen and then exits with code 1 ~13 seconds after
// launch (an unhandled exception on a background/timer thread, NOT a startup fail-fast).
// Debug.WriteLine is effectively invisible in a packaged UWP app and there is no console, so
// this provider appends one readable line per log entry to a file (LocalState\vicesharp.log)
// that survives the process and can be read after the crash. The provider is deliberately
// portable (no #if HAS_UWP, System.IO only) so it also compiles on the workload-free net10.0
// fallback; only App.xaml.cs (UWP-guarded) actually constructs it with a concrete path.
namespace ViceSharp.Xbox.Logging;

using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

/// <summary>
/// A minimal, thread-safe <see cref="ILoggerProvider"/> that appends every log entry as one
/// line to a text file. Line format is
/// <c>yyyy-MM-dd HH:mm:ss.fff [Level] Category: message</c>, followed by the exception's
/// <see cref="Exception.ToString"/> on subsequent lines when an exception is supplied. The
/// target file is truncated (created fresh) in the constructor on a best-effort basis so each
/// launch starts a clean log. All writes are serialized under a single lock, so the provider is
/// safe to share across the UI thread, the render timer, and background/task threads.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly object _gate = new();
    private readonly string _path;

    /// <summary>
    /// Creates the provider and truncates/creates the log file at <paramref name="path"/> so the
    /// current launch writes a fresh log. File-system failures (e.g. an unwritable path) are
    /// swallowed: logging must never take down the app it is trying to diagnose.
    /// </summary>
    /// <param name="path">The absolute path of the log file to append entries to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
    public FileLoggerProvider(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // Truncate/create fresh for this launch (best-effort).
            File.WriteAllText(_path, string.Empty);
        }
        catch
        {
            // Best-effort: if the log file cannot be prepared, entries simply do not persist.
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged/long-lived handles: each write opens, appends, and closes under the lock.
    }

    private void Append(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var line = $"{timestamp} [{logLevel}] {categoryName}: {message}";
        var payload = exception is null
            ? line + Environment.NewLine
            : line + Environment.NewLine + exception + Environment.NewLine;

        lock (_gate)
        {
            try
            {
                File.AppendAllText(_path, payload);
            }
            catch
            {
                // Best-effort: never throw out of a logging call.
            }
        }
    }

    /// <summary>The per-category <see cref="ILogger"/> that forwards entries to the owning provider.</summary>
    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        public FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null)
                return;

            _provider.Append(logLevel, _categoryName, message, exception);
        }

        /// <summary>A no-op scope: this provider does not render scopes.</summary>
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            private NullScope()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
