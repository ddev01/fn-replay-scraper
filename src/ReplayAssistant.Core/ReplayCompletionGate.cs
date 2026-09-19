namespace ReplayAssistant.Core;

public sealed class ReplayCompletionGate
{
    private readonly TimeSpan _stableFor;
    private readonly Dictionary<string, Observation> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _emitted = new(StringComparer.OrdinalIgnoreCase);

    public ReplayCompletionGate(TimeSpan stableFor)
    {
        _stableFor = stableFor < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : stableFor;
    }

    public void Note(string path, long length, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var key = Path.GetFullPath(path);
        if (_files.TryGetValue(key, out var existing) && existing.Length != length)
        {
            _emitted.Remove(key);
        }

        _files[key] = new Observation(length, utcNow);
    }

    public IReadOnlyList<string> DrainReady(DateTimeOffset utcNow)
    {
        var ready = new List<string>();
        foreach (var (path, observation) in _files)
        {
            if (_emitted.Contains(path))
            {
                continue;
            }

            if (utcNow - observation.LastChangedUtc >= _stableFor)
            {
                _emitted.Add(path);
                ready.Add(path);
            }
        }

        return ready;
    }

    public bool WouldParse(string path, DateTimeOffset utcNow)
    {
        var key = Path.GetFullPath(path);
        if (!_files.TryGetValue(key, out var observation) || _emitted.Contains(key))
        {
            return false;
        }

        return utcNow - observation.LastChangedUtc >= _stableFor;
    }

    public static bool IsOldEnoughForCatchUp(DateTime lastWriteUtc, DateTimeOffset utcNow, TimeSpan stableFor)
    {
        var write = DateTime.SpecifyKind(lastWriteUtc, DateTimeKind.Utc);
        return utcNow - write >= stableFor;
    }

    private readonly record struct Observation(long Length, DateTimeOffset LastChangedUtc);
}
