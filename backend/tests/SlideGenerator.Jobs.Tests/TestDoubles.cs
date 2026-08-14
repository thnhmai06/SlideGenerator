/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs.Tests
 * File: TestDoubles.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Jobs.Engine;

namespace SlideGenerator.Jobs.Tests;

/// <summary>Minimal domain-free state used by every <see cref="JobEngine{TKey,TState}" /> test.</summary>
internal sealed record TestState(int Step);

/// <summary>
///     A workload that, for each of <paramref name="stepCount" /> steps, reports its state and then blocks
///     on a test-controlled gate before checkpointing — giving tests a deterministic point to pause/inspect
///     without racing the workload's own execution.
/// </summary>
internal sealed class ScriptedWorkload(
    int stepCount,
    TestState finalState,
    int throwAtStep = -1,
    Exception? exception = null)
    : IJobWorkload<TestState>
{
    /// <summary>Signaled right after step <c>i</c>'s <see cref="IJobContext{TState}.ReportAsync" /> call returns.</summary>
    public IReadOnlyList<TaskCompletionSource> Reported { get; } =
    [
        .. Enumerable.Range(0, stepCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
    ];

    /// <summary>
    ///     The test completes step <c>i</c>'s entry to let the workload proceed into
    ///     <see cref="IJobContext{TState}.CheckpointAsync" />.
    /// </summary>
    public IReadOnlyList<TaskCompletionSource> Release { get; } =
    [
        .. Enumerable.Range(0, stepCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
    ];

    public bool RanToCompletion { get; private set; }

    public async Task<TestState> RunAsync(TestState state, IJobContext<TestState> context, CancellationToken ct)
    {
        for (var i = 0; i < stepCount; i++)
        {
            var stepState = state with { Step = i + 1 };
            await context.ReportAsync(stepState, false, ct).ConfigureAwait(false);
            Reported[i].TrySetResult();
            await Release[i].Task.ConfigureAwait(false);
            await context.CheckpointAsync(ct).ConfigureAwait(false);
            if (i == throwAtStep) throw exception!;
        }

        RanToCompletion = true;
        return finalState;
    }
}

/// <summary>
///     A workload whose body never runs any await point before throwing/returning — used for trivial start/duplicate
///     tests.
/// </summary>
internal sealed class ImmediateWorkload(Func<TestState, TestState> transform) : IJobWorkload<TestState>
{
    public Task<TestState> RunAsync(TestState state, IJobContext<TestState> context, CancellationToken ct)
    {
        return Task.FromResult(transform(state));
    }
}

/// <summary>A workload that must never actually run — used to assert a job never left the concurrency queue.</summary>
internal sealed class UnreachableWorkload : IJobWorkload<TestState>
{
    public bool WasCalled { get; private set; }

    public Task<TestState> RunAsync(TestState state, IJobContext<TestState> context, CancellationToken ct)
    {
        WasCalled = true;
        throw new InvalidOperationException("This workload should never run.");
    }
}

/// <summary>Records every <see cref="IJobObserver{TKey,TState}" /> callback and exposes a per-key terminal-outcome waiter.</summary>
internal sealed class RecordingObserver : IJobObserver<string, TestState>
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, TaskCompletionSource<JobTerminalResult<TestState>>> _terminal = new();

    public List<(string Key, TestState State, bool Durable)> Progress { get; } = [];
    public List<(string Key, TestState State)> Paused { get; } = [];
    public List<(string Key, TestState State)> Resumed { get; } = [];

    public Task OnProgressAsync(string key, TestState state, bool durable, CancellationToken ct)
    {
        lock (_lock)
        {
            Progress.Add((key, state, durable));
        }

        return Task.CompletedTask;
    }

    public Task OnPausedAsync(string key, TestState state, CancellationToken ct)
    {
        lock (_lock)
        {
            Paused.Add((key, state));
        }

        return Task.CompletedTask;
    }

    public Task OnResumedAsync(string key, TestState state, CancellationToken ct)
    {
        lock (_lock)
        {
            Resumed.Add((key, state));
        }

        return Task.CompletedTask;
    }

    public Task OnTerminalAsync(string key, JobTerminalResult<TestState> result, CancellationToken ct)
    {
        GetOrAddTcs(key).TrySetResult(result);
        return Task.CompletedTask;
    }

    /// <summary>Waits for <paramref name="key" />'s terminal outcome. Safe to call before or after it happens.</summary>
    public Task<JobTerminalResult<TestState>> WaitTerminalAsync(string key)
    {
        return GetOrAddTcs(key).Task;
    }

    private TaskCompletionSource<JobTerminalResult<TestState>> GetOrAddTcs(string key)
    {
        lock (_lock)
        {
            if (_terminal.TryGetValue(key, out var tcs)) return tcs;
            tcs = new TaskCompletionSource<JobTerminalResult<TestState>>(TaskCreationOptions
                .RunContinuationsAsynchronously);
            _terminal[key] = tcs;
            return tcs;
        }
    }
}

/// <summary>Mutable concurrency limit, so tests can change it mid-run.</summary>
internal sealed class FakeConcurrencyProvider(int initial) : IJobConcurrencyProvider
{
    public int MaxConcurrentJobs { get; set; } = initial;
}

/// <summary>Returns a fixed, test-supplied list of jobs to resume.</summary>
internal sealed class FakeResumeSource(IReadOnlyList<PendingJob<string, TestState>> pending)
    : IJobResumeSource<string, TestState>
{
    public Task<IReadOnlyList<PendingJob<string, TestState>>> GetPendingJobsAsync(CancellationToken ct)
    {
        return Task.FromResult(pending);
    }
}