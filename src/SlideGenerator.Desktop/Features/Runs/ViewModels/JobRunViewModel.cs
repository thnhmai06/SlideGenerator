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
    [ObservableProperty] private string _outputPath;
    [ObservableProperty] private DateTimeOffset? _completedAt;

    /// <summary>Constructs a row from an initial <see cref="JobSummary" /> fetched at load time.</summary>
    public JobRunViewModel(int jobId, JobSummary summary)
    {
        JobId = jobId;
        _jobStatus = summary.JobStatus;
        _phase = summary.Phase;
        _currentIndex = summary.CurrentIndex;
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
        OutputPath = snapshot.OutputPath;
        if (snapshot.JobStatus is JobStatus.Complete or JobStatus.Cancelled or JobStatus.Error)
            CompletedAt = snapshot.Timestamp;
    }
}
