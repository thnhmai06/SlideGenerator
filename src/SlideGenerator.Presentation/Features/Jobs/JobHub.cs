using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Jobs;
using SlideGenerator.Application.Features.Jobs.Contracts;
using SlideGenerator.Application.Features.Jobs.DTOs.Requests;
using SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Application.Features.Slides.DTOs.Components;
using SlideGenerator.Application.Features.Slides.DTOs.Enums;
using SlideGenerator.Application.Features.Slides.DTOs.Requests;
using SlideGenerator.Application.Features.Slides.DTOs.Responses.Errors;
using SlideGenerator.Application.Features.Slides.DTOs.Responses.Successes;
using SlideGenerator.Domain.Features.Jobs.Components;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Jobs.States;
using HubBase = SlideGenerator.Presentation.Common.Hubs.Hub;

namespace SlideGenerator.Presentation.Features.Jobs;

/// <summary>
///     SignalR hub for job creation, control, and query.
/// </summary>
public class JobHub(
    IJobManager jobManager,
    ISlideTemplateManager slideTemplateManager,
    IJobStateStore jobStateStore,
    ILogger<JobHub> logger) : HubBase
{
    private static readonly JsonSerializerOptions JobExportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task SubscribeGroup(string groupJobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, JobSignalRGroups.GroupGroup(groupJobId));
    }

    public Task SubscribeSheet(string sheetJobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, JobSignalRGroups.SheetGroup(sheetJobId));
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
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
                "taskcreate" or "jobcreate" => ExecuteJobCreate(
                    Deserialize<JobCreate>(message)),
                "taskcontrol" or "jobcontrol" => ExecuteJobControl(
                    Deserialize<JobControl>(message)),
                "taskquery" or "jobquery" => ExecuteJobQuery(
                    Deserialize<JobQuery>(message)),
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
            if (added) slideTemplateManager.RemoveTemplate(request.FilePath);
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
            if (added) slideTemplateManager.RemoveTemplate(request.FilePath);
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
            if (added) slideTemplateManager.RemoveTemplate(request.FilePath);
        }
    }

    private JobCreateSuccess ExecuteJobCreate(JobCreate request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplatePath))
            throw new InvalidOperationException("TemplatePath is required.");
        if (string.IsNullOrWhiteSpace(request.SpreadsheetPath))
            throw new InvalidOperationException("SpreadsheetPath is required.");
        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new InvalidOperationException("OutputPath is required.");

        if (request.JobType == JobType.Sheet && string.IsNullOrWhiteSpace(request.SheetName))
            throw new InvalidOperationException("SheetName is required for sheet jobs.");

        logger.LogInformation(
            "Creating job: Type={JobType}, Template={TemplatePath}, Spreadsheet={SpreadsheetPath}, AutoStart={AutoStart}",
            request.JobType, request.TemplatePath, request.SpreadsheetPath, request.AutoStart);

        var group = jobManager.Active.CreateGroup(request);
        if (request.AutoStart)
            jobManager.Active.StartGroup(group.Id);

        logger.LogInformation("Job group created: {GroupId} with {SheetCount} sheets",
            group.Id, group.Sheets.Count);

        if (request.JobType == JobType.Sheet)
        {
            var sheet = group.Sheets.Values.First(s =>
                string.Equals(s.SheetName, request.SheetName, StringComparison.OrdinalIgnoreCase));

            return new JobCreateSuccess(BuildJobSummary(sheet), null);
        }

        var jobIds = new Dictionary<string, string>(group.Sheets.Count);
        foreach (var kv in group.Sheets)
            jobIds[kv.Value.SheetName] = kv.Key;

        return new JobCreateSuccess(BuildJobSummary(group), jobIds);
    }

    private JobQuerySuccess ExecuteJobQuery(JobQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.JobId))
        {
            var (jobType, group, sheet) = ResolveJob(request.JobId, request.JobType);
            var payload = request.IncludePayload
                ? jobType == JobType.Group
                    ? GetGroupPayload(request.JobId)
                    : GetSheetPayload(request.JobId)
                : null;

            var detail = jobType == JobType.Group
                ? BuildJobDetail(group!, request.IncludeSheets, payload)
                : BuildJobDetail(sheet!, payload);

            return new JobQuerySuccess(detail, null);
        }

        var includeGroups = request.JobType != JobType.Sheet;
        var includeSheets = request.JobType != JobType.Group;

        var jobs = new List<JobSummary>();
        if (request.Scope is JobQueryScope.Active or JobQueryScope.All)
        {
            if (includeGroups)
                jobs.AddRange(jobManager.Active.EnumerateGroups().Select(BuildJobSummary));
            if (includeSheets)
                jobs.AddRange(jobManager.Active.EnumerateSheets().Select(BuildJobSummary));
        }

        if (request.Scope is JobQueryScope.Completed or JobQueryScope.All)
        {
            if (includeGroups)
                jobs.AddRange(jobManager.Completed.EnumerateGroups().Select(BuildJobSummary));
            if (includeSheets)
                jobs.AddRange(jobManager.Completed.EnumerateSheets().Select(BuildJobSummary));
        }

        return new JobQuerySuccess(null, jobs);
    }

    private JobControlSuccess ExecuteJobControl(JobControl request)
    {
        var (jobType, group, sheet) = ResolveJob(request.JobId, request.JobType);
        var action = request.Action == ControlAction.Stop ? ControlAction.Cancel : request.Action;

        logger.LogInformation("Job control: {Action} on {JobType} {JobId}",
            action, jobType, request.JobId);

        switch (jobType)
        {
            case JobType.Group:
                switch (action)
                {
                    case ControlAction.Pause:
                        jobManager.Active.PauseGroup(group!.Id);
                        break;
                    case ControlAction.Resume:
                        jobManager.Active.ResumeGroup(group!.Id);
                        break;
                    case ControlAction.Cancel:
                        jobManager.Active.CancelGroup(group!.Id);
                        break;
                    case ControlAction.Remove:
                        if (jobManager.Active.ContainsGroup(group!.Id))
                            jobManager.Active.CancelAndRemoveGroup(group.Id);
                        else
                            jobManager.Completed.RemoveGroup(group.Id);
                        break;
                }

                break;
            case JobType.Sheet:
                switch (action)
                {
                    case ControlAction.Pause:
                        jobManager.Active.PauseSheet(sheet!.Id);
                        break;
                    case ControlAction.Resume:
                        jobManager.Active.ResumeSheet(sheet!.Id);
                        break;
                    case ControlAction.Cancel:
                        jobManager.Active.CancelSheet(sheet!.Id);
                        break;
                    case ControlAction.Remove:
                        if (jobManager.Active.ContainsSheet(sheet!.Id))
                            jobManager.Active.CancelAndRemoveSheet(sheet.Id);
                        else
                            jobManager.Completed.RemoveSheet(sheet.Id);
                        break;
                }

                break;
        }

        return new JobControlSuccess(request.JobId, jobType, action);
    }

    private static JobSummary BuildJobSummary(IJobGroup group)
    {
        return new JobSummary(
            group.Id,
            JobType.Group,
            group.Status.ToJobState(),
            group.Progress,
            null,
            null,
            group.OutputFolder.FullName,
            group.ErrorCount,
            null);
    }

    private static JobSummary BuildJobSummary(IJobSheet sheet)
    {
        return new JobSummary(
            sheet.Id,
            JobType.Sheet,
            sheet.Status.ToJobState(),
            sheet.Progress,
            sheet.GroupId,
            sheet.SheetName,
            sheet.OutputPath,
            sheet.ErrorCount,
            sheet.HangfireJobId);
    }

    private static JobDetail BuildJobDetail(IJobGroup group, bool includeSheets, string? payloadJson)
    {
        IReadOnlyDictionary<string, JobSummary>? sheets = null;
        if (includeSheets)
            sheets = group.Sheets.ToDictionary(
                kv => kv.Key,
                kv => BuildJobSummary(kv.Value));

        return new JobDetail(
            group.Id,
            JobType.Group,
            group.Status.ToJobState(),
            group.Progress,
            group.ErrorCount,
            null,
            null,
            null,
            null,
            null,
            null,
            group.OutputFolder.FullName,
            sheets,
            payloadJson,
            null);
    }

    private static JobDetail BuildJobDetail(IJobSheet sheet, string? payloadJson)
    {
        return new JobDetail(
            sheet.Id,
            JobType.Sheet,
            sheet.Status.ToJobState(),
            sheet.Progress,
            sheet.ErrorCount,
            sheet.ErrorMessage,
            sheet.GroupId,
            sheet.SheetName,
            sheet.CurrentRow,
            sheet.TotalRows,
            sheet.OutputPath,
            null,
            null,
            payloadJson,
            sheet.HangfireJobId);
    }

    private (JobType JobType, IJobGroup? Group, IJobSheet? Sheet) ResolveJob(string jobId, JobType? jobType)
    {
        if (jobType == JobType.Group)
        {
            var group = jobManager.GetGroup(jobId)
                        ?? throw new InvalidOperationException($"Group job {jobId} not found");
            return (JobType.Group, group, null);
        }

        if (jobType == JobType.Sheet)
        {
            var sheet = jobManager.GetSheet(jobId)
                        ?? throw new InvalidOperationException($"Sheet job {jobId} not found");
            return (JobType.Sheet, null, sheet);
        }

        var resolvedGroup = jobManager.GetGroup(jobId);
        if (resolvedGroup != null)
            return (JobType.Group, resolvedGroup, null);

        var resolvedSheet = jobManager.GetSheet(jobId);
        if (resolvedSheet != null)
            return (JobType.Sheet, null, resolvedSheet);

        throw new InvalidOperationException($"Job {jobId} not found");
    }

    private string? GetGroupPayload(string groupId)
    {
        var groupState = jobStateStore.GetGroupAsync(groupId, CancellationToken.None)
            .GetAwaiter().GetResult();
        if (groupState == null)
            return null;

        var sheets = jobStateStore.GetSheetsByGroupAsync(groupId, CancellationToken.None)
            .GetAwaiter().GetResult();
        return BuildGroupPayload(groupState, sheets);
    }

    private string? GetSheetPayload(string sheetId)
    {
        var sheetState = jobStateStore.GetSheetAsync(sheetId, CancellationToken.None)
            .GetAwaiter().GetResult();
        if (sheetState == null)
            return null;

        var groupState = jobStateStore.GetGroupAsync(sheetState.GroupId, CancellationToken.None)
            .GetAwaiter().GetResult();
        return BuildSheetPayload(sheetState, groupState);
    }

    private static string BuildGroupPayload(
        GroupJobState groupState,
        IReadOnlyList<SheetJobState> sheetStates)
    {
        var sheetNames = sheetStates.Select(s => s.SheetName).Distinct().ToArray();
        var firstSheet = sheetStates.FirstOrDefault();
        var textConfigs = firstSheet != null ? MapTextConfigs(firstSheet.TextConfigs) : null;
        var imageConfigs = firstSheet != null ? MapImageConfigs(firstSheet.ImageConfigs) : null;
        var payload = new JobExportPayload(
            JobType.Group,
            groupState.TemplatePath,
            groupState.WorkbookPath,
            groupState.OutputFolderPath,
            sheetNames,
            null,
            textConfigs,
            imageConfigs);

        return JsonSerializer.Serialize(payload, JobExportJsonOptions);
    }

    private static string BuildSheetPayload(
        SheetJobState sheetState,
        GroupJobState? groupState)
    {
        var payload = new JobExportPayload(
            JobType.Sheet,
            groupState?.TemplatePath ?? string.Empty,
            groupState?.WorkbookPath ?? string.Empty,
            sheetState.OutputPath,
            null,
            sheetState.SheetName,
            MapTextConfigs(sheetState.TextConfigs),
            MapImageConfigs(sheetState.ImageConfigs));

        return JsonSerializer.Serialize(payload, JobExportJsonOptions);
    }

    private static SlideTextConfig[]? MapTextConfigs(JobTextConfig[] configs)
    {
        if (configs.Length == 0) return null;
        return [.. configs.Select(c => new SlideTextConfig(c.Pattern, c.Columns))];
    }

    private static SlideImageConfig[]? MapImageConfigs(JobImageConfig[] configs)
    {
        if (configs.Length == 0) return null;
        return [.. configs.Select(c => new SlideImageConfig(c.ShapeId, c.Columns, c.RoiType, c.CropType))];
    }

    private sealed record JobExportPayload(
        JobType JobType,
        string TemplatePath,
        string SpreadsheetPath,
        string OutputPath,
        string[]? SheetNames,
        string? SheetName,
        SlideTextConfig[]? TextConfigs,
        SlideImageConfig[]? ImageConfigs);
}