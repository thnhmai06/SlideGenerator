namespace SlideGenerator.Application.Modules.Resources.Interfaces;

/// <summary>
///     Contract for low-level keyed locker implementations (e.g., library adapters in Infrastructure).
/// </summary>
public interface ILocker<in TKey> : IDisposable
    where TKey : notnull
{
    ValueTask<ILock> AcquireAsync(TKey key, CancellationToken cancellationToken);
}