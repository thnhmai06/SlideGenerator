using System.Collections.Concurrent;
using SlideGenerator.Gate.Models;
using SlideGenerator.Settings.Interfaces;

namespace SlideGenerator.Gate.Services;

/// <summary>
///     High-level concurrency gate for <see cref="GateType"/>.
///     Manages per-gate limits dynamically using a pure counting mechanism.
/// </summary>
public sealed class GateLocker(ISettingProvider settingProvider) : IDisposable
{
    /// <summary>
    ///     The dictionary of gate states, keyed by <see cref="GateType" />.
    /// </summary>
    private readonly ConcurrentDictionary<GateType, GateState> _gates = new();

    /// <summary>
    ///     Asynchronously waits to acquire a lock for the specified gate.
    /// </summary>
    /// <param name="gate">The gate type.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    public async ValueTask AcquireAsync(GateType gate, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var state = _gates.GetOrAdd(gate, _ => new GateState());
        var limit = Math.Max(1, ResolveLimit(gate));

        Task waitTask;
        CancellationTokenRegistration ctr = default;

        lock (state)
        {
            state.TryAdmit(limit);

            if (state.ActiveCount < limit)
            {
                state.ActiveCount++;
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            state.Waiters.Enqueue(tcs);
            waitTask = tcs.Task;

            if (ct.CanBeCanceled) ctr = ct.Register(() => tcs.TrySetCanceled(ct));
        }

        try
        {
            await waitTask.ConfigureAwait(false);
        }
        finally
        {
            await ctr.DisposeAsync();
        }
    }

    /// <summary>
    ///     Tries to acquire a lock for the specified gate immediately without blocking.
    /// </summary>
    /// <param name="gate">The gate type.</param>
    /// <returns><see langword="true" /> if the lock was acquired; otherwise, <see langword="false" />.</returns>
    public bool TryAcquire(GateType gate)
    {
        var state = _gates.GetOrAdd(gate, _ => new GateState());
        var limit = Math.Max(1, ResolveLimit(gate));

        lock (state)
        {
            state.TryAdmit(limit);

            if (state.ActiveCount >= limit) return false;
            state.ActiveCount++;
            return true;
        }
    }

    /// <summary>
    ///     Releases the lock for the specified gate.
    /// </summary>
    /// <param name="gate">The gate type.</param>
    public void Release(GateType gate)
    {
        if (!_gates.TryGetValue(gate, out var state)) return;

        var limit = Math.Max(1, ResolveLimit(gate));

        lock (state)
        {
            if (state.ActiveCount > 0) state.ActiveCount--;
            state.TryAdmit(limit);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var state in _gates.Values)
        {
            lock (state)
            {
                while (state.Waiters.Count > 0)
                    state.Waiters.Dequeue().TrySetCanceled();
            }
        }

        _gates.Clear();
    }

    /// <summary>
    ///     Represents the current state of a concurrency gate.
    /// </summary>
    private sealed class GateState
    {
        /// <summary>
        ///     The number of active operations currently holding a lock for this gate.
        /// </summary>
        public int ActiveCount;

        /// <summary>
        ///     The queue of waiters waiting to acquire a lock for this gate.
        /// </summary>
        public readonly Queue<TaskCompletionSource<bool>> Waiters = new();

        /// <summary>
        ///     Attempts to admit pending waiters if the current active count is below the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of active operations allowed for the gate.</param>
        public void TryAdmit(int limit)
        {
            while (ActiveCount < limit && Waiters.Count > 0)
            {
                var tcs = Waiters.Dequeue();
                if (tcs.TrySetResult(true)) ActiveCount++;
            }
        }
    }

    private int ResolveLimit(GateType gate)
    {
        var setting = settingProvider.Current;
        return gate switch
        {
            GateType.DownloadImage => setting.Job.MaxParallelDownloadImage,
            GateType.EditImage => setting.Job.MaxParallelEditImage,
            GateType.EditPresentation => setting.Job.MaxParallelEditPresentation,
            GateType.ReadWorkbook => setting.Job.MaxParallelReadWorkbook,
            GateType.ReadPresentation => setting.Job.MaxParallelReadPresentation,
            _ => throw new ArgumentOutOfRangeException(nameof(gate), gate, null)
        };
    }
}