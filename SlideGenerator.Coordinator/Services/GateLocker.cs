using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Coordinator.Services;

/// <summary>
///     High-level concurrency gate for <see cref="GateType"/>.
///     Manages per-gate limits dynamically using a pure counting mechanism.
/// </summary>
public sealed class GateLocker(ISettingProvider settingProvider, ILogger<GateLocker> logger) : IDisposable
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
                logger.LogTrace("Acquired gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
                return;
            }

            logger.LogDebug("Gate {Gate} reached limit ({Limit}). Waiting for admission.", gate, limit);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var node = state.Waiters.AddLast(tcs);
            waitTask = tcs.Task;

            if (ct.CanBeCanceled)
                ctr = ct.Register(() =>
                {
                    // O(1) removal via stored node reference — no stale entries accumulate.
                    lock (state)
                    {
                        if (node.List != null) state.Waiters.Remove(node);
                    }
                    tcs.TrySetCanceled(ct);
                });
        }

        try
        {
            await waitTask.ConfigureAwait(false);
            logger.LogTrace("Acquired gate {Gate} after waiting (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
        }
        finally
        {
            await ctr.DisposeAsync().ConfigureAwait(false);
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

            if (state.ActiveCount >= limit)
            {
                logger.LogTrace("Failed to try-acquire gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
                return false;
            }
            state.ActiveCount++;
            logger.LogTrace("Try-acquired gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
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
            logger.LogTrace("Released gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
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
                {
                    state.Waiters.First!.Value.TrySetCanceled();
                    state.Waiters.RemoveFirst();
                }
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
        ///     The ordered list of waiters pending admission.
        ///     <see cref="LinkedList{T}"/> allows O(1) removal of canceled waiters via stored node references.
        /// </summary>
        public readonly LinkedList<TaskCompletionSource<bool>> Waiters = [];

        /// <summary>
        ///     Attempts to admit pending waiters if the current active count is below the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of active operations allowed for the gate.</param>
        public void TryAdmit(int limit)
        {
            while (ActiveCount < limit && Waiters.Count > 0)
            {
                var node = Waiters.First!;
                Waiters.RemoveFirst();
                if (node.Value.TrySetResult(true)) ActiveCount++;
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
