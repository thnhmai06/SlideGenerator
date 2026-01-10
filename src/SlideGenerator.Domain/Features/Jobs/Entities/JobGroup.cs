using System.Collections.Concurrent;
using SlideGenerator.Domain.Features.Jobs.Components;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Sheets.Interfaces;
using SlideGenerator.Domain.Features.Slides;

namespace SlideGenerator.Domain.Features.Jobs.Entities;

/// <summary>
///     Represents a group job composed of multiple sheet jobs.
/// </summary>
public sealed class JobGroup : IJobGroup
{
    private readonly ConcurrentDictionary<string, JobSheet> _jobs = new();

    /// <summary>
    ///     Creates a new group job instance; optionally preserves an existing id for restore.
    /// </summary>
    public JobGroup(
        ISheetBook workbook,
        ITemplatePresentation template,
        DirectoryInfo outputFolder,
        JobTextConfig[] textConfigs,
        JobImageConfig[] imageConfigs,
        DateTimeOffset? createdAt = null,
        string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Workbook = workbook;
        Template = template;
        OutputFolder = outputFolder;
        TextConfigs = textConfigs;
        ImageConfigs = imageConfigs;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Gets the creation timestamp for the group.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    ///     Gets the configured text replacements for the group.
    /// </summary>
    public JobTextConfig[] TextConfigs { get; }

    /// <summary>
    ///     Gets the configured image replacements for the group.
    /// </summary>
    public JobImageConfig[] ImageConfigs { get; }

    /// <summary>
    ///     Gets internal sheet jobs for management purposes.
    /// </summary>
    public IReadOnlyDictionary<string, JobSheet> InternalJobs => _jobs;

    /// <summary>
    ///     Indicates whether any sheet is still active.
    /// </summary>
    public bool IsActive => Status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public ISheetBook Workbook { get; }

    /// <inheritdoc />
    public ITemplatePresentation Template { get; }

    /// <inheritdoc />
    public DirectoryInfo OutputFolder { get; }

    /// <inheritdoc />
    public GroupStatus Status { get; private set; } = GroupStatus.Pending;

    /// <inheritdoc />
    public float Progress
    {
        get
        {
            if (_jobs.IsEmpty) return 0;

            long totalRows = 0;
            long completedRows = 0;
            foreach (var job in _jobs.Values)
            {
                var total = job.TotalRows;
                totalRows += total;
                completedRows += Math.Min(job.CurrentRow, total);
            }

            return totalRows == 0 ? 0 : (float)completedRows / totalRows * 100.0f;
        }
    }

    /// <inheritdoc />
    public int ErrorCount => _jobs.Values.Sum(j => j.ErrorCount);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobSheet> Sheets
    {
        get
        {
            var result = new Dictionary<string, IJobSheet>(_jobs.Count);
            foreach (var kv in _jobs)
                result.Add(kv.Key, kv.Value);
            return result;
        }
    }

    /// <inheritdoc />
    public int SheetCount => _jobs.Count;

    /// <summary>
    ///     Adds a new sheet job for the specified worksheet name.
    /// </summary>
    public JobSheet AddJob(string sheetName, string outputPath, string? sheetId = null)
    {
        if (!Workbook.Worksheets.TryGetValue(sheetName, out var worksheet))
            throw new InvalidOperationException($"Sheet '{sheetName}' not found in workbook.");

        var job = new JobSheet(Id, worksheet, outputPath, TextConfigs, ImageConfigs, sheetId);
        _jobs[job.Id] = job;
        return job;
    }

    /// <summary>
    ///     Removes a sheet job by id.
    /// </summary>
    public bool RemoveJob(string sheetId)
    {
        return _jobs.TryRemove(sheetId, out _);
    }

    /// <summary>
    ///     Sets the status of the group.
    /// </summary>
    public void SetStatus(GroupStatus status)
    {
        Status = status;
    }

    /// <summary>
    ///     Updates the group status based on its sheets.
    /// </summary>
    public void UpdateStatus()
    {
        var jobs = _jobs.Values;
        if (jobs.Count == 0)
        {
            Status = GroupStatus.Pending;
            return;
        }

        var hasActive = jobs.Any(j =>
            j.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused);
        if (!hasActive)
        {
            if (jobs.Any(j => j.Status == SheetJobStatus.Failed))
            {
                Status = GroupStatus.Failed;
                return;
            }

            Status = jobs.Any(j => j.Status == SheetJobStatus.Cancelled)
                ? GroupStatus.Cancelled
                : GroupStatus.Completed;
            return;
        }

        if (jobs.Any(j => j.Status == SheetJobStatus.Running))
        {
            Status = GroupStatus.Running;
            return;
        }

        if (jobs.Any(j => j.Status == SheetJobStatus.Paused))
        {
            Status = GroupStatus.Paused;
            return;
        }

        Status = GroupStatus.Pending;
    }
}
