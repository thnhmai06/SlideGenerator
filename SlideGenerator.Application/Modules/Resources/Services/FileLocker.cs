using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Application.Modules.Resources.Services;

/// <summary>
///     Service providing file-path-based exclusion using a low-level keyed locker.
/// </summary>
public sealed class FileLocker(ILocker<string> locker) : IDisposable
{
    public void Dispose()
    {
        locker.Dispose();
    }

    public ValueTask<ILock> ReadLockAsync(string key, CancellationToken ct = default)
    {
        return locker.AcquireAsync(key, ct);
    }

    public ValueTask<ILock> WriteLockAsync(string key, CancellationToken ct = default)
    {
        return locker.AcquireAsync(key, ct);
    }
}