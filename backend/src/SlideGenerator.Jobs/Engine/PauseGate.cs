/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: PauseGate.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>
///     Swappable cooperative pause checkpoint: <see cref="Pause" />/<see cref="Resume" /> toggle a shared
///     signal; <see cref="CheckpointAsync" /> blocks the caller while paused. Only ever observed at a point
///     a workload itself calls <see cref="CheckpointAsync" /> — never mid-step.
/// </summary>
public sealed class PauseGate
{
    private TaskCompletionSource<bool>? _pauseSignal;

    public void Pause() =>
        Interlocked.CompareExchange(ref _pauseSignal,
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously), null);

    public void Resume() => Interlocked.Exchange(ref _pauseSignal, null)?.TrySetResult(true);

    public async Task CheckpointAsync(CancellationToken ct)
    {
        var signal = _pauseSignal;
        if (signal is null) return;
        await using var registration = ct.Register(() => signal.TrySetCanceled(ct));
        await signal.Task.ConfigureAwait(false);
    }
}
