/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RequestRunViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop.Features.Runs.ViewModels;

/// <summary>
///     One request row + its detail panel. Owns its own Pause/Resume/Stop/Delete commands (all request-scoped
///     — <c>IService</c> has no per-job control) so each row is self-sufficient rather than routing actions
///     back through the parent list ViewModel.
/// </summary>
public sealed partial class RequestRunViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IService _service;

    /// <summary>Gets the id grouping every job of this request.</summary>
    public string RequestId { get; }

    /// <summary>Gets the recipe id this request was generated from — <see langword="null" /> for requests predating recipes.</summary>
    public int RecipeId { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private JobStatus _status;
    [ObservableProperty] private RequestPhase? _phase;
    [ObservableProperty] private DateTimeOffset _createdAt;
    [ObservableProperty] private DateTimeOffset? _completedAt;
    [ObservableProperty] private ObservableCollection<LogEntry> _logs = [];
    [ObservableProperty] private bool _isLoadingLogs;

    /// <summary>Gets every job of this request, keyed by job id order.</summary>
    public ObservableCollection<JobRunViewModel> Jobs { get; } = [];

    /// <summary>Constructs a row from an initial <see cref="Summary" /> fetched at load time.</summary>
    public RequestRunViewModel(string requestId, Summary summary, IService service, IDialogService dialogService)
    {
        RequestId = requestId;
        _service = service;
        _dialogService = dialogService;
        RecipeId = summary.Request.RecipeId;
        _name = summary.Request.Name;
        _status = summary.JobStatus;
        _phase = summary.Phase;
        _createdAt = summary.CreatedAt;
        _completedAt = summary.CompletedAt;
        foreach (var (jobId, jobSummary) in summary.Jobs) Jobs.Add(new JobRunViewModel(jobId, jobSummary));
    }

    /// <summary>Patches this row's job at <paramref name="snapshot" />'s <c>JobId</c> from a live update.</summary>
    public void ApplyLiveJobUpdate(JobSnapshot snapshot)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == snapshot.JobId);
        job?.ApplyLiveUpdate(snapshot);
        Status = DeriveDisplayStatus();
        if (Status is JobStatus.Complete or JobStatus.Cancelled) CompletedAt ??= snapshot.Timestamp;
    }

    private JobStatus DeriveDisplayStatus()
    {
        if (Jobs.Count == 0) return JobStatus.Complete;
        if (Jobs.Any(j => j.JobStatus is JobStatus.Running or JobStatus.Pending)) return JobStatus.Running;
        if (Jobs.Any(j => j.JobStatus == JobStatus.Paused)) return JobStatus.Paused;
        if (Jobs.All(j => j.JobStatus == JobStatus.Cancelled)) return JobStatus.Cancelled;
        return JobStatus.Complete;
    }

    /// <summary>Loads this request's log entries (never fetched at list time — see <c>includeLogs</c>).</summary>
    public async Task LoadLogsAsync()
    {
        if (IsLoadingLogs || Logs.Count > 0) return;
        IsLoadingLogs = true;
        try
        {
            var summaries = Status is JobStatus.Running or JobStatus.Paused
                ? await _service.ListActiveAsync().ConfigureAwait(true)
                : await _service.ListCompletedAsync().ConfigureAwait(true);
            if (summaries.TryGetValue(RequestId, out var summary))
                Logs = new ObservableCollection<LogEntry>(summary.Logs.Concat(summary.Jobs.Values.SelectMany(j => j.Logs))
                    .OrderBy(e => e.Timestamp));
        }
        finally
        {
            IsLoadingLogs = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private async Task PauseAsync()
    {
        await _service.PauseAsync(RequestId).ConfigureAwait(true);
    }

    private bool CanPause()
    {
        return Status == JobStatus.Running;
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private async Task ResumeAsync()
    {
        await _service.ResumeAsync(RequestId).ConfigureAwait(true);
    }

    private bool CanResume()
    {
        return Status == JobStatus.Paused;
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        await _service.StopAsync(RequestId).ConfigureAwait(true);
    }

    private bool CanStop()
    {
        return Status is JobStatus.Running or JobStatus.Paused or JobStatus.Pending;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await _dialogService
            .ConfirmAsync("Xoá yêu cầu", $"Xoá vĩnh viễn \"{Name}\" và toàn bộ dữ liệu của nó?", "Xoá", "Huỷ")
            .ConfigureAwait(true);
        if (confirmed) await _service.DeleteAsync(RequestId).ConfigureAwait(true);
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var job = Jobs.FirstOrDefault();
        if (job is null) return;
        var folder = Path.GetDirectoryName(job.OutputPath);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

        var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo("explorer.exe", $"\"{folder}\"")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? new ProcessStartInfo("open", $"\"{folder}\"")
                : new ProcessStartInfo("xdg-open", $"\"{folder}\"");
        startInfo.UseShellExecute = false;
        Process.Start(startInfo);
    }
}
