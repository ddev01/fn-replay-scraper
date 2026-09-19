namespace ReplayAssistant.Core;

public sealed class DemosWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public DemosWatcher(string directory)
    {
        Directory.CreateDirectory(directory);
        _watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.replay",
            IncludeSubdirectories = false,
            NotifyFilter =
                NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            InternalBufferSize = 64 * 1024,
        };
        _watcher.Created += OnEvent;
        _watcher.Changed += OnEvent;
        _watcher.Renamed += OnRenamed;
    }

    public event Action<string>? ReplayTouched;

    public bool EnableRaisingEvents
    {
        get => _watcher.EnableRaisingEvents;
        set => _watcher.EnableRaisingEvents = value;
    }

    public void Dispose() => _watcher.Dispose();

    private void OnEvent(object sender, FileSystemEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.FullPath))
        {
            ReplayTouched?.Invoke(e.FullPath);
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.FullPath))
        {
            ReplayTouched?.Invoke(e.FullPath);
        }
    }
}
