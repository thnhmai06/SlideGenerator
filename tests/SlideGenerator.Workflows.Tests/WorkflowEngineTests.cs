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
}