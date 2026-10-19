/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ProgressHub.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;

namespace SlideGenerator.Desktop.Services.Progress;

/// <summary>
///     Marshals <see cref="GeneratingEventBus" />/<see cref="LogNotifier" /> events (raised from background
///     threads, potentially very dense — one <see cref="RowProgress" /> per row per stage per concurrently
///     running job) onto the UI thread in coalesced batches, so ViewModels never touch the raw event bus.
/// </summary>
/// <remarks>
///     <para>
///         Must be constructed, and its constructor's subscriptions attached, <b>before</b>
///         <c>IService.InitializeAsync()</c> is called — <c>WorkflowController.InitializeAsync</c> schedules
///         crash-resumed workflows immediately and returns without waiting, so a workflow resumed from a previous
///         crash is not a new request and its first progress events would be missed by a subscriber that attaches
///         any later. See <c>App.axaml.cs</c>'s startup sequence.
///     </para>
///     <para>
///         Workflow snapshots coalesce by <c>WorkflowId</c> and job snapshots by <c>(WorkflowId, JobId)</c> (last
///         write wins), row progress coalesces by
///         <c>(WorkflowId, JobId, RowIndex)</c>, and log lines are append-only (every line matters, never
///         dropped) — mirroring the old <c>ProgressCoalescer</c> that existed in the removed IPC sidecar,
///         adapted for direct in-process consumption instead of JSON-RPC notifications.
///     </para>
/// </remarks>
public interface IProgressHub
{
    /// <summary>Gets the current snapshot of every job seen so far, updated on the UI thread every ~250ms.</summary>
    ObservableCollection<JobSnapshot> Jobs { get; }

    /// <summary>Gets the most recent progress per <c>(WorkflowId, JobId, RowIndex)</c>, updated on the UI thread.</summary>
    ObservableCollection<RowProgress> Rows { get; }

    /// <summary>Gets every log line seen so far, in arrival order, updated on the UI thread.</summary>
    ObservableCollection<LogEntry> Logs { get; }

    /// <summary>
    ///     Gets the latest state of every workflow seen so far (a workflow's own lifecycle, e.g. paused), updated on
    ///     the UI thread every ~250ms.
    /// </summary>
    ObservableCollection<WorkflowSnapshot> Workflows { get; }
}

/// <inheritdoc cref="IProgressHub" />
internal sealed class ProgressHub : IProgressHub, IDisposable
{
    private static readonly TimeSpan DrainInterval = TimeSpan.FromMilliseconds(250);

    private readonly ConcurrentDictionary<string, WorkflowSnapshot> _dirtyWorkflows = new();
    private readonly ConcurrentDictionary<(string WorkflowId, int JobId), JobSnapshot> _dirtyJobs = new();
    private readonly ConcurrentDictionary<(string WorkflowId, int JobId, int RowIndex), RowProgress> _dirtyRows = new();
    private readonly ConcurrentQueue<LogEntry> _pendingLogs = new();
    private readonly DispatcherTimer _timer;

    /// <summary>
    ///     Subscribes to <paramref name="eventBus" />/<paramref name="logNotifier" /> and starts the drain
    ///     timer. The concrete types (not <see cref="IEventBus" />/<see cref="ILogNotifier" />) are required
    ///     because the C# events this class subscribes to are declared on the concrete classes, not the
    ///     publish-only interfaces <c>SlideGenerator.Generator</c> depends on.
    /// </summary>
    public ProgressHub(GeneratingEventBus eventBus, LogNotifier logNotifier)
    {
        eventBus.OnJobProgress += job => _dirtyJobs[(job.WorkflowId, job.JobId)] = job;
        eventBus.OnRowProgress += row => _dirtyRows[(row.WorkflowId, row.JobId, row.RowIndex)] = row;
        eventBus.OnWorkflowProgress += workflow => _dirtyWorkflows[workflow.WorkflowId] = workflow;
        logNotifier.OnLogEntry += entry => _pendingLogs.Enqueue(entry);

        _timer = new DispatcherTimer { Interval = DrainInterval };
        _timer.Tick += (_, _) => Drain();
        _timer.Start();
    }

    /// <inheritdoc />
    public ObservableCollection<JobSnapshot> Jobs { get; } = [];

    /// <inheritdoc />
    public ObservableCollection<RowProgress> Rows { get; } = [];

    /// <inheritdoc />
    public ObservableCollection<LogEntry> Logs { get; } = [];

    /// <inheritdoc />
    public ObservableCollection<WorkflowSnapshot> Workflows { get; } = [];

    /// <summary>Stops the drain timer. Safe to call multiple times.</summary>
    public void Dispose()
    {
        _timer.Stop();
    }

    /// <summary>Drains coalesced batches into the observable collections. Internal so tests can call it
    ///     directly instead of waiting on the real timer tick.</summary>
    internal void Drain()
    {
        if (!_dirtyWorkflows.IsEmpty)
        {
            var batch = _dirtyWorkflows.Values;
            foreach (var workflow in batch) UpsertByKey(Workflows, workflow, w => w.WorkflowId == workflow.WorkflowId);
            _dirtyWorkflows.Clear();
        }

        if (!_dirtyJobs.IsEmpty)
        {
            var batch = _dirtyJobs.Values;
            foreach (var job in batch) UpsertByKey(Jobs, job, j => j.WorkflowId == job.WorkflowId && j.JobId == job.JobId);
            _dirtyJobs.Clear();
        }

        if (!_dirtyRows.IsEmpty)
        {
            var batch = _dirtyRows.Values;
            foreach (var row in batch)
                UpsertByKey(Rows, row,
                    r => r.WorkflowId == row.WorkflowId && r.JobId == row.JobId && r.RowIndex == row.RowIndex);
            _dirtyRows.Clear();
        }

        while (_pendingLogs.TryDequeue(out var log)) Logs.Add(log);
    }

    private static void UpsertByKey<T>(ObservableCollection<T> collection, T value, Func<T, bool> isSameKey)
    {
        for (var i = 0; i < collection.Count; i++)
            if (isSameKey(collection[i]))
            {
                collection[i] = value;
                return;
            }

        collection.Add(value);
    }
}
