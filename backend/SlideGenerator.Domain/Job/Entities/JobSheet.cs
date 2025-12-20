using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Domain.Slide.Interfaces;

namespace SlideGenerator.Domain.Job.Entities;

/// <inheritdoc />
public class JobSheet(JobGroup jobGroup, ISheet worksheet, string outputPath) : IJobSheet
{
    private readonly Lock _lock = new();
    private readonly ManualResetEventSlim _pauseEvent = new(true); // true = not paused

    public string? JobId { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string GroupId => jobGroup.Id;
    public string OutputPath { get; } = outputPath;
    public SheetJobStatus Status { get; private set; } = SheetJobStatus.Pending;
    public int CurrentRow { get; private set; }
    public int TotalRows { get; } = worksheet.RowCount;
    public float Progress => TotalRows > 0 ? (float)CurrentRow / TotalRows * 100 : 0;
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public ISheet Worksheet { get; } = worksheet;
    public string SheetName => Worksheet.Name;
    public ITemplatePresentation Template => jobGroup.Template;
    public TextConfig[] TextConfigs => jobGroup.TextConfigs;
    public ImageConfig[] ImageConfigs => jobGroup.ImageConfigs;

    /// <summary>
    ///     Waits if the job is paused. Returns immediately if not paused.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    public void WaitIfPaused(CancellationToken cancellationToken)
    {
        _pauseEvent.Wait(cancellationToken);
    }

    public void SetStatus(SheetJobStatus status, string? errorMessage = null)
    {
        lock (_lock)
        {
            Status = status;
            ErrorMessage = errorMessage;

            switch (status)
            {
                case SheetJobStatus.Running when StartedAt == null:
                    StartedAt = DateTime.UtcNow;
                    _pauseEvent.Set(); // continue
                    break;
                case SheetJobStatus.Running:
                    _pauseEvent.Set(); // continue
                    break;
                case SheetJobStatus.Paused:
                    _pauseEvent.Reset(); // block
                    break;
                case SheetJobStatus.Completed or SheetJobStatus.Failed or SheetJobStatus.Cancelled:
                    CompletedAt = DateTime.UtcNow;
                    _pauseEvent.Set(); // continue
                    break;
            }
        }
    }

    public void UpdateProgress(int currentRow)
    {
        lock (_lock)
        {
            CurrentRow = currentRow;
        }
    }
}