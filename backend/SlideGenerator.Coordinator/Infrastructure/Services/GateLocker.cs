/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: GateLocker.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */
using System.Collections.Concurrent;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Settings.Domain.Abstractions;

namespace SlideGenerator.Coordinator.Infrastructure.Services;

/// <summary>
///     High-level concurrency gate for <see cref="GateType" />.
///     Manages per-gate limits dynamically using a pure counting mechanism.
/// </summary>
internal sealed class GateLocker(ISettingProvider settingProvider, ISystemLogger logger) : IGateLocker
{
    /// <summary>
    ///     The dictionary of gate states, keyed by <see cref="GateType" />.
    /// </summary>
    private readonly ConcurrentDictionary<GateType, GateState> _gates = new();

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
                logger.Trace("Acquired gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
                return;
            }

            logger.Debug("Gate {Gate} reached limit ({Limit}). Waiting for admission.", gate, limit);
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
            logger.Trace("Acquired gate {Gate} after waiting (Active: {Count}/{Limit})", gate, state.ActiveCount,
                limit);
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
                logger.Trace("Failed to try-acquire gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount,
                    limit);
                return false;
            }

            state.ActiveCount++;
            logger.Trace("Try-acquired gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
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
            logger.Trace("Released gate {Gate} (Active: {Count}/{Limit})", gate, state.ActiveCount, limit);
            state.TryAdmit(limit);
        }
    }

    private int ResolveLimit(GateType gate)
    {
        var setting = settingProvider.Current;
        return gate switch
        {
            GateType.DownloadImage => setting.Performance.MaxParallelDownloadImage,
            GateType.EditImage => setting.Performance.MaxParallelEditImage,
            GateType.EditPresentation => setting.Performance.MaxParallelEditPresentation,
            GateType.ReadWorkbook => setting.Performance.MaxParallelReadWorkbook,
            GateType.ReadPresentation => setting.Performance.MaxParallelReadPresentation,
            _ => throw new ArgumentOutOfRangeException(nameof(gate), gate, null)
        };
    }

    /// <summary>
    ///     Represents the current state of a concurrency gate.
    /// </summary>
    private sealed class GateState
    {
        /// <summary>
        ///     The ordered list of waiters pending admission.
        ///     <see cref="LinkedList{T}" /> allows O(1) removal of canceled waiters via stored node references.
        /// </summary>
        public readonly LinkedList<TaskCompletionSource<bool>> Waiters = [];

        /// <summary>
        ///     The number of active operations currently holding a lock for this gate.
        /// </summary>
        public int ActiveCount;

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
}
