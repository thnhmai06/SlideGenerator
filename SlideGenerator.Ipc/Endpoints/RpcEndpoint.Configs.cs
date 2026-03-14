using StreamJsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint
{
    [JsonRpcMethod("configs.get")]
    public object GetConfigs()
    {
        return _settingManager.Current;
    }

    [JsonRpcMethod("configs.reload")]
    public object ReloadConfigs()
    {
        var ok = _settingManager.Load();
        return new
        {
            ok,
            config = _settingManager.Current
        };
    }

    [JsonRpcMethod("configs.save")]
    public object SaveConfigs()
    {
        var ok = _settingManager.Save();
        return new { ok };
    }

    [JsonRpcMethod("configs.reset")]
    public object ResetConfigs()
    {
        var ok = _settingManager.ResetToDefaults();
        return new
        {
            ok,
            config = _settingManager.Current
        };
    }
}