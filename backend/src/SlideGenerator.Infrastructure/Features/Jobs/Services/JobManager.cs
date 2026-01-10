using Hangfire;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Jobs.Contracts;
using SlideGenerator.Application.Features.Jobs.Contracts.Collections;
using SlideGenerator.Application.Features.Sheets;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Domain.Features.IO;
using SlideGenerator.Domain.Features.Jobs.Entities;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Jobs.States;
using SlideGenerator.Domain.Features.Sheets.Interfaces;
using SlideGenerator.Domain.Features.Slides;
using SlideGenerator.Domain.Features.Slides.Components;
using SlideGenerator.Infrastructure.Common.Base;
using SlideGenerator.Infrastructure.Features.Jobs.Models;

namespace SlideGenerator.Infrastructure.Features.Jobs.Services;

/// <inheritdoc cref="IJobManager" />
public class JobManager : Service, IJobManager
{
    private readonly ActiveJobCollection _active;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly CompletedJobCollection _completed;
    private readonly IJobStateStore _jobStateStore;
    private readonly ISheetService _sheetService;
    private readonly ISlideTemplateManager _slideTemplateManager;

    public JobManager(
        ILogger<JobManager> logger,
        ILoggerFactory loggerFactory,
        ISheetService sheetService,
        ISlideTemplateManager slideTemplateManager,
        IBackgroundJobClient backgroundJobClient,
        IJobStateStore jobStateStore,
        IJobNotifier jobNotifier,
        IFileSystem fileSystem) : base(logger)
    {
        _sheetService = sheetService;
        _slideTemplateManager = slideTemplateManager;
        _backgroundJobClient = backgroundJobClient;
        _jobStateStore = jobStateStore;

        _completed = new CompletedJobCollection(
            loggerFactory.CreateLogger<CompletedJobCollection>(),
            jobStateStore);

        _active = new ActiveJobCollection(
            loggerFactory.CreateLogger<ActiveJobCollection>(),
            sheetService,
            slideTemplateManager,
            backgroundJobClient,
            jobStateStore,
            fileSystem,
            jobNotifier,
            group => _completed.AddGroup(group));
    }

    #region Restore

    /// <summary>
    ///     Restores unfinished jobs from persisted state.
    /// </summary>
    public async Task RestoreAsync(CancellationToken cancellationToken)
    {
        var groupStates = await _jobStateStore.GetAllGroupsAsync(cancellationToken);
        if (groupStates.Count == 0)
            groupStates = [.. await _jobStateStore.GetActiveGroupsAsync(cancellationToken)];
        foreach (var groupState in groupStates)
        {
            var sheetStates = await _jobStateStore.GetSheetsByGroupAsync(groupState.Id, cancellationToken);
            if (sheetStates.Count == 0) continue;

            if (!IsActiveStatus(groupState.Status))
            {
                RestoreCompletedGroup(groupState, sheetStates);
                continue;
            }

            var workbook = _sheetService.OpenFile(groupState.WorkbookPath);
            _slideTemplateManager.AddTemplate(groupState.TemplatePath);
            var template = _slideTemplateManager.GetTemplate(groupState.TemplatePath);
            var outputFolder = new DirectoryInfo(groupState.OutputFolderPath);

            var textConfigs = sheetStates[0].TextConfigs;
            var imageConfigs = sheetStates[0].ImageConfigs;

            var group = new JobGroup(workbook, template, outputFolder, textConfigs, imageConfigs, groupState.CreatedAt,
                groupState.Id);

            // Force status to Paused if it was Running or Pending
            group.SetStatus(groupState.Status is GroupStatus.Running or GroupStatus.Pending
                ? GroupStatus.Paused
                : groupState.Status);

            foreach (var sheetState in sheetStates)
            {
                var sheet = group.AddJob(sheetState.SheetName, sheetState.OutputPath, sheetState.Id);
                sheet.UpdateProgress(Math.Max(0, sheetState.NextRowIndex - 1));
                sheet.RestoreErrorCount(sheetState.ErrorCount);

                // Force status to Paused if it was Running/Pending/Paused (ensure pause signal is set).
                if (sheetState.Status is SheetJobStatus.Running or SheetJobStatus.Pending or SheetJobStatus.Paused)
                    sheet.Pause();
                else
                    sheet.SetStatus(sheetState.Status, sheetState.ErrorMessage);
            }

            _active.RestoreGroup(group);
        }
    }

