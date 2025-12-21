using SlideGenerator.Domain.Job.Components;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Sheet.Interfaces;

namespace SlideGenerator.Domain.Job.Entities;

/// <summary>
///     Represents a single worksheet job that generates one output presentation.
/// </summary>
public sealed class JobSheet : IJobSheet
{
    private readonly PauseSignal _pauseSignal = new();

    /// <summary>
    ///     Creates a new sheet job instance; optionally preserves an existing id for restore.
    /// </summary>
    public JobSheet(
        string groupId,
        ISheet worksheet,
        string outputPath,
        JobTextConfig[] textConfigs,
        JobImageConfig[] imageConfigs,
        string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        GroupId = groupId;
        Worksheet = worksheet;
        OutputPath = outputPath;
        TextConfigs = textConfigs;
        ImageConfigs = imageConfigs;
    }

    /// <summary>
    ///     Gets the worksheet backing this job.
    /// </summary>
    public ISheet Worksheet { get; }

    /// <summary>
    ///     Gets the configured text replacements for this sheet.
    /// </summary>
    public JobTextConfig[] TextConfigs { get; }

    /// <summary>
    ///     Gets the configured image replacements for this sheet.
    /// </summary>
    public JobImageConfig[] ImageConfigs { get; }

    /// <summary>
    ///     Gets the row index (1-based) that should be processed next.
    /// </summary>
    public int NextRowIndex => CurrentRow + 1;

    /// <summary>
    ///     Gets the cancellation token source for this job.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <summary>
    ///     Gets a value indicating whether this job is currently executing.
    /// </summary>
    public bool IsExecuting { get; private set; }

    /// <summary>
    ///     Gets the Hangfire job id associated with this sheet execution.
    /// </summary>
    public string? HangfireJobId { get; set; }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string GroupId { get; }

    /// <inheritdoc />
    public string SheetName => Worksheet.Name;

    /// <inheritdoc />
    public string OutputPath { get; }

    /// <inheritdoc />
    public SheetJobStatus Status { get; private set; } = SheetJobStatus.Pending;

    /// <inheritdoc />
    public string? ErrorMessage { get; private set; }

    /// <inheritdoc />
    public int CurrentRow { get; private set; }

    /// <inheritdoc />
    public int TotalRows => Worksheet.RowCount;

    /// <inheritdoc />
    public float Progress => TotalRows == 0 ? 0 : (float)CurrentRow / TotalRows * 100.0f;

    /// <inheritdoc />
    public int ErrorCount { get; private set; }

    /// <summary>
    ///     Sets the job status and optional message.
    /// </summary>
    public void SetStatus(SheetJobStatus status, string? message = null)
    {
        Status = status;
        ErrorMessage = message;
    }

    /// <summary>
    ///     Updates the current row for progress tracking.
    /// </summary>
    public void UpdateProgress(int currentRow)
    {
        CurrentRow = Math.Clamp(currentRow, 0, TotalRows);
    }

    /// <summary>
    ///     Registers an error for a specific row.
    /// </summary>
    public void RegisterRowError(int rowIndex, string message)
    {
        ErrorCount++;
    }

    /// <summary>
    ///     Restores the error count from persisted state.
    /// </summary>
    public void RestoreErrorCount(int count)
    {
        ErrorCount = Math.Max(0, count);
    }

    /// <summary>
    ///     Marks the job as executing or idle.
    /// </summary>
    public void MarkExecuting(bool isExecuting)
    {
        IsExecuting = isExecuting;
    }

    /// <summary>
    ///     Requests the job to pause on the next checkpoint.
    /// </summary>
    public void Pause()
    {
        _pauseSignal.Pause();
        SetStatus(SheetJobStatus.Paused);
    }

    /// <summary>
    ///     Resumes the job from a paused state.
    /// </summary>
    public void Resume()
    {
        _pauseSignal.Resume();
    }

    /// <summary>
    ///     Waits if the job is currently paused.
    /// </summary>
    public Task WaitIfPausedAsync(CancellationToken cancellationToken)
    {
        return _pauseSignal.WaitIfPausedAsync(cancellationToken);
    }
}