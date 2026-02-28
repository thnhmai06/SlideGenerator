using SlideGenerator.Ipc.Contracts.Requests;
using SlideGenerator.Services.Scanning.Models.Slides;
using StreamJsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint
{
    [JsonRpcMethod("slide.scan")]
    public async Task<Presentation> ScanSlidesAsync(ScanFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FilePath))
            throw new ArgumentException("params.filePath is required", nameof(request));

        return await _backendService.ScanSlideAsync(request.FilePath, cancellationToken);
    }
}