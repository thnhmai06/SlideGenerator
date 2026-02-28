using SlideGenerator.Features.Configs.Services;
using SlideGenerator.Features.Jobs.Entities.Jobs;
using SlideGenerator.Services;
using JsonRpcConnection = StreamJsonRpc.JsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint : IDisposable
{
    private readonly BackendService _backendService;
    private readonly ConfigManager _configManager;
    private JsonRpcConnection? _rpc;

    public RpcEndpoint(BackendService backendService, ConfigManager configManager)
    {
        _backendService = backendService;
        _configManager = configManager;
        _backendService.JobUpdated += HandleJobUpdated;
    }

    public void Dispose()
    {
        _backendService.JobUpdated -= HandleJobUpdated;
    }

    internal void Attach(JsonRpcConnection rpc)
    {
        _rpc = rpc;
    }

    private void HandleJobUpdated(JobSnapshotEntity snapshot)
    {
        var rpc = _rpc;
        if (rpc == null) return;

        _ = rpc.NotifyAsync("jobs.updated", snapshot)
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                    Console.Error.WriteLine(task.Exception);
            }, TaskScheduler.Default);
    }
}