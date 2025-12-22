using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Slide.DTOs.Notifications;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Notifications;
using SlideGenerator.Infrastructure.Base;

namespace SlideGenerator.Infrastructure.Job.Services;

/// <inheritdoc cref="IJobNotifier" />
public class JobNotifier<TTHub>(
    ILogger<JobNotifier<TTHub>> logger,
    IHubContext<TTHub> hubContext) : Service(logger), IJobNotifier
    where TTHub : Hub
{
    private const string ReceiveMethod = "ReceiveNotification";

    public async Task NotifyJobProgress(string jobId, int currentRow, int totalRows, float progress, int errorCount)
    {
        var notification = new JobProgressNotification(jobId, currentRow, totalRows, progress, errorCount,
            DateTimeOffset.UtcNow);
        await hubContext.Clients.Group(JobSignalRGroups.SheetGroup(jobId))
            .SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyJobStatusChanged(string jobId, SheetJobStatus status, string? message = null)
    {
        var notification = new JobStatusNotification(jobId, status, message, DateTimeOffset.UtcNow);
        await hubContext.Clients.Group(JobSignalRGroups.SheetGroup(jobId))
            .SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyJobError(string jobId, string error)
    {
        var notification = new JobErrorNotification(jobId, error, DateTimeOffset.UtcNow);
        await hubContext.Clients.Group(JobSignalRGroups.SheetGroup(jobId))
            .SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyGroupProgress(string groupId, float progress, int errorCount)
    {
        var notification = new GroupProgressNotification(groupId, progress, errorCount, DateTimeOffset.UtcNow);
        await hubContext.Clients.Group(JobSignalRGroups.GroupGroup(groupId))
            .SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyGroupStatusChanged(string groupId, GroupStatus status, string? message = null)
    {
        var notification = new GroupStatusNotification(groupId, status, message, DateTimeOffset.UtcNow);
        await hubContext.Clients.Group(JobSignalRGroups.GroupGroup(groupId))
            .SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyLog(JobEvent jobEvent)
    {
        var notification = new JobLogNotification(jobEvent.JobId, jobEvent.Level, jobEvent.Message,
            jobEvent.Timestamp, jobEvent.Data);
        var groupName = jobEvent.Scope switch
        {
            JobEventScope.Group => JobSignalRGroups.GroupGroup(jobEvent.JobId),
            JobEventScope.Sheet => JobSignalRGroups.SheetGroup(jobEvent.JobId),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(groupName))
            return;

        await hubContext.Clients.Group(groupName).SendAsync(ReceiveMethod, notification);
    }
}