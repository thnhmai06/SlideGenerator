using System.Collections.Concurrent;
using SlideGenerator.Application.Modules.Resources.Entities;
using SlideGenerator.Application.Modules.Resources.Interfaces;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Services.Generating.Rules;

namespace SlideGenerator.Application.Modules.Resources.Services;

/// <summary>
///     Concurrency gate for <see cref="GateType" /> that resolves per-gate slot limits
///     from <see cref="ISettingProvider" />.
/// </summary>
public sealed class GateLocker<TGate>(Func<TGate, int> resolver) : IDisposable where TGate : notnull
{
    private readonly ConcurrentDictionary<TGate, SemaphoreSlim> _semaphores = new();

    public void Dispose()
    {
        foreach (var s in _semaphores.Values) s.Dispose();
        _semaphores.Clear();
    }

    public async ValueTask<ILock> LockAsync(TGate key, CancellationToken ct = default)
    {
        var semaphore = _semaphores.GetOrAdd(key, k =>
        {
            var limit = resolver(k);
            return new SemaphoreSlim(Math.Max(1, limit), Math.Max(1, limit));
        });

        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        return new GateLock(semaphore);
    }
}