using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Job;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Slide;
using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;
using SlideGenerator.Application.Slide.DTOs.Requests;
using SlideGenerator.Application.Slide.DTOs.Requests.Global;
using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Application.Slide.DTOs.Requests.Job;
using SlideGenerator.Application.Slide.DTOs.Responses.Errors;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Presentation.Exceptions.Hubs;

namespace SlideGenerator.Presentation.Hubs;

public class SlideHub(
    IJobManager jobManager,
    ISlideTemplateManager slideTemplateManager,
    ILogger<SlideHub> logger) : Hub
{
    public Task SubscribeGroup(string groupJobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, JobSignalRGroups.GroupGroup(groupJobId));
    }

    public Task SubscribeSheet(string sheetJobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, JobSignalRGroups.SheetGroup(sheetJobId));
    }

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
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
                "scanplaceholders" => ExecuteScanPlaceholders(
                    Deserialize<SlideScanPlaceholders>(message)),
                "scantemplate" => ExecuteScanTemplate(
                    Deserialize<SlideScanTemplate>(message)),
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
               ?? throw new InvalidRequestFormat(typeof(T).Name);
    }

    private SlideScanShapesSuccess ExecuteScanShapes(SlideScanShapes request)
    {
        var added = slideTemplateManager.AddTemplate(request.FilePath);
        try
        {
            var template = slideTemplateManager.GetTemplate(request.FilePath);

            var imageShapes = template.GetAllImageShapes();
            var shapes = template.GetAllShapes()
                .Select(shape =>
                {
                    var data = imageShapes.TryGetValue(shape.Id, out var preview)
                        ? Convert.ToBase64String(preview.Image)
                        : string.Empty;
                    return new ShapeDto(shape.Id, shape.Name, data, shape.Kind, shape.IsImage);
                })
                .ToArray();

            return new SlideScanShapesSuccess(request.FilePath, shapes);
        }
        finally
        {
            if (added)
            {
                slideTemplateManager.RemoveTemplate(request.FilePath);
            }
        }
    }

    private SlideScanPlaceholdersSuccess ExecuteScanPlaceholders(SlideScanPlaceholders request)
    {
        var added = slideTemplateManager.AddTemplate(request.FilePath);
        try
        {
            var template = slideTemplateManager.GetTemplate(request.FilePath);

            var placeholders = template.GetAllTextPlaceholders().ToArray();
            return new SlideScanPlaceholdersSuccess(request.FilePath, placeholders);
        }
        finally
        {
            if (added)
            {
                slideTemplateManager.RemoveTemplate(request.FilePath);
            }
        }
    }

    private SlideScanTemplateSuccess ExecuteScanTemplate(SlideScanTemplate request)
    {
        var added = slideTemplateManager.AddTemplate(request.FilePath);
        try
        {
            var template = slideTemplateManager.GetTemplate(request.FilePath);

            var imageShapes = template.GetAllImageShapes();
            var shapes = template.GetAllShapes()
                .Select(shape =>
                {
                    var data = imageShapes.TryGetValue(shape.Id, out var preview)
                        ? Convert.ToBase64String(preview.Image)
                        : string.Empty;
                    return new ShapeDto(shape.Id, shape.Name, data, shape.Kind, shape.IsImage);
                })
                .ToArray();

            var placeholders = template.GetAllTextPlaceholders().ToArray();
            return new SlideScanTemplateSuccess(request.FilePath, shapes, placeholders);
        }
        finally
        {
            if (added)
            {
                slideTemplateManager.RemoveTemplate(request.FilePath);
            }
        }
    }

    private SlideGroupCreateSuccess ExecuteGroupCreate(GenerateSlideGroupCreate request)
    {
        var group = jobManager.Active.CreateGroup(request);
        jobManager.Active.StartGroup(group.Id);

        var jobIds = group.Sheets.ToDictionary(
            kv => kv.Value.SheetName,
            kv => kv.Key);

        return new SlideGroupCreateSuccess(group.Id, group.OutputFolder.FullName, jobIds);
    }

    private SlideGroupStatusSuccess ExecuteGroupStatus(GenerateSlideGroupStatus request)
    {
        var group = ResolveGroup(request.GroupId, request.GetOutputPath())
                    ?? throw new InvalidOperationException("Group not found");

        var jobs = group.Sheets.ToDictionary(
            kv => kv.Key,
            kv => new JobStatusInfo(
                kv.Key,
                kv.Value.SheetName,
                kv.Value.Status,
                kv.Value.CurrentRow,
                kv.Value.TotalRows,
                kv.Value.Progress,
                kv.Value.ErrorMessage,
                kv.Value.ErrorCount));

        return new SlideGroupStatusSuccess(group.Id, group.Status, group.Progress, jobs, group.ErrorCount);
    }

    private SlideGroupControlSuccess ExecuteGroupControl(GenerateSlideGroupControlRequest request)
    {
        var group = ResolveGroup(request.GroupId, request.GetOutputPath())
                    ?? throw new InvalidOperationException("Group not found");

        var action = request.GetAction();
        switch (action)
        {
            case ControlAction.Pause:
                jobManager.Active.PauseGroup(group.Id);
                break;
            case ControlAction.Resume:
                jobManager.Active.ResumeGroup(group.Id);
                break;
            case ControlAction.Cancel:
            case ControlAction.Stop:
                jobManager.Active.CancelGroup(group.Id);
                break;
        }

        return new SlideGroupControlSuccess(group.Id, action);
    }

    private SlideJobStatusSuccess ExecuteJobStatus(SlideJobStatus request)
    {
        var job = jobManager.GetSheet(request.JobId)
                  ?? throw new InvalidOperationException($"Job {request.JobId} not found");

        return new SlideJobStatusSuccess(
            job.Id,
            job.SheetName,
            job.Status,
            job.CurrentRow,
            job.TotalRows,
            job.Progress,
            job.ErrorMessage,
            job.ErrorCount);
    }

    private SlideJobControlSuccess ExecuteJobControl(GenerateSlideJobControlRequest request)
    {
        var action = request.GetAction();
        switch (action)
        {
            case ControlAction.Pause:
                jobManager.Active.PauseSheet(request.JobId);
                break;
            case ControlAction.Resume:
                jobManager.Active.ResumeSheet(request.JobId);
                break;
            case ControlAction.Cancel:
            case ControlAction.Stop:
                jobManager.Active.CancelSheet(request.JobId);
                break;
        }

        return new SlideJobControlSuccess(request.JobId, action);
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
            affectedJobs += group.Sheets.Count(j =>
                j.Value.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused);
        }

        var action = request.GetAction();
        switch (action)
        {
            case ControlAction.Pause:
                jobManager.Active.PauseAll();
                break;
            case ControlAction.Resume:
                jobManager.Active.ResumeAll();
                break;
            case ControlAction.Cancel:
            case ControlAction.Stop:
                jobManager.Active.CancelAll();
                break;
        }

        return new SlideGlobalControlSuccess(action, affectedGroups, affectedJobs);
    }

    private SlideGlobalGetGroupsSuccess ExecuteGetAllGroups()
    {
        var groups = jobManager.GetAllGroups()
            .Select(kv => new GroupSummary(
                kv.Key,
                kv.Value.Workbook.FilePath,
                kv.Value.Status,
                kv.Value.Progress,
                kv.Value.SheetCount,
                kv.Value.Sheets.Count(j => j.Value.Status == SheetJobStatus.Completed),
                kv.Value.ErrorCount))
            .ToList();

        return new SlideGlobalGetGroupsSuccess(groups);
    }

    private IJobGroup? ResolveGroup(string? groupId, string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(groupId))
            return jobManager.GetGroup(groupId);

        if (string.IsNullOrWhiteSpace(outputPath))
            return null;

        var normalizedPath = NormalizeOutputFolderPath(outputPath);
        return jobManager.GetAllGroups().Values.FirstOrDefault(group =>
            string.Equals(group.OutputFolder.FullName, normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeOutputFolderPath(string outputPath)
    {
        var fullPath = Path.GetFullPath(outputPath);
        if (Path.HasExtension(fullPath) &&
            string.Equals(Path.GetExtension(fullPath), ".pptx", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                return directory;
        }

        return fullPath;
    }
}
