/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs.Tests
 * File: JobEngineTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Jobs.Engine;
using Xunit;

namespace SlideGenerator.Jobs.Tests;

/// <summary>
///     Domain-free behavioral tests for <see cref="JobEngine{TKey,TState}" />, using
///     <see cref="ScriptedWorkload" />/<see cref="RecordingObserver" /> to control and observe execution
///     deterministically (no <c>Thread.Sleep</c>/timing-based flakiness). Locks down the invariants recorded
///     in the Engine/Workload split design: pause/stop checkpointing, semaphore swap-not-resize,
///     non-blocking initialize, and terminal-state ownership rules.
/// </summary>
public sealed class JobEngineTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan NoProgressWindow = TimeSpan.FromMilliseconds(250);

    private static (JobEngine<string, TestState> Engine, RecordingObserver Observer, FakeConcurrencyProvider Concurrency
        )
        CreateEngine(int maxConcurrent = 5)
    {
        var concurrency = new FakeConcurrencyProvider(maxConcurrent);
        var observer = new RecordingObserver();
        var engine =
            new JobEngine<string, TestState>(concurrency, observer, NullLogger<JobEngine<string, TestState>>.Instance);
        return (engine, observer, concurrency);
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

    /// <summary>Pause must block a workload at its next checkpoint, but never interrupt a step already in flight.</summary>
    [Fact]
    public async Task Pause_BlocksAtNextCheckpoint_ButNotMidStep()
    {
        var (engine, observer, _) = CreateEngine();
        var workload = new ScriptedWorkload(2, new TestState(99));

        await engine.StartJobAsync("job", new TestState(0), workload);
        await workload.Reported[0].Task.WaitAsync(ShortTimeout);

        (await engine.PauseAsync("job")).Should().BeTrue();
        workload.Release[0].SetResult(); // let the workload proceed into CheckpointAsync — should now block

        (await CompletesWithinAsync(workload.Reported[1].Task, NoProgressWindow)).Should()
            .BeFalse("pause should block the workload before it reaches step 1");

        (await engine.ResumeAsync("job")).Should().BeTrue();
        await workload.Reported[1].Task.WaitAsync(ShortTimeout);

        workload.Release[1].SetResult();
        var result = await observer.WaitTerminalAsync("job").WaitAsync(ShortTimeout);
        result.Outcome.Should().Be(JobOutcome.Completed);
    }

    /// <summary>Stopping a paused job must unblock its checkpoint immediately rather than deadlocking.</summary>
    [Fact]
    public async Task Stop_UnblocksAPausedJob_DoesNotDeadlock()
    {
        var (engine, observer, _) = CreateEngine();
        var workload = new ScriptedWorkload(2, new TestState(99));

        await engine.StartJobAsync("job", new TestState(0), workload);
        await workload.Reported[0].Task.WaitAsync(ShortTimeout);

        await engine.PauseAsync("job");
        workload.Release[0].SetResult();
        (await CompletesWithinAsync(workload.Reported[1].Task, NoProgressWindow)).Should().BeFalse();

        (await engine.StopAsync("job")).Should().BeTrue();

        var result = await observer.WaitTerminalAsync("job").WaitAsync(ShortTimeout);
        result.Outcome.Should().Be(JobOutcome.Cancelled);
    }

    /// <summary>A job stopped while still queued for a concurrency slot must end Cancelled without ever running its workload.</summary>
    [Fact]
    public async Task Stop_BeforeSemaphoreAcquired_NeverRunsTheWorkload()
    {
        var (engine, observer, _) = CreateEngine(1);

        var blocker = new ScriptedWorkload(1, new TestState(1));
        await engine.StartJobAsync("blocker", new TestState(0), blocker);
        await blocker.Reported[0].Task.WaitAsync(ShortTimeout); // blocker now holds the only slot

        var queued = new UnreachableWorkload();
        await engine.StartJobAsync("queued", new TestState(0), queued);
        (await engine.StopAsync("queued")).Should().BeTrue();

        var result = await observer.WaitTerminalAsync("queued").WaitAsync(ShortTimeout);
        result.Outcome.Should().Be(JobOutcome.Cancelled);
        queued.WasCalled.Should().BeFalse();

        blocker.Release[0].SetResult();
        await observer.WaitTerminalAsync("blocker").WaitAsync(ShortTimeout);
    }

    /// <summary>Shutting down while a job is paused must still complete, unblocking the pause rather than hanging forever.</summary>
    [Fact]
    public async Task Shutdown_WithAPausedJob_DoesNotHang()
    {
        var (engine, observer, _) = CreateEngine();
        var workload = new ScriptedWorkload(2, new TestState(99));

        await engine.StartJobAsync("job", new TestState(0), workload);
        await workload.Reported[0].Task.WaitAsync(ShortTimeout);

        await engine.PauseAsync("job");
        workload.Release[0].SetResult();
        (await CompletesWithinAsync(workload.Reported[1].Task, NoProgressWindow)).Should().BeFalse();

        await engine.ShutdownAsync().WaitAsync(ShortTimeout);

        var result = await observer.WaitTerminalAsync("job").WaitAsync(ShortTimeout);
        result.Outcome.Should().Be(JobOutcome.Cancelled);
    }

    /// <summary>The concurrency limit must hold: a job beyond the cap waits until a slot frees up.</summary>
    [Fact]
    public async Task ConcurrencyCap_LimitsParallelJobs()
    {
        var (engine, observer, _) = CreateEngine(1);
        var a = new ScriptedWorkload(1, new TestState(1));
        var b = new ScriptedWorkload(1, new TestState(1));

        await engine.StartJobAsync("a", new TestState(0), a);
        await a.Reported[0].Task.WaitAsync(ShortTimeout);

        await engine.StartJobAsync("b", new TestState(0), b);
        (await CompletesWithinAsync(b.Reported[0].Task, NoProgressWindow)).Should().BeFalse("b must wait for a's slot");

        a.Release[0].SetResult();
        await observer.WaitTerminalAsync("a").WaitAsync(ShortTimeout);
        await b.Reported[0].Task.WaitAsync(ShortTimeout);

        b.Release[0].SetResult();
        await observer.WaitTerminalAsync("b").WaitAsync(ShortTimeout);
    }

    /// <summary>
    ///     Raising the limit mid-run swaps in a new semaphore instance rather than resizing the shared one — a job
    ///     started after the swap is independent of jobs still holding the old one.
    /// </summary>
    [Fact]
    public async Task ConcurrencyLimit_SwapDoesNotAffectAlreadyRunningJobs()
    {
        var (engine, observer, concurrency) = CreateEngine(1);
        var a = new ScriptedWorkload(1, new TestState(1));

        await engine.StartJobAsync("a", new TestState(0), a);
        await a.Reported[0].Task.WaitAsync(ShortTimeout); // a holds the only slot on the original semaphore

        concurrency.MaxConcurrentJobs = 5; // takes effect only on the next StartJobAsync/InitializeAsync
        var b = new ScriptedWorkload(1, new TestState(1));
        await engine.StartJobAsync("b", new TestState(0), b); // swaps in a fresh semaphore(5)
        await b.Reported[0].Task.WaitAsync(ShortTimeout); // b runs immediately despite a still holding its own slot

        a.Release[0].SetResult();
        b.Release[0].SetResult();
        (await observer.WaitTerminalAsync("a").WaitAsync(ShortTimeout)).Outcome.Should().Be(JobOutcome.Completed);
        (await observer.WaitTerminalAsync("b").WaitAsync(ShortTimeout)).Outcome.Should().Be(JobOutcome.Completed);
    }

    /// <summary>InitializeAsync must only schedule resumed jobs and return — never wait for them to finish.</summary>
    [Fact]
    public async Task Initialize_DoesNotWaitForResumedJobsToFinish_AndStartsAllOfThem()
    {
        var (engine, observer, _) = CreateEngine();
        var workloadA = new ScriptedWorkload(1, new TestState(1));
        var workloadB = new ScriptedWorkload(1, new TestState(1));
        var pending = new List<PendingJob<string, TestState>>
        {
            new("a", new TestState(0), workloadA),
            new("b", new TestState(0), workloadB)
        };

        // If InitializeAsync incorrectly awaited job completion, this would time out: neither job is
        // released yet, so they cannot possibly finish before this call returns.
        await engine.InitializeAsync(new FakeResumeSource(pending)).WaitAsync(ShortTimeout);

        await workloadA.Reported[0].Task.WaitAsync(ShortTimeout);
        await workloadB.Reported[0].Task.WaitAsync(ShortTimeout);

        observer.Progress.Should().Contain(p => p.Key == "a" && p.Durable);
        observer.Progress.Should().Contain(p => p.Key == "b" && p.Durable);

        workloadA.Release[0].SetResult();
        workloadB.Release[0].SetResult();
        await observer.WaitTerminalAsync("a").WaitAsync(ShortTimeout);
        await observer.WaitTerminalAsync("b").WaitAsync(ShortTimeout);
    }

    /// <summary>
    ///     On normal completion, the terminal state must be the workload's own return value — not whatever it last
    ///     reported.
    /// </summary>
    [Fact]
    public async Task Terminal_OnCompleted_UsesTheWorkloadsReturnValue_NotTheLastReportedState()
    {
        var (engine, observer, _) = CreateEngine();
        var returnedState = new TestState(999); // deliberately different from what gets reported mid-run
        var workload = new ScriptedWorkload(1, returnedState);

        await engine.StartJobAsync("job", new TestState(0), workload);
        await workload.Reported[0].Task.WaitAsync(ShortTimeout);
        workload.Release[0].SetResult();

        var result = await observer.WaitTerminalAsync("job").WaitAsync(ShortTimeout);
        result.Outcome.Should().Be(JobOutcome.Completed);
        result.State.Should().Be(returnedState);
    }

    /// <summary>On fault, the terminal state must be the last state reported before the throw — never the initial state.</summary>
    [Fact]
    public async Task Terminal_OnFaulted_UsesTheLastReportedState_NotTheInitialState()
    {
        var (engine, observer, _) = CreateEngine();
        var exception = new InvalidOperationException("boom");
        var workload = new ScriptedWorkload(2, new TestState(999), 1, exception);

        await engine.StartJobAsync("job", new TestState(0), workload);
        await workload.Reported[0].Task.WaitAsync(ShortTimeout);
        workload.Release[0].SetResult();
        await workload.Reported[1].Task.WaitAsync(ShortTimeout); // last state reported before the throw
        workload.Release[1].SetResult();

        var result = await observer.WaitTerminalAsync("job").WaitAsync(ShortTimeout);
        result.Outcome.Should().Be(JobOutcome.Faulted);
        result.Exception.Should().BeSameAs(exception);
        result.State.Should().Be(new TestState(2));
        result.State.Should().NotBe(new TestState(0));
    }

    /// <summary>
    ///     Starting a job with a key that's already registered is a caller bug — must throw, not silently
    ///     ignore/overwrite.
    /// </summary>
    [Fact]
    public async Task StartJobAsync_DuplicateKey_ThrowsInvalidOperationException()
    {
        var (engine, _, _) = CreateEngine();
        var workload1 = new ScriptedWorkload(1, new TestState(1));
        var workload2 = new UnreachableWorkload();

        await engine.StartJobAsync("job", new TestState(0), workload1);

        var act = async () => await engine.StartJobAsync("job", new TestState(0), workload2);
        await act.Should().ThrowAsync<InvalidOperationException>();

        workload1.Release[0].SetResult();
        workload2.WasCalled.Should().BeFalse();
    }
}