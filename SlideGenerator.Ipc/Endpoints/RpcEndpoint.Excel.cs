using SlideGenerator.Ipc.Contracts.Requests;
using StreamJsonRpc;
using SlideGenerator.Services.Scanning.Models.Sheets;

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