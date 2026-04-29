using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Application.Modules.Resources.Entities;

/// <summary>
/// Represents a lightweight implementation of the <see cref="ILock"/> interface,
/// which manages access to a shared resource using semaphore.
/// </summary>
internal sealed class GateLock(SemaphoreSlim semaphore) : ILock
{
    public void Dispose() => semaphore.Release();
}