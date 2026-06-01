/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: GateLocker.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Application.Abstractions;

namespace SlideGenerator.Coordinator.Application.Services;

/// <summary>
///     Concurrency gate implementation for <typeparamref name="TGate" />.
///     Maintains per-gate active counts and a FIFO waiter queue; slot limits are
///     resolved at runtime via a caller-supplied delegate.
/// </summary>
/// <typeparam name="TGate">An enum whose values each represent an independent concurrency gate.</typeparam>
public sealed class GateLocker<TGate>(
    Func<TGate, uint> limitResolver,
    ILogger<GateLocker<TGate>>? logger = null) : IGateLocker<TGate>
    where TGate : struct, Enum
{
    /// <summary>
    ///     Per-gate state, keyed by <typeparamref name="TGate" /> value.
    /// </summary>
    private readonly ConcurrentDictionary<TGate, GateState> _gates = new();

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var state in _gates.Values)
            lock (state)
            {
                while (state.Waiters.Count > 0)
                {
                    state.Waiters.First!.Value.TrySetCanceled();
                    state.Waiters.RemoveFirst();
                }
            }

        _gates.Clear();
    }

    /// <inheritdoc />
    public async ValueTask AcquireAsync(TGate gate, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var state = _gates.GetOrAdd(gate, _ => new GateState());
        var limit = (int)Math.Max(1u, limitResolver(gate));

        Task waitTask;
        CancellationTokenRegistration ctr = default;

        lock (state)
        {
            state.TryAdmit(limit);

            if (state.ActiveCount < limit)
            {
                state.ActiveCount++;
                logger?.LogTrace("Acquired gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
                return;
            }

            logger?.LogDebug("Gate {Gate} reached limit ({Limit}). Waiting for admission.", gate, limit);
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
            logger?.LogTrace("Acquired gate {Gate} after waiting (Active: {Count}/{Limit})", gate, state.ActiveCount,
                limit);
        }
        finally
        {
            await ctr.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public bool TryAcquire(TGate gate)
    {
        var state = _gates.GetOrAdd(gate, _ => new GateState());
        var limit = (int)Math.Max(1u, limitResolver(gate));

        lock (state)
        {
            state.TryAdmit(limit);

            if (state.ActiveCount >= limit)
            {
                logger?.LogTrace("Failed to try-acquire gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount,
                    limit);
                return false;
            }

            state.ActiveCount++;
            logger?.LogTrace("Try-acquired gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
            return true;
        }
    }

    /// <inheritdoc />
    public void Release(TGate gate)
    {
        if (!_gates.TryGetValue(gate, out var state)) return;

        var limit = (int)Math.Max(1u, limitResolver(gate));

        lock (state)
        {
            if (state.ActiveCount > 0) state.ActiveCount--;
            logger?.LogTrace("Released gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
            state.TryAdmit(limit);
        }
    }

    /// <summary>
    ///     Tracks the active count and pending waiter queue for a single gate value.
    /// </summary>
    private sealed class GateState
    {
        /// <summary>
        ///     Ordered queue of waiters pending admission.
        ///     <see cref="LinkedList{T}" /> enables O(1) removal of cancelled waiters via stored node references.
        /// </summary>
        public readonly LinkedList<TaskCompletionSource<bool>> Waiters = [];

        /// <summary>
        ///     Number of callers currently holding an acquired slot for this gate.
        /// </summary>
        public int ActiveCount;

        /// <summary>
        ///     Admits pending waiters while the active count is below <paramref name="limit" />.
        /// </summary>
        /// <param name="limit">Maximum number of concurrent holders allowed.</param>
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
}