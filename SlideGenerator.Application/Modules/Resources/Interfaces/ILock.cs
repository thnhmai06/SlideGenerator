namespace SlideGenerator.Application.Modules.Resources.Interfaces;

/// <summary>
///     Represents an acquired per-key lock. Disposing releases the underlying
///     <see cref="System.Threading.SemaphoreSlim" /> permit; when the last holder disposes,
///     the slim itself is freed by the <see cref="ILocker{TKey}" /> implementation.
/// </summary>
public interface ILock : IDisposable;