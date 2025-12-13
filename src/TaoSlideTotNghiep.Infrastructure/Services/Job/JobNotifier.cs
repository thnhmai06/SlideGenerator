using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Job.Contracts;
using TaoSlideTotNghiep.Application.Slide.DTOs.Notifications;
using TaoSlideTotNghiep.Domain.Sheet.Enums;
using TaoSlideTotNghiep.Infrastructure.Services.Base;

namespace TaoSlideTotNghiep.Infrastructure.Services.Job;

public class JobNotifier<TTHub>(
    ILogger<JobNotifier<TTHub>> logger,
    IHubContext<TTHub> hubContext) : Service(logger), IJobNotifier
    where TTHub : Hub
{
    private const string ReceiveMethod = "ReceiveNotification";

    public async Task NotifyJobProgress(string jobId, int currentRow, int totalRows, float progress)
    {
        var notification = new JobProgressNotification(jobId, currentRow, totalRows, progress);
        await hubContext.Clients.All.SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyJobStatusChanged(string jobId, SheetJobStatus status, string? message = null)
    {
        var notification = new JobStatusNotification(jobId, status, message);
        await hubContext.Clients.All.SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyJobError(string jobId, string error)
    {
        var notification = new JobErrorNotification(jobId, error);
        await hubContext.Clients.All.SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyGroupProgress(string groupId, float progress)
    {
        var notification = new GroupProgressNotification(groupId, progress);
        await hubContext.Clients.All.SendAsync(ReceiveMethod, notification);
    }

    public async Task NotifyGroupStatusChanged(string groupId, GroupStatus status, string? message = null)
    {
        var notification = new GroupStatusNotification(groupId, status, message);
        await hubContext.Clients.All.SendAsync(ReceiveMethod, notification);
    }
}