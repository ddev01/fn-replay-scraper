using Microsoft.Extensions.Logging;

namespace ReplayAssistant;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly object _gate = new();
    private const long MaxBytes = 512 * 1024;

    public FileLoggerProvider(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    public string ActivePath => Path.Combine(_directory, "replay-assistant.log");

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose() { }

    internal void Append(string line)
    {
        lock (_gate)
        {
            RotateIfNeeded();
            File.AppendAllText(ActivePath, line);
        }
    }

    private void RotateIfNeeded()
    {
        var path = ActivePath;
        var info = new FileInfo(path);
        if (!info.Exists || info.Length < MaxBytes)
        {
            return;
        }

        var bak = path + ".1";
        if (File.Exists(bak))
        {
            File.Delete(bak);
        }

        File.Move(path, bak);
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel is >= LogLevel.Information and not LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line =
                $"{DateTimeOffset.Now:u} {logLevel} {category}: {formatter(state, exception)}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            provider.Append(line + Environment.NewLine);
        }
    }
}
