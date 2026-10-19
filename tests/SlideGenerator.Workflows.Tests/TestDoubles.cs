/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows.Tests
 * File: TestDoubles.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Workflows.Tests;

/// <summary>Minimal domain-free state used by every <see cref="WorkflowEngine{TKey,TState}" /> test.</summary>
internal sealed record TestState(int Step);

/// <summary>
///     A runner that, for each of <paramref name="stepCount" /> steps, reports its state and then blocks
///     on a test-controlled gate before checkpointing — giving tests a deterministic point to pause/inspect
///     without racing the runner's own execution.
/// </summary>
internal sealed class ScriptedRunner(
    int stepCount,
    TestState finalState,
    int throwAtStep = -1,
    Exception? exception = null)
    : IWorkflowRunner<TestState>
{
    /// <summary>Signaled right after step <c>i</c>'s <see cref="IWorkflowContext{TState}.ReportAsync" /> call returns.</summary>
    public IReadOnlyList<TaskCompletionSource> Reported { get; } =
    [
        .. Enumerable.Range(0, stepCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
    ];

    /// <summary>
    ///     The test completes step <c>i</c>'s entry to let the runner proceed into
    ///     <see cref="IWorkflowContext{TState}.CheckpointAsync" />.
    /// </summary>
    public IReadOnlyList<TaskCompletionSource> Release { get; } =
    [
        .. Enumerable.Range(0, stepCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
    ];

    public async Task<TestState> RunAsync(TestState state, IWorkflowContext<TestState> context, CancellationToken ct)
    {
        for (var i = 0; i < stepCount; i++)
        {
            var stepState = new TestState(Step: i + 1);
            await context.ReportAsync(stepState, false, ct).ConfigureAwait(false);
            Reported[i].TrySetResult();
            await Release[i].Task.ConfigureAwait(false);
            await context.CheckpointAsync(ct).ConfigureAwait(false);
            if (i == throwAtStep) throw exception!;
        }

        return finalState;
    }
}

/// <summary>
///     A runner whose body never runs any await point before throwing/returning — used for trivial start/duplicate
///     tests.
/// </summary>
internal sealed class ImmediateRunner(Func<TestState, TestState> transform) : IWorkflowRunner<TestState>
{
    public Task<TestState> RunAsync(TestState state, IWorkflowContext<TestState> context, CancellationToken ct)
    {
        return Task.FromResult(transform(state));
    }
}

/// <summary>A runner that must never actually run — used to assert a workflow never left the concurrency queue.</summary>
internal sealed class UnreachableRunner : IWorkflowRunner<TestState>
{
    public bool WasCalled { get; private set; }

    public Task<TestState> RunAsync(TestState state, IWorkflowContext<TestState> context, CancellationToken ct)
    {
        WasCalled = true;
        throw new InvalidOperationException("This runner should never run.");
    }
}

/// <summary>Records every <see cref="IWorkflowObserver{TKey,TState}" /> callback and exposes a per-key terminal-outcome waiter.</summary>
internal sealed class RecordingObserver : IWorkflowObserver<string, TestState>
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, TaskCompletionSource<WorkflowTerminalResult<TestState>>> _terminal = new();

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

    public Task OnTerminalAsync(string key, WorkflowTerminalResult<TestState> result, CancellationToken ct)
    {
        GetOrAddTcs(key).TrySetResult(result);
        return Task.CompletedTask;
    }

    /// <summary>Waits for <paramref name="key" />'s terminal outcome. Safe to call before or after it happens.</summary>
    public Task<WorkflowTerminalResult<TestState>> WaitTerminalAsync(string key)
    {
        return GetOrAddTcs(key).Task;
    }

    private TaskCompletionSource<WorkflowTerminalResult<TestState>> GetOrAddTcs(string key)
    {
        lock (_lock)
        {
            if (_terminal.TryGetValue(key, out var tcs)) return tcs;
            tcs = new TaskCompletionSource<WorkflowTerminalResult<TestState>>(TaskCreationOptions
                .RunContinuationsAsynchronously);
            _terminal[key] = tcs;
            return tcs;
        }
    }
}

/// <summary>Mutable concurrency limit, so tests can change it mid-run.</summary>
internal sealed class FakeConcurrencyProvider(int initial) : IWorkflowConcurrencyProvider
{
    public int MaxConcurrentWorkflows { get; set; } = initial;
}

/// <summary>Returns a fixed, test-supplied list of workflows to resume.</summary>
internal sealed class FakeResumeSource(IReadOnlyList<PendingWorkflow<string, TestState>> pending)
    : IWorkflowResumeSource<string, TestState>
{
    public Task<IReadOnlyList<PendingWorkflow<string, TestState>>> GetPendingWorkflowsAsync(CancellationToken ct)
    {
        return Task.FromResult(pending);
    }
}