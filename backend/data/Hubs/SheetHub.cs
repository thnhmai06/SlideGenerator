using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TaoSlideTotNghiep.DTOs;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Models;
using TaoSlideTotNghiep.Services;

namespace TaoSlideTotNghiep.Hubs;

/// <summary>
/// SignalR Hub for spreadsheet operations.
/// </summary>
public class SheetHub(ISheetService sheetService, ILogger<SheetHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Workbook>> WorkbooksOfConnections = new();
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[Sheet] Client connected: {ConnectionId}", Context.ConnectionId);
        WorkbooksOfConnections[Context.ConnectionId] = new ConcurrentDictionary<string, Workbook>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("[Sheet] Client disconnected: {ConnectionId}", Context.ConnectionId);

        // Cleanup open workbooks for this connection
        if (WorkbooksOfConnections.TryRemove(Context.ConnectionId, out var workbooks))
        {
            foreach (var key in workbooks.Keys)
            {
                if (workbooks.TryRemove(key, out var wb))
                    wb.Dispose();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private ConcurrentDictionary<string, Workbook> Workbooks
        => WorkbooksOfConnections.GetValueOrDefault(Context.ConnectionId)
           ?? throw new ConnectionNotFoundException(Context.ConnectionId);

    /// <summary>
    /// Processes a sheet request based on type.
    /// </summary>
    public async Task ProcessRequest(JsonElement message)
    {
        BaseResponse response;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(typeStr))
                throw new TypeNotIncludedException(typeof(SheetRequestType));

            response = typeStr switch
            {
                "openfile" => ExecuteOpenFile(
                    JsonSerializer.Deserialize<OpenFileSheetRequest>(message.GetRawText(), _serializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(OpenFileSheetRequest))),
                "closefile" => ExecuteCloseFile(
                    JsonSerializer.Deserialize<CloseFileSheetRequest>(message.GetRawText(), _serializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(CloseFileSheetRequest))),
                "gettables" => ExecuteGetTables(
                    JsonSerializer.Deserialize<GetTablesSheetRequest>(message.GetRawText(), _serializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(GetTablesSheetRequest))),
                "getheaders" => ExecuteGetHeaders(
                    JsonSerializer.Deserialize<GetTableHeadersSheetRequest>(message.GetRawText(), _serializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(GetTableHeadersSheetRequest))),
                "getrow" => ExecuteGetRow(
                    JsonSerializer.Deserialize<GetTableRowSheetRequest>(message.GetRawText(), _serializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(GetTableRowSheetRequest))),
                _ => throw new TypeNotIncludedException(typeof(SheetRequestType))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing sheet request");
            response = new ErrorResponse(ex, RequestType.Sheet);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    private OpenFileSheetResponse ExecuteOpenFile(OpenFileSheetRequest request)
    {
        GetOrOpenWorkbook(request.SheetPath);
        return new OpenFileSheetResponse { SheetPath = request.SheetPath };
    }

    private CloseFileSheetResponse ExecuteCloseFile(CloseFileSheetRequest request)
    {
        if (Workbooks.TryRemove(request.SheetPath, out var wb))
        {
            wb.Dispose();
        }

        return new CloseFileSheetResponse { SheetPath = request.SheetPath };
    }

    private GetTablesSheetResponse ExecuteGetTables(GetTablesSheetRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.SheetPath);

        return new GetTablesSheetResponse
        {
            SheetPath = request.SheetPath,
            Tables = sheetService.GetTables(workbook)
        };
    }

    private GetTableHeadersSheetResponse ExecuteGetHeaders(GetTableHeadersSheetRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.SheetPath);

        return new GetTableHeadersSheetResponse
        {
            SheetPath = request.SheetPath,
            TableName = request.TableName,
            Headers = sheetService.GetHeaders(workbook, request.TableName).ToList()
        };
    }

    private GetTableRowSheetResponse ExecuteGetRow(GetTableRowSheetRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.SheetPath);

        return new GetTableRowSheetResponse
        {
            SheetPath = request.SheetPath,
            TableName = request.TableName,
            RowNumber = request.RowNumber,
            RowData = sheetService.GetRow(workbook, request.TableName, request.RowNumber)
        };
    }

    private Workbook GetOrOpenWorkbook(string sheetPath)
    {
        if (!Workbooks.TryGetValue(sheetPath, out var workbook))
        {
            workbook = sheetService.OpenFile(sheetPath);
            Workbooks[sheetPath] = workbook;
        }
        return workbook;
    }
}
