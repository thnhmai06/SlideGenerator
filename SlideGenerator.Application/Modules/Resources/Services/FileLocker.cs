using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Application.Modules.Resources.Services;

/// <summary>
///     Service providing file-path-based exclusion using a low-level keyed locker.
/// </summary>
public sealed class FileLocker(ILocker<string> locker) : IDisposable
{
    public ValueTask<ILock> ReadLockAsync(string key, CancellationToken ct = default) =>
        locker.AcquireAsync(key, ct);

    public ValueTask<ILock> WriteLockAsync(string key, CancellationToken ct = default) =>
        locker.AcquireAsync(key, ct);

    public void Dispose() => locker.Dispose();
}