/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: JobRunViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop.Features.Runs.ViewModels;

/// <summary>
///     One read-only job row within a request's detail panel. <c>IService</c> exposes control only
///     at the request level (<c>Stop/Pause/Resume(requestId)</c>) — there is no per-job command here by
///     design, matching what the backend actually allows.
/// </summary>
public sealed partial class JobRunViewModel : ObservableObject
{
    /// <summary>Gets the job's ordinal position within its request.</summary>
    public int JobId { get; }

    [ObservableProperty] private JobStatus _jobStatus;
    [ObservableProperty] private JobPhase _phase;
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

    /// <summary>Constructs a row from an initial <see cref="JobSummary" /> fetched at load time.</summary>
    public JobRunViewModel(int jobId, JobSummary summary)
    {
        JobId = jobId;
        _jobStatus = summary.JobStatus;
        _phase = summary.Phase;
        _currentIndex = summary.CurrentIndex;
        _totalRows = summary.TotalRows;
        _outputPath = summary.OutputPath;
        _completedAt = summary.CompletedAt;
    }

    /// <summary>
    ///     Patches this row from a live <see cref="JobSnapshot" /> (see <c>IProgressHub</c>) — called on
    ///     every drain tick instead of re-fetching the whole request from the backend.
    /// </summary>
    public void ApplyLiveUpdate(JobSnapshot snapshot)
    {
        JobStatus = snapshot.JobStatus;
        Phase = snapshot.Phase;
        CurrentIndex = snapshot.CurrentIndex;
        TotalRows = snapshot.TotalRows;
        OutputPath = snapshot.OutputPath;
        if (snapshot.JobStatus is JobStatus.Complete or JobStatus.Cancelled or JobStatus.Error)
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
