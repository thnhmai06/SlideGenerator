namespace SlideGenerator.Application.Modules.Resources.Abstractions;

/// <summary>
///     Represents an acquired per-key lock. Disposing releases the underlying
///     <see cref="System.Threading.SemaphoreSlim" /> permit; when the last holder disposes,
///     the slim itself is freed by the <see cref="IAsyncKeyedLocker{TKey}" /> implementation.
/// </summary>
public interface IKeyedLockHandle : IDisposable;