    #endregion

    #region Collections

    /// <inheritdoc />
    public IActiveJobCollection Active => _active;

    /// <inheritdoc />
    public ICompletedJobCollection Completed => _completed;

    #endregion

    #region Cross-Collection Query

    /// <inheritdoc />
    public IJobGroup? GetGroup(string groupId)
    {
        return _active.GetGroup(groupId) ?? _completed.GetGroup(groupId);
    }

    /// <inheritdoc />
    public IJobSheet? GetSheet(string sheetId)
    {
        return _active.GetSheet(sheetId) ?? _completed.GetSheet(sheetId);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _active.GetAllGroups())
            result[kv.Key] = kv.Value;
        foreach (var kv in _completed.GetAllGroups())
            result[kv.Key] = kv.Value;
        return result;
    }

    #endregion

    #region Internal Methods (for JobExecutor)

    internal JobSheet? GetInternalSheet(string sheetId)
    {
        return _active.GetInternalSheet(sheetId);
    }

    internal JobGroup? GetInternalGroup(string groupId)
    {
        return _active.GetInternalGroup(groupId);
    }

    internal void NotifySheetCompleted(string sheetId)
    {
        _active.NotifySheetCompleted(sheetId);
    }

    #endregion

    #region Restore Helpers

    private void RestoreCompletedGroup(GroupJobState groupState, IReadOnlyList<SheetJobState> sheetStates)
    {
        var workbook = new PersistedSheetBook(groupState.WorkbookPath, sheetStates);
        var template = new PersistedTemplatePresentation(groupState.TemplatePath);
        var outputFolder = new DirectoryInfo(groupState.OutputFolderPath);

        var textConfigs = sheetStates[0].TextConfigs;
        var imageConfigs = sheetStates[0].ImageConfigs;

        var group = new JobGroup(workbook, template, outputFolder, textConfigs, imageConfigs, groupState.CreatedAt,
            groupState.Id);
        group.SetStatus(groupState.Status);

        foreach (var sheetState in sheetStates)
        {
            var sheet = group.AddJob(sheetState.SheetName, sheetState.OutputPath, sheetState.Id);
            sheet.UpdateProgress(Math.Max(0, sheetState.NextRowIndex - 1));
            sheet.RestoreErrorCount(sheetState.ErrorCount);
            sheet.SetStatus(sheetState.Status, sheetState.ErrorMessage);
        }

        group.UpdateStatus();
        _completed.AddGroup(group);
    }

    private static bool IsActiveStatus(GroupStatus status)
    {
        return status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused;
    }

    private sealed class PersistedSheetBook : ISheetBook
    {
        public PersistedSheetBook(string filePath, IEnumerable<SheetJobState> sheetStates)
        {
            FilePath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath);

            Worksheets = sheetStates
                .GroupBy(state => state.SheetName)
                .ToDictionary(
                    group => group.Key,
                    group => (ISheet)new PersistedSheet(group.Key, group.First().TotalRows));
        }

        public string FilePath { get; }

        public string? Name { get; }

        public IReadOnlyDictionary<string, ISheet> Worksheets { get; }

        public IReadOnlyDictionary<string, int> GetSheetsInfo()
        {
            return Worksheets.ToDictionary(kv => kv.Key, kv => kv.Value.RowCount);
        }

        public void Dispose()
        {
        }
    }

    private sealed class PersistedSheet(string name, int rowCount) : ISheet
    {
        public string Name { get; } = name;

        public IReadOnlyList<string?> Headers { get; } = [];

        public int RowCount { get; } = rowCount;

        public Dictionary<string, string?> GetRow(int rowNumber)
        {
            return new Dictionary<string, string?>();
        }

        public List<Dictionary<string, string?>> GetAllRows()
        {
            return [];
        }
    }

    private sealed class PersistedTemplatePresentation(string filePath) : ITemplatePresentation
    {
        public string FilePath { get; } = filePath;

        public int SlideCount => 1;

        public Dictionary<uint, ImagePreview> GetAllImageShapes()
        {
            return new Dictionary<uint, ImagePreview>();
        }

        public IReadOnlyList<ShapeInfo> GetAllShapes()
        {
            return [];
        }

        public IReadOnlyCollection<string> GetAllTextPlaceholders()
        {
            return [];
        }

        public void Dispose()
        {
        }
    }

    #endregion
}