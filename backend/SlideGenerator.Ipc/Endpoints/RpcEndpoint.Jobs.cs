using SlideGenerator.Ipc.Contracts.Requests;
using SlideGenerator.Domain.Tasks.Models;
using StreamJsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint
{
    [JsonRpcMethod("jobs.create")]
    public async Task<object> CreateJobAsync(GenerateRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentException("jobs.create params is invalid", nameof(request));

        var jobId = await _backendService.CreateJobAsync(request, cancellationToken);
        return new { jobId };
    }

    [JsonRpcMethod("jobs.get")]
    public async Task<JobSnapshot?> GetJobAsync(JobIdRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentException("params.jobId is required", nameof(request));

        return await _backendService.GetJobAsync(request.JobId, cancellationToken);
    }

    [JsonRpcMethod("jobs.list")]
    public async Task<IReadOnlyList<JobSnapshot>> ListJobsAsync(CancellationToken cancellationToken)
    {
        return await _backendService.ListJobsAsync(cancellationToken);
    }

    [JsonRpcMethod("jobs.pause")]
    public async Task<object> PauseJobAsync(JobIdRequest request, CancellationToken cancellationToken)
    {
        await ControlJobAsync(request, TaskControl.Pause, cancellationToken);
        return new { ok = true };
    }

    [JsonRpcMethod("jobs.resume")]
    public async Task<object> ResumeJobAsync(JobIdRequest request, CancellationToken cancellationToken)
    {
        await ControlJobAsync(request, TaskControl.Resume, cancellationToken);
        return new { ok = true };
    }

    [JsonRpcMethod("jobs.cancel")]
    public async Task<object> CancelJobAsync(JobIdRequest request, CancellationToken cancellationToken)
    {
        await ControlJobAsync(request, TaskControl.Cancel, cancellationToken);
        return new { ok = true };
    }

    private async Task ControlJobAsync(JobIdRequest request, TaskControl action,
        CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentException("params.jobId is required", nameof(request));

        await _backendService.ControlJobAsync(request.JobId, action, cancellationToken);
    }
}