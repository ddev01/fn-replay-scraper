namespace ReplayAssistant.Core;

public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly bool _owned;

    private SingleInstance(Mutex mutex, bool owned)
    {
        _mutex = mutex;
        _owned = owned;
    }

    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, AppPaths.MutexName, out var created);
        if (created)
        {
            return new SingleInstance(mutex, owned: true);
        }

        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        if (_owned)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
