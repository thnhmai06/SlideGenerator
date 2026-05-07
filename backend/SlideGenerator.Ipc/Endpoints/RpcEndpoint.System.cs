using StreamJsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint
{
    [JsonRpcMethod("system.health")]
    public static object Health()
    {
        return new { ok = true };
    }
}