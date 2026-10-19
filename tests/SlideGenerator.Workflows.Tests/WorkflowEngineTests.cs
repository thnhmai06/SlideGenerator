/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows.Tests
 * File: WorkflowEngineTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Workflows.Engine;
using Xunit;

namespace SlideGenerator.Workflows.Tests;

/// <summary>
///     Domain-free behavioral tests for <see cref="WorkflowEngine{TKey,TState}" />, using
///     <see cref="ScriptedRunner" />/<see cref="RecordingObserver" /> to control and observe execution
///     deterministically (no <c>Thread.Sleep</c>/timing-based flakiness). Locks down the invariants recorded
///     in the Engine/Runner split design: pause/stop checkpointing,
///     non-blocking initialize, and terminal-state ownership rules.
/// </summary>
public sealed class WorkflowEngineTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan NoProgressWindow = TimeSpan.FromMilliseconds(250);

    private static (WorkflowEngine<string, TestState> Engine, RecordingObserver Observer) CreateEngine()
    {
        var observer = new RecordingObserver();
        var engine =
            new WorkflowEngine<string, TestState>(observer, NullLogger<WorkflowEngine<string, TestState>>.Instance);
        return (engine, observer);
    }

    private static (WorkflowEngine<string, TestState> Engine, HeldTerminalObserver Observer)
        CreateEngineWithHeldTerminal()
    {
        var observer = new HeldTerminalObserver();
        var engine =
            new WorkflowEngine<string, TestState>(observer, NullLogger<WorkflowEngine<string, TestState>>.Instance);
        return (engine, observer);
    }

    /// <summary>
    ///     Returns <see langword="true" /> if <paramref name="task" /> completes within <paramref name="timeout" />,
    ///     without throwing on timeout.
    /// </summary>
    private static async Task<bool> CompletesWithinAsync(Task task, TimeSpan timeout)
    {
        var winner = await Task.WhenAny(task, Task.Delay(timeout));
        return winner == task;
    }

    /// <summary>Pause must block a runner at its next checkpoint but never interrupt a step already in flight.</summary>
    [Fact]
    public async Task Pause_BlocksAtNextCheckpoint_ButNotMidStep()
    {
        var (engine, observer) = CreateEngine();
        var runner = new ScriptedRunner(2, new TestState(99));

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        (await engine.PauseAsync("workflow")).Should().BeTrue();
        runner.Release[0].SetResult(); // let the runner proceed into CheckpointAsync — should now block

        (await CompletesWithinAsync(runner.Reported[1].Task, NoProgressWindow)).Should()
            .BeFalse("pause should block the runner before it reaches step 1");

        (await engine.ResumeAsync("workflow")).Should().BeTrue();
        await runner.Reported[1].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        runner.Release[1].SetResult();
        var result = await observer.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Completed);
    }

    /// <summary>Stopping a paused workflow must unblock its checkpoint immediately rather than deadlocking.</summary>
    [Fact]
    public async Task Stop_UnblocksAPausedWorkflow_DoesNotDeadlock()
    {
        var (engine, observer) = CreateEngine();
        var runner = new ScriptedRunner(2, new TestState(99));

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        await engine.PauseAsync("workflow");
        runner.Release[0].SetResult();
        (await CompletesWithinAsync(runner.Reported[1].Task, NoProgressWindow)).Should().BeFalse();

        (await engine.StopAsync("workflow")).Should().BeTrue();

        var result = await observer.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Cancelled);
    }

    /// <summary>Shutting down while a workflow is paused must still complete, unblocking the pause rather than hanging forever.</summary>
    [Fact]
    public async Task Shutdown_WithAPausedWorkflow_DoesNotHang()
    {
        var (engine, observer) = CreateEngine();
        var runner = new ScriptedRunner(2, new TestState(99));

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        await engine.PauseAsync("workflow");
        runner.Release[0].SetResult();
        (await CompletesWithinAsync(runner.Reported[1].Task, NoProgressWindow)).Should().BeFalse();

        await engine.ShutdownAsync(TestContext.Current.CancellationToken)
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        var result = await observer.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Cancelled);
    }

    /// <summary>InitializeAsync must only schedule resumed workflows and return — never wait for them to finish.</summary>
    [Fact]
    public async Task Initialize_DoesNotWaitForResumedWorkflowsToFinish_AndStartsAllOfThem()
    {
        var (engine, observer) = CreateEngine();
        var runnerA = new ScriptedRunner(1, new TestState(1));
        var runnerB = new ScriptedRunner(1, new TestState(1));
        var pending = new List<PendingWorkflow<string, TestState>>
        {
            new("a", new TestState(0), runnerA),
            new("b", new TestState(0), runnerB)
        };

        // If InitializeAsync incorrectly awaited workflow completion, this would time out: neither workflow is
        // released yet, so they cannot possibly finish before this call returns.
        await engine.InitializeAsync(new FakeResumeSource(pending), TestContext.Current.CancellationToken)
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        await runnerA.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        await runnerB.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        observer.Progress.Should().Contain(p => p.Key == "a" && p.Durable);
        observer.Progress.Should().Contain(p => p.Key == "b" && p.Durable);

        runnerA.Release[0].SetResult();
        runnerB.Release[0].SetResult();
        await observer.WaitTerminalAsync("a").WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        await observer.WaitTerminalAsync("b").WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     On normal completion, the terminal state must be the runner's own return value — not whatever it last
    ///     reported.
    /// </summary>
    [Fact]
    public async Task Terminal_OnCompleted_UsesTheRunnersReturnValue_NotTheLastReportedState()
    {
        var (engine, observer) = CreateEngine();
        var returnedState = new TestState(999); // deliberately different from what gets reported mid-run
        var runner = new ScriptedRunner(1, returnedState);

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        runner.Release[0].SetResult();

        var result = await observer.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Completed);
        result.State.Should().Be(returnedState);
    }

    /// <summary>On fault, the terminal state must be the last state reported before the throw — never the initial state.</summary>
    [Fact]
    public async Task Terminal_OnFaulted_UsesTheLastReportedState_NotTheInitialState()
    {
        var (engine, observer) = CreateEngine();
        var exception = new InvalidOperationException("boom");
        var runner = new ScriptedRunner(2, new TestState(999), 1, exception);

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        runner.Release[0].SetResult();
        await runner.Reported[1].Task
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken); // last state reported before the throw
        runner.Release[1].SetResult();

        var result = await observer.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Faulted);
        result.Exception.Should().BeSameAs(exception);
        result.State.Should().Be(new TestState(2));
        result.State.Should().NotBe(new TestState(0));
    }

    /// <summary>
    ///     Starting a workflow with a key that's already registered is a caller bug — must throw, not silently
    ///     ignore/overwrite.
    /// </summary>
    [Fact]
    public async Task StartWorkflowAsync_DuplicateKey_ThrowsInvalidOperationException()
    {
        var (engine, _) = CreateEngine();
        var runner1 = new ScriptedRunner(1, new TestState(1));
        var runner2 = new UnreachableRunner();

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner1, TestContext.Current.CancellationToken);

        var act = async () => await engine.StartWorkflowAsync("workflow", new TestState(0), runner2);
        await act.Should().ThrowAsync<InvalidOperationException>();

        runner1.Release[0].SetResult();
        runner2.WasCalled.Should().BeFalse();
    }

    /// <summary>
    ///     <c>StopAsync(waitForExit: true)</c> must not complete until the runner has unwound and its terminal
    ///     outcome has actually been forwarded to the observer.
    /// </summary>
    [Fact]
    public async Task StopAsync_WaitForExit_CompletesOnlyAfterTerminalCallbackIsForwarded()
    {
        var (engine, observer) = CreateEngineWithHeldTerminal();
        var runner = new CancellationBlockingRunner();

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Entered.Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        var stop = engine.StopAsync("workflow", waitForExit: true);
        await observer.Entered.Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        (await CompletesWithinAsync(stop, NoProgressWindow)).Should()
            .BeFalse("the terminal callback is still held, so the workflow has not exited yet");

        observer.Release.SetResult();
        (await stop.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken)).Should().BeTrue();

        var result = await observer.Inner.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Cancelled);
    }

    /// <summary>
    ///     Stopping a paused workflow with <c>waitForExit: true</c> must unblock its checkpoint, wait for the
    ///     cancel to unwind, and report <see cref="WorkflowOutcome.Cancelled" />.
    /// </summary>
    [Fact]
    public async Task StopAsync_WaitForExitWhilePaused_UnblocksTheGateAndCompletes()
    {
        var (engine, observer) = CreateEngine();
        var runner = new ScriptedRunner(2, new TestState(99));

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Reported[0].Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        await engine.PauseAsync("workflow");
        runner.Release[0].SetResult();
        (await CompletesWithinAsync(runner.Reported[1].Task, NoProgressWindow)).Should().BeFalse();

        var stopped = await engine.StopAsync("workflow", waitForExit: true)
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        stopped.Should().BeTrue();

        var result = await observer.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Cancelled);
    }

    /// <summary>
    ///     Without <c>waitForExit</c>, <c>StopAsync</c> must return as soon as cancellation is requested, before
    ///     the runner has unwound or the observer has seen the terminal outcome.
    /// </summary>
    [Fact]
    public async Task StopAsync_WithoutWaitForExit_ReturnsBeforeTheRunnerHasExited()
    {
        var (engine, observer) = CreateEngineWithHeldTerminal();
        var runner = new CancellationBlockingRunner();

        await engine.StartWorkflowAsync("workflow", new TestState(0), runner, TestContext.Current.CancellationToken);
        await runner.Entered.Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        (await engine.StopAsync("workflow").WaitAsync(ShortTimeout, TestContext.Current.CancellationToken)).Should()
            .BeTrue();
        await observer.Entered.Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        (await CompletesWithinAsync(observer.Inner.WaitTerminalAsync("workflow"), NoProgressWindow)).Should()
            .BeFalse("the terminal callback is still held, so stop must not have waited for it");

        observer.Release.SetResult();
        var result = await observer.Inner.WaitTerminalAsync("workflow")
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        result.Outcome.Should().Be(WorkflowOutcome.Cancelled);
    }

    /// <summary>
    ///     Stopping an unknown key must return <see langword="false" /> immediately, even when asked to wait for
    ///     an exit that can never happen.
    /// </summary>
    [Fact]
    public async Task StopAsync_UnknownKey_ReturnsFalseEvenWhenWaiting()
    {
        var (engine, _) = CreateEngine();

        var stopped = await engine.StopAsync("missing", waitForExit: true)
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        stopped.Should().BeFalse();
    }

    /// <summary>
    ///     A runner that parks inside <see cref="Task.Delay(TimeSpan, CancellationToken)" /> until the engine
    ///     cancels it — nothing else can complete it, so it deterministically stays running.
    /// </summary>
    private sealed class CancellationBlockingRunner : IWorkflowRunner<TestState>
    {
        /// <summary>Signalled once the runner has entered its only await point.</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc />
        public async Task<TestState> RunAsync(TestState state, IWorkflowContext<TestState> context, CancellationToken ct)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            return state; // unreachable: the delay only completes by throwing on cancellation
        }
    }

    /// <summary>
    ///     Delegates every callback to a <see cref="RecordingObserver" /> but holds
    ///     <see cref="OnTerminalAsync" /> until the test releases it, so a test can prove whether a caller really
    ///     waits for the terminal outcome to be forwarded.
    /// </summary>
    private sealed class HeldTerminalObserver : IWorkflowObserver<string, TestState>
    {
        /// <summary>Signalled once <see cref="OnTerminalAsync" /> has been entered and is now held.</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completed by the test to let the held terminal callback return.</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>The callbacks recorded once <see cref="Release" /> is completed.</summary>
        public RecordingObserver Inner { get; } = new();

        /// <inheritdoc />
        public Task OnProgressAsync(string key, TestState state, bool durable, CancellationToken ct) =>
            Inner.OnProgressAsync(key, state, durable, ct);

        /// <inheritdoc />
        public Task OnPausedAsync(string key, TestState state, CancellationToken ct) =>
            Inner.OnPausedAsync(key, state, ct);

        /// <inheritdoc />
        public Task OnResumedAsync(string key, TestState state, CancellationToken ct) =>
            Inner.OnResumedAsync(key, state, ct);

        /// <inheritdoc />
        public async Task OnTerminalAsync(string key, WorkflowTerminalResult<TestState> result, CancellationToken ct)
        {
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            await Inner.OnTerminalAsync(key, result, ct).ConfigureAwait(false);
        }
    }
}