using Hangfire;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Application.Sheet.Contracts;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Job.Models;

namespace SlideGenerator.Infrastructure.Job.Services;

/// <inheritdoc />
public class JobManager : Service, IJobManager
{
    private readonly ActiveJobCollection _active;
    private readonly CompletedJobCollection _completed;

    public JobManager(
        ILogger<JobManager> logger,
        ILoggerFactory loggerFactory,
        ISheetService sheetService,
        ISlideTemplateManager slideTemplateManager,
        IBackgroundJobClient backgroundJobClient) : base(logger)
    {
        _completed = new CompletedJobCollection(loggerFactory.CreateLogger<CompletedJobCollection>());

        _active = new ActiveJobCollection(
            loggerFactory.CreateLogger<ActiveJobCollection>(),
            sheetService,
            slideTemplateManager,
            backgroundJobClient,
            group => _completed.AddGroup(group));
    }

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