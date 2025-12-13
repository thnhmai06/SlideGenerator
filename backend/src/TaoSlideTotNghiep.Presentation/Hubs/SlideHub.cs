using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TaoSlideTotNghiep.Application.Base.DTOs.Responses;
using TaoSlideTotNghiep.Application.Job.Contracts;
using TaoSlideTotNghiep.Application.Slide.Contracts;
using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;
using TaoSlideTotNghiep.Application.Slide.DTOs.Requests;
using TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Global;
using TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Group;
using TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Job;
using TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Errors;
using TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes;
using TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Global;
using TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Group;
using TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Job;
using TaoSlideTotNghiep.Domain.Sheet.Enums;
using TaoSlideTotNghiep.Presentation.Exceptions.Hubs;

namespace TaoSlideTotNghiep.Presentation.Hubs;

public class SlideHub(
    IJobManager jobManager,
    ISlideTemplateService slideTemplateService,
    ILogger<SlideHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[Presentation] Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("[Presentation] Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task ProcessRequest(JsonElement message)
    {
        Response response;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            response = typeStr switch
            {
                "scanshapes" => ExecuteScanShapes(
                    Deserialize<SlideScanShapes>(message)),
                "groupcreate" => ExecuteGroupCreate(
                    Deserialize<GenerateSlideGroupCreate>(message)),
                "groupstatus" => ExecuteGroupStatus(
                    Deserialize<GenerateSlideGroupStatus>(message)),
                "groupcontrol" => ExecuteGroupControl(
                    Deserialize<GenerateSlideGroupControlRequest>(message)),
                "jobstatus" => ExecuteJobStatus(
                    Deserialize<SlideJobStatus>(message)),
                "jobcontrol" => ExecuteJobControl(
                    Deserialize<GenerateSlideJobControlRequest>(message)),
                "globalcontrol" => ExecuteGlobalControl(
                    Deserialize<SlideGlobalControl>(message)),
                "getallgroups" => ExecuteGetAllGroups(),
                _ => throw new ArgumentOutOfRangeException(nameof(typeStr), typeStr, "Unknown request type")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing presentation request");
            response = new Error(ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    private T Deserialize<T>(JsonElement message)
    {
        return JsonSerializer.Deserialize<T>(message.GetRawText(), SerializerOptions)
               ?? throw new InvalidRequestFormatException(typeof(T).Name);
    }

    private SlideScanShapesSuccess ExecuteScanShapes(SlideScanShapes request)
    {
        slideTemplateService.AddTemplate(request.FilePath);
        var template = slideTemplateService.GetTemplate(request.FilePath);

        var shapes = template.GetAllImageShapes()
            .Select(kv => new ShapeDto(
                kv.Key,
                kv.Value.Name,
                Convert.ToBase64String(kv.Value.ImageBytes)))
            .ToArray();

        return new SlideScanShapesSuccess(request.FilePath, shapes);
    }

    private SlideGroupCreateSuccess ExecuteGroupCreate(GenerateSlideGroupCreate request)
    {
        var group = jobManager.CreateGroup(request);
        jobManager.StartGroup(group.Id);

        var jobIds = group.Jobs.ToDictionary(
            kv => kv.Value.SheetName,
            kv => kv.Key);

        return new SlideGroupCreateSuccess(group.Id, group.OutputFolder, jobIds);
    }

    private SlideGroupStatusSuccess ExecuteGroupStatus(GenerateSlideGroupStatus request)
    {
        var group = jobManager.GetGroup(request.GroupId)
                    ?? throw new InvalidOperationException($"Group {request.GroupId} not found");

        var jobs = group.Jobs.ToDictionary(
            kv => kv.Key,
            kv => new JobStatusInfo(
                kv.Key,
                kv.Value.SheetName,
                kv.Value.Status,
                kv.Value.CurrentRow,
                kv.Value.TotalRows,
                kv.Value.Progress,
                kv.Value.ErrorMessage));

        return new SlideGroupStatusSuccess(group.Id, group.Status, group.Progress, jobs);
    }

    private SlideGroupControlSuccess ExecuteGroupControl(GenerateSlideGroupControlRequest request)
    {
        switch (request.Action)
        {
            case ControlAction.Pause:
                jobManager.PauseGroup(request.GroupId);
                break;
            case ControlAction.Resume:
                jobManager.ResumeGroup(request.GroupId);
                break;
            case ControlAction.Cancel:
                jobManager.CancelGroup(request.GroupId);
                break;
        }

        return new SlideGroupControlSuccess(request.GroupId, request.Action);
    }

    private SlideJobStatusSuccess ExecuteJobStatus(SlideJobStatus request)
    {
        var job = jobManager.GetJob(request.JobId)
                  ?? throw new InvalidOperationException($"Job {request.JobId} not found");

        return new SlideJobStatusSuccess(
            job.Id,
            job.SheetName,
            job.Status,
            job.CurrentRow,
            job.TotalRows,
            job.Progress,
            job.ErrorMessage);
    }

    private SlideJobControlSuccess ExecuteJobControl(GenerateSlideJobControlRequest request)
    {
        switch (request.Action)
        {
            case ControlAction.Pause:
                jobManager.PauseJob(request.JobId);
                break;
            case ControlAction.Resume:
                jobManager.ResumeJob(request.JobId);
                break;
            case ControlAction.Cancel:
                jobManager.CancelJob(request.JobId);
                break;
        }

        return new SlideJobControlSuccess(request.JobId, request.Action);
    }

    private SlideGlobalControlSuccess ExecuteGlobalControl(SlideGlobalControl request)
    {
        var groups = jobManager.GetAllGroups();
        var affectedGroups = 0;
        var affectedJobs = 0;

        foreach (var group in groups.Values)
        {
            var isActive = group.Status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused;
            if (!isActive) continue;

            affectedGroups++;
            affectedJobs += group.Jobs.Count(j =>
                j.Value.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused);
        }

        switch (request.Action)
        {
            case ControlAction.Pause:
                jobManager.PauseAll();
                break;
            case ControlAction.Resume:
                jobManager.ResumeAll();
                break;
            case ControlAction.Cancel:
                jobManager.CancelAll();
                break;
        }

        return new SlideGlobalControlSuccess(request.Action, affectedGroups, affectedJobs);
    }

    private SlideGlobalGetGroupsSuccess ExecuteGetAllGroups()
    {
        var groups = jobManager.GetAllGroups()
            .Select(kv => new GroupSummary(
                kv.Key,
                kv.Value.Workbook.FilePath,
                kv.Value.Status,
                kv.Value.Progress,
                kv.Value.Jobs.Count,
                kv.Value.Jobs.Count(j => j.Value.Status == SheetJobStatus.Completed)))
            .ToList();

        return new SlideGlobalGetGroupsSuccess(groups);
    }
}