/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows
 * File: WorkflowEngine.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Workflows.Engine;

/// <summary>
///     Runs workflows concurrently, tracked by <typeparamref name="TKey" />, without knowing what a workflow does
///     or what <typeparamref name="TState" /> means. It imposes no concurrency limit of its own: a runner that fans
///     out into parallel work throttles that work itself. See <see cref="IWorkflowRunner{TState}" /> for the
///     domain-specific half of the contract.
/// </summary>
public interface IWorkflowEngine<TKey, TState> where TKey : notnull
{
    /// <summary>
    ///     Schedules every workflow returned by <paramref name="resumeSource" /> and returns — it does not wait
    ///     for any of them to finish. Call once at startup.
    /// </summary>
    Task InitializeAsync(IWorkflowResumeSource<TKey, TState> resumeSource, CancellationToken ct = default);

    /// <summary>
    ///     Cancels every running workflow, unblocks any that are paused, and waits for all of them to unwind
    ///     before returning — guaranteeing no runner is still calling
    ///     <see cref="IWorkflowContext{TState}.ReportAsync" /> once this completes. Call at shutdown.
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers and starts a fresh workflow. Throws <see cref="InvalidOperationException" /> if
    ///     <paramref name="key" /> is already registered — a duplicate key is a caller bug, not a state to
    ///     silently tolerate.
    /// </summary>
    Task StartWorkflowAsync(TKey key, TState initialState, IWorkflowRunner<TState> runner,
        CancellationToken ct = default);

    /// <summary>Pauses a running workflow at its next checkpoint. Returns <see langword="false" /> if not currently registered.</summary>
    Task<bool> PauseAsync(TKey key);

    /// <summary>Resumes a paused workflow. Returns <see langword="false" /> if not currently registered.</summary>
    Task<bool> ResumeAsync(TKey key);

    /// <summary>
    ///     Cancels a running or paused workflow. Returns <see langword="false" /> if not currently registered.
    ///     When <paramref name="waitForExit" /> is <see langword="true" />, the returned task completes only after
    ///     the runner has unwound and its terminal outcome has been forwarded to the observer, so the caller can
    ///     safely delete the workflow's data afterwards. Never await it from inside the runner itself — it would
    ///     wait for itself.
    /// </summary>
    Task<bool> StopAsync(TKey key, bool waitForExit = false);
}

