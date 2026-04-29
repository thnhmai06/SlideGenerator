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
public sealed class GateLocker(ISettingProvider settingProvider) : IDisposable
{
    private readonly ConcurrentDictionary<GateType, SemaphoreSlim> _semaphores = new();

    public async ValueTask<ILock> LockAsync(GateType key, CancellationToken ct = default)
    {
        var semaphore = _semaphores.GetOrAdd(key, k =>
        {
            var job = settingProvider.Current.Job;
            var limit = k switch
            {
                GateType.Download => job.MaxConcurrentDownloadFlows,
                GateType.EditImage => job.MaxConcurrentImageEditingFlows,
                GateType.EditSlide => job.MaxConcurrentSlideEditingFlows,
                _ => 5
            };

            return new SemaphoreSlim(Math.Max(1, limit), Math.Max(1, limit));
        });

        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        return new GateLock(semaphore);
    }

    public void Dispose()
    {
        foreach (var s in _semaphores.Values) s.Dispose();
        _semaphores.Clear();
    }
}