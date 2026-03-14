using SlideGenerator.Application;
using SlideGenerator.Domain.Settings.Services;
using JsonRpcConnection = StreamJsonRpc.JsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint : IDisposable
{
    private readonly BackendService _backendService;
    private readonly SettingManager _settingManager;
    private JsonRpcConnection? _rpc;

    public RpcEndpoint(BackendService backendService, SettingManager settingManager)
    {
        _backendService = backendService;
        _settingManager = settingManager;
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

    private void HandleJobUpdated(JobSnapshot snapshot)
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