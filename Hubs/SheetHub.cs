using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TaoSlideTotNghiep.DTOs;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Logic;

namespace TaoSlideTotNghiep.Hubs;

/// <summary>
/// SignalR Hub for spreadsheet operations.
/// </summary>
public class SheetHub(ILogger<SheetHub> logger) : Hub
{
    // Thread-safe storage for open workbooks per connection
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Workbook>> ConnectionWorkbooks = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Sheet client connected: {ConnectionId}", Context.ConnectionId);
        ConnectionWorkbooks[Context.ConnectionId] = new ConcurrentDictionary<string, Workbook>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Sheet client disconnected: {ConnectionId}", Context.ConnectionId);
        
        // Cleanup open workbooks for this connection
        if (ConnectionWorkbooks.TryRemove(Context.ConnectionId, out var workbooks))
        {
            foreach (var key in workbooks.Keys)
            {
                if (workbooks.TryRemove(key, out var wb))
                    wb.Dispose();
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }

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

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            response = typeStr switch
            {
                "openfile" => ExecuteOpenFile(
                    JsonSerializer.Deserialize<OpenFileSheetRequest>(message.GetRawText(), options)!),
                "closefile" => ExecuteCloseFile(
                    JsonSerializer.Deserialize<CloseFileSheetRequest>(message.GetRawText(), options)!),
                "gettables" => ExecuteGetTables(
                    JsonSerializer.Deserialize<GetTablesSheetRequest>(message.GetRawText(), options)!),
                "getheaders" => ExecuteGetHeaders(
                    JsonSerializer.Deserialize<GetTableHeadersSheetRequest>(message.GetRawText(), options)!),
                "getrow" => ExecuteGetRow(
                    JsonSerializer.Deserialize<GetTableRowSheetRequest>(message.GetRawText(), options)!),
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

    private ConcurrentDictionary<string, Workbook> GetWorkbooks()
    {
        return ConnectionWorkbooks.GetValueOrDefault(Context.ConnectionId) 
               ?? throw new InvalidOperationException("Connection not found");
    }

    private OpenFileSheetResponse ExecuteOpenFile(OpenFileSheetRequest request)
    {
        var workbooks = GetWorkbooks();
        
        if (!workbooks.ContainsKey(request.SheetPath))
        {
            workbooks[request.SheetPath] = new Workbook(request.SheetPath);
        }

        return new OpenFileSheetResponse { SheetPath = request.SheetPath };
    }

    private CloseFileSheetResponse ExecuteCloseFile(CloseFileSheetRequest request)
    {
        var workbooks = GetWorkbooks();
        
        if (workbooks.TryRemove(request.SheetPath, out var wb))
        {
            wb.Dispose();
        }

        return new CloseFileSheetResponse { SheetPath = request.SheetPath };
    }

    private GetTablesSheetResponse ExecuteGetTables(GetTablesSheetRequest request)
    {
        var workbooks = GetWorkbooks();
        
        if (!workbooks.TryGetValue(request.SheetPath, out var group))
        {
            group = new Workbook(request.SheetPath);
            workbooks[request.SheetPath] = group;
        }

        return new GetTablesSheetResponse
        {
            SheetPath = request.SheetPath,
            Tables = group.GetTableInfo()
        };
    }

    private GetTableHeadersSheetResponse ExecuteGetHeaders(GetTableHeadersSheetRequest request)
    {
        var workbooks = GetWorkbooks();
        
        if (!workbooks.TryGetValue(request.SheetPath, out var group))
        {
            group = new Workbook(request.SheetPath);
            workbooks[request.SheetPath] = group;
        }

        if (!group.Sheets.TryGetValue(request.TableName, out var table))
        {
            throw new KeyNotFoundException($"Table '{request.TableName}' not found");
        }

        return new GetTableHeadersSheetResponse
        {
            SheetPath = request.SheetPath,
            TableName = request.TableName,
            Headers = table.Headers.ToList()
        };
    }

    private GetTableRowSheetResponse ExecuteGetRow(GetTableRowSheetRequest request)
    {
        var workbooks = GetWorkbooks();
        
        if (!workbooks.TryGetValue(request.SheetPath, out var group))
        {
            group = new Workbook(request.SheetPath);
            workbooks[request.SheetPath] = group;
        }

        if (!group.Sheets.TryGetValue(request.TableName, out var table))
        {
            throw new KeyNotFoundException($"Table '{request.TableName}' not found");
        }

        return new GetTableRowSheetResponse
        {
            SheetPath = request.SheetPath,
            TableName = request.TableName,
            RowNumber = request.RowNumber,
            RowData = table.GetRow(request.RowNumber)
        };
    }
}
