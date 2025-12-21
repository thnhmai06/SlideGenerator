using Hangfire;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Application.Sheet;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.IO;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Job.Models;

namespace SlideGenerator.Infrastructure.Job.Services;

/// <inheritdoc />
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
            group => _completed.AddGroup(group));
    }

    #region Restore

    /// <summary>
    ///     Restores unfinished jobs from persisted state.
    /// </summary>
    public async Task RestoreAsync(CancellationToken cancellationToken)
    {
        var groupStates = await _jobStateStore.GetActiveGroupsAsync(cancellationToken);
        foreach (var groupState in groupStates)
        {
            var sheetStates = await _jobStateStore.GetSheetsByGroupAsync(groupState.Id, cancellationToken);
            if (sheetStates.Count == 0) continue;

            var workbook = _sheetService.OpenFile(groupState.WorkbookPath);
            _slideTemplateManager.AddTemplate(groupState.TemplatePath);
            var template = _slideTemplateManager.GetTemplate(groupState.TemplatePath);
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

                if (sheet.Status == SheetJobStatus.Running)
                    sheet.SetStatus(SheetJobStatus.Pending);
            }

            group.UpdateStatus();
            _active.RestoreGroup(group);

            foreach (var sheet in group.InternalJobs.Values.Where(s =>
                         s.Status == SheetJobStatus.Pending))
            {
                var hangfireJobId = _backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                    executor.ExecuteJobAsync(sheet.Id, CancellationToken.None));
                sheet.HangfireJobId = hangfireJobId;
            }
        }
    }

    #endregion

    #region Collections

    public IActiveJobCollection Active => _active;
    public ICompletedJobCollection Completed => _completed;

    #endregion

    #region Cross-Collection Query

    public IJobGroup? GetGroup(string groupId)
    {
        return _active.GetGroup(groupId) ?? _completed.GetGroup(groupId);
    }

    public IJobSheet? GetSheet(string sheetId)
    {
        return _active.GetSheet(sheetId) ?? _completed.GetSheet(sheetId);
    }

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
}