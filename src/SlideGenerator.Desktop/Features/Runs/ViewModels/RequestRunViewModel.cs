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
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop.Features.Runs.ViewModels;

/// <summary>
///     One request row + its detail panel. Owns its own Pause/Resume/Stop/Delete commands (all request-scoped
///     — <c>IService</c> has no per-workflow control) so each row is self-sufficient rather than routing actions
///     back through the parent list ViewModel.
/// </summary>
public sealed partial class RequestRunViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IService _service;

    /// <summary>Gets the id grouping every workflow of this request.</summary>
    public string RequestId { get; }

    /// <summary>Gets the recipe id this request was generated from — <see langword="null" /> for requests predating recipes.</summary>
    public int RecipeId { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private WorkflowStatus _status;
    [ObservableProperty] private RequestPhase? _phase;
    [ObservableProperty] private DateTimeOffset _createdAt;
    [ObservableProperty] private DateTimeOffset? _completedAt;
    [ObservableProperty] private ObservableCollection<LogEntry> _logs = [];
    [ObservableProperty] private bool _isLoadingLogs;

    /// <summary>Gets every workflow of this request, keyed by workflow id order.</summary>
    public ObservableCollection<WorkflowRunViewModel> Workflows { get; } = [];

    /// <summary>Constructs a row from an initial <see cref="Summary" /> fetched at load time.</summary>
    public RequestRunViewModel(string requestId, Summary summary, IService service, IDialogService dialogService)
    {
        RequestId = requestId;
        _service = service;
        _dialogService = dialogService;
        RecipeId = summary.Request.RecipeId;
        _name = summary.Request.Name;
        _status = summary.WorkflowStatus;
        _phase = summary.Phase;
        _createdAt = summary.CreatedAt;
        _completedAt = summary.CompletedAt;
        foreach (var (workflowId, workflowSummary) in summary.Workflows) Workflows.Add(new WorkflowRunViewModel(workflowId, workflowSummary));
    }

    /// <summary>Patches this row's workflow at <paramref name="snapshot" />'s <c>WorkflowId</c> from a live update.</summary>
    public void ApplyLiveWorkflowUpdate(WorkflowSnapshot snapshot)
    {
        var workflow = Workflows.FirstOrDefault(j => j.WorkflowId == snapshot.WorkflowId);
        workflow?.ApplyLiveUpdate(snapshot);
        Status = DeriveDisplayStatus();
        if (Status is WorkflowStatus.Complete or WorkflowStatus.Cancelled) CompletedAt ??= snapshot.Timestamp;
    }

    /// <summary>Patches this row's workflow at <paramref name="row" />'s <c>WorkflowId</c> with a live row-activity update.</summary>
    public void ApplyLiveRowUpdate(RowProgress row)
    {
        Workflows.FirstOrDefault(j => j.WorkflowId == row.WorkflowId)?.ApplyLiveRowUpdate(row);
    }

    private WorkflowStatus DeriveDisplayStatus()
    {
        if (Workflows.Count == 0) return WorkflowStatus.Complete;
        if (Workflows.Any(j => j.WorkflowStatus is WorkflowStatus.Running or WorkflowStatus.Pending)) return WorkflowStatus.Running;
        if (Workflows.Any(j => j.WorkflowStatus == WorkflowStatus.Paused)) return WorkflowStatus.Paused;
        if (Workflows.All(j => j.WorkflowStatus == WorkflowStatus.Cancelled)) return WorkflowStatus.Cancelled;
        return WorkflowStatus.Complete;
    }

    /// <summary>Loads this request's log entries (never fetched at list time — see <c>includeLogs</c>).</summary>
    public async Task LoadLogsAsync()
    {
        if (IsLoadingLogs || Logs.Count > 0) return;
        IsLoadingLogs = true;
        try
        {
            var summaries = Status is WorkflowStatus.Running or WorkflowStatus.Paused
                ? await _service.ListActiveAsync().ConfigureAwait(true)
                : await _service.ListCompletedAsync().ConfigureAwait(true);
            if (summaries.TryGetValue(RequestId, out var summary))
                Logs = new ObservableCollection<LogEntry>(summary.Logs.Concat(summary.Workflows.Values.SelectMany(j => j.Logs))
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
        return Status == WorkflowStatus.Running;
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private async Task ResumeAsync()
    {
        await _service.ResumeAsync(RequestId).ConfigureAwait(true);
    }

    private bool CanResume()
    {
        return Status == WorkflowStatus.Paused;
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        await _service.StopAsync(RequestId).ConfigureAwait(true);
    }

    private bool CanStop()
    {
        return Status is WorkflowStatus.Running or WorkflowStatus.Paused or WorkflowStatus.Pending;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await _dialogService
            .ConfirmAsync(LocalizationService.Instance["runs.delete.title"],
                string.Format(LocalizationService.Instance["runs.delete.message"], Name),
                LocalizationService.Instance["runs.delete.confirm"], LocalizationService.Instance["runs.delete.cancel"],
                danger: true)
            .ConfigureAwait(true);
        if (confirmed) await _service.DeleteAsync(RequestId).ConfigureAwait(true);
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var workflow = Workflows.FirstOrDefault();
        if (workflow is null) return;
        var folder = Path.GetDirectoryName(workflow.OutputPath);
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
