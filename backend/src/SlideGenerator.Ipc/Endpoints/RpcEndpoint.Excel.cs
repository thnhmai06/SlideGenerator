using SlideGenerator.Ipc.Contracts.Requests;
using SlideGenerator.Scanning.Models.Sheets;
using StreamJsonRpc;

namespace SlideGenerator.Ipc.Endpoints;

public sealed partial class RpcEndpoint
{
    [JsonRpcMethod("sheet.scan")]
    public async Task<Workbook> ScanSheetAsync(ScanFileRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FilePath))
            throw new ArgumentException("params.filePath is required", nameof(request));

        return await _backendService.ScanSheetAsync(request.FilePath, cancellationToken);
    }
}