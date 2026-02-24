using StreamJsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint
{
    [JsonRpcMethod("configs.get")]
    public object GetConfigs()
    {
        return _configManager.Current;
    }

    [JsonRpcMethod("configs.reload")]
    public object ReloadConfigs()
    {
        var ok = _configManager.Load();
        return new
        {
            ok,
            config = _configManager.Current
        };
    }

    [JsonRpcMethod("configs.save")]
    public object SaveConfigs()
    {
        var ok = _configManager.Save();
        return new { ok };
    }

    [JsonRpcMethod("configs.reset")]
    public object ResetConfigs()
    {
        var ok = _configManager.ResetToDefaults();
        return new
        {
            ok,
            config = _configManager.Current
        };
    }
}
