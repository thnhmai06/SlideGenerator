/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: WorkflowRunViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop.Features.Runs.ViewModels;

/// <summary>
///     One read-only workflow row within a request's detail panel. <c>IService</c> exposes control only
///     at the request level (<c>Stop/Pause/Resume(requestId)</c>) — there is no per-workflow command here by
///     design, matching what the backend actually allows.
/// </summary>
public sealed partial class WorkflowRunViewModel : ObservableObject
{
    /// <summary>Gets the workflow's ordinal position within its request.</summary>
    public int WorkflowId { get; }

    [ObservableProperty] private WorkflowStatus _workflowStatus;
    [ObservableProperty] private WorkflowPhase _phase;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int? _totalRows;
    [ObservableProperty] private string _outputPath;
    [ObservableProperty] private DateTimeOffset? _completedAt;

    /// <summary>Gets the current row's stage, for the live activity line — <see cref="RowStage.None" /> hides
    ///     it (no image-editing sub-stage in progress right now). Never persisted — live-only, sourced from
    ///     <c>IProgressHub.Rows</c> (plan §5.4: "dòng hoạt động live").</summary>
    [ObservableProperty] private RowStage _currentActivityStage;

    /// <summary>Gets the current row's free-text note (e.g. the URL being downloaded) — <see langword="null" />
    ///     until the first live row update arrives.</summary>
    [ObservableProperty] private string? _currentActivityNote;

    /// <summary>Constructs a row from an initial <see cref="WorkflowSummary" /> fetched at load time.</summary>
    public WorkflowRunViewModel(int workflowId, WorkflowSummary summary)
    {
        WorkflowId = workflowId;
        _workflowStatus = summary.WorkflowStatus;
        _phase = summary.Phase;
        _currentIndex = summary.CurrentIndex;
        _totalRows = summary.TotalRows;
        _outputPath = summary.OutputPath;
        _completedAt = summary.CompletedAt;
    }

    /// <summary>
    ///     Patches this row from a live <see cref="WorkflowSnapshot" /> (see <c>IProgressHub</c>) — called on
    ///     every drain tick instead of re-fetching the whole request from the backend.
    /// </summary>
    public void ApplyLiveUpdate(WorkflowSnapshot snapshot)
    {
        WorkflowStatus = snapshot.WorkflowStatus;
        Phase = snapshot.Phase;
        CurrentIndex = snapshot.CurrentIndex;
        TotalRows = snapshot.TotalRows;
        OutputPath = snapshot.OutputPath;
        if (snapshot.WorkflowStatus is WorkflowStatus.Complete or WorkflowStatus.Cancelled or WorkflowStatus.Error)
            CompletedAt = snapshot.Timestamp;
    }

    /// <summary>Patches the live activity line from a <see cref="RowProgress" /> event (see <c>IProgressHub.Rows</c>) —
    ///     only the most recent row matters here, so this always overwrites rather than accumulating.</summary>
    public void ApplyLiveRowUpdate(RowProgress row)
    {
        CurrentActivityStage = row.Stage;
        CurrentActivityNote = row.Note;
    }
}