/// <summary>
///     Runs each workflow's <see cref="IWorkflowRunner{TState}" /> on its own <see cref="Task.Run(Action)" />. See
///     <see cref="IWorkflowEngine{TKey,TState}" /> for the full contract — this type knows nothing about what a
///     workflow does, or what <typeparamref name="TState" />/<typeparamref name="TKey" /> mean.
/// </summary>
public sealed class WorkflowEngine<TKey, TState>(
    IWorkflowObserver<TKey, TState> observer,
    ILogger<WorkflowEngine<TKey, TState>> logger) : IWorkflowEngine<TKey, TState> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, RunningWorkflow> _running = new();

    /// <inheritdoc />
    public async Task InitializeAsync(IWorkflowResumeSource<TKey, TState> resumeSource, CancellationToken ct = default)
    {
        var pending = await resumeSource.GetPendingWorkflowsAsync(ct).ConfigureAwait(false);
        foreach (var workflow in pending)
        {
            var running = Register(workflow.Key, workflow.State);
            running.RunTask = Task.Run(
                () => RunWorkflowAsync(workflow.Key, workflow.State, workflow.Runner, true, running),
                CancellationToken.None);
        }

        logger.LogInformation("WorkflowEngine initialized. Resumed {Count} pending workflow(s).", pending.Count);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        foreach (var workflow in _running.Values)
        {
            await workflow.Cts.CancelAsync().ConfigureAwait(false);
            workflow.Gate.Resume(); // unblock a paused checkpoint so cancellation is observed promptly
        }

        await Task.WhenAll(_running.Values.Select(w => w.RunTask ?? Task.CompletedTask))
            .ContinueWith(_ => { }, TaskScheduler.Default).ConfigureAwait(false);
        logger.LogInformation("WorkflowEngine shut down.");
    }

    /// <inheritdoc />
    public Task StartWorkflowAsync(
        TKey key, TState initialState, IWorkflowRunner<TState> runner, CancellationToken ct = default)
    {
        var running = Register(key, initialState);
        running.RunTask = Task.Run(
            () => RunWorkflowAsync(key, initialState, runner, false, running), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> PauseAsync(TKey key)
    {
        if (!_running.TryGetValue(key, out var workflow)) return false;
        workflow.Gate.Pause();
        await observer.OnPausedAsync(key, workflow.GetLastState(), CancellationToken.None).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ResumeAsync(TKey key)
    {
        if (!_running.TryGetValue(key, out var workflow)) return false;
        workflow.Gate.Resume();
        await observer.OnResumedAsync(key, workflow.GetLastState(), CancellationToken.None).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> StopAsync(TKey key, bool waitForExit = false)
    {
        if (!_running.TryGetValue(key, out var workflow)) return false;
        workflow.Cts.Cancel();
        workflow.Gate.Resume(); // unblock a paused checkpoint so cancellation is observed promptly
        if (waitForExit) await workflow.Exited.Task.ConfigureAwait(false);
        return true;
    }

    /// <summary>Registers a new running workflow. Throws if <paramref name="key" /> is already registered.</summary>
    private RunningWorkflow Register(TKey key, TState initialState)
    {
        var workflow = new RunningWorkflow(initialState);
        return _running.TryAdd(key, workflow)
            ? workflow
            : throw new InvalidOperationException($"Workflow '{key}' is already registered.");
    }

    /// <summary>
    ///     Runs one workflow to completion (or until cancelled/faulted), publishing the initial "starting
    ///     execution" progress first, then the terminal outcome on exit.
    /// </summary>
    private async Task RunWorkflowAsync(
        TKey key, TState initialState, IWorkflowRunner<TState> runner, bool isResume, RunningWorkflow running)
    {
        var ct = running.Cts.Token;
        try
        {
            await observer.OnProgressAsync(key, initialState, true, CancellationToken.None)
                .ConfigureAwait(false);

            var context = new WorkflowContext(isResume, running, observer, key);
            var finalState = await runner.RunAsync(initialState, context, ct).ConfigureAwait(false);
            await observer.OnTerminalAsync(key,
                new WorkflowTerminalResult<TState>(WorkflowOutcome.Completed, finalState, null),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (running.Cts.IsCancellationRequested)
        {
            await observer.OnTerminalAsync(key,
                new WorkflowTerminalResult<TState>(WorkflowOutcome.Cancelled, running.GetLastState(), null),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow {Key} failed.", key);
            await observer.OnTerminalAsync(key,
                new WorkflowTerminalResult<TState>(WorkflowOutcome.Faulted, running.GetLastState(), ex),
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _running.TryRemove(key, out _);
            running.Exited.TrySetResult();
        }
    }

    /// <summary>In-memory registry entry for one currently running (or resumed) workflow.</summary>
    private sealed class RunningWorkflow(TState initialState)
    {
        private readonly Lock _lock = new();
        private TState _lastState = initialState;

        public CancellationTokenSource Cts { get; } = new();
        public PauseGate Gate { get; } = new();
        public Task? RunTask { get; set; }

        /// <summary>Completed once the runner has finished and its terminal outcome has been forwarded to the observer.</summary>
        public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void SetLastState(TState state)
        {
            lock (_lock)
            {
                _lastState = state;
            }
        }

        public TState GetLastState()
        {
            lock (_lock)
            {
                return _lastState;
            }
        }
    }

    /// <summary>
    ///     Bridges a running workflow's <see cref="RunningWorkflow" />/<see cref="IWorkflowObserver{TKey,TState}" />
    ///     to the <see cref="IWorkflowContext{TState}" /> a runner sees.
    /// </summary>
    private sealed class WorkflowContext(
        bool isResume, RunningWorkflow running, IWorkflowObserver<TKey, TState> observer, TKey key)
        : IWorkflowContext<TState>
    {
        public bool IsResume { get; } = isResume;

        public async Task ReportAsync(TState state, bool durable = false, CancellationToken ct = default)
        {
            running.SetLastState(state);
            await observer.OnProgressAsync(key, state, durable, ct).ConfigureAwait(false);
        }

        public Task CheckpointAsync(CancellationToken ct)
        {
            return running.Gate.CheckpointAsync(ct);
        }
    }
}
