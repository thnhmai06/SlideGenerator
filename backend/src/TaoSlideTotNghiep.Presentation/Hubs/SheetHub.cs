using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;
using TaoSlideTotNghiep.Application.Base.DTOs.Responses;
using TaoSlideTotNghiep.Application.Sheet.Contracts;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Components;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Requests.Workbook;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Requests.Worksheet;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Errors;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Successes.Workbook;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Successes.Worksheet;
using TaoSlideTotNghiep.Domain.Sheet.Interfaces;
using TaoSlideTotNghiep.Presentation.Exceptions.Hubs;

namespace TaoSlideTotNghiep.Presentation.Hubs;

/// <summary>
/// SignalR Hub for spreadsheet operations.
/// </summary>
public class SheetHub(ISheetService sheetService, ILogger<SheetHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ISheetBook>>
        WorkbooksOfConnections = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[Sheet] Client connected: {ConnectionId}", Context.ConnectionId);
        WorkbooksOfConnections[Context.ConnectionId] = new ConcurrentDictionary<string, ISheetBook>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("[Sheet] Client disconnected: {ConnectionId}", Context.ConnectionId);

        // Cleanup open workbooks for this connection
        if (WorkbooksOfConnections.TryRemove(Context.ConnectionId, out var workbooks))
            foreach (var key in workbooks.Keys)
                if (workbooks.TryRemove(key, out var wb))
                    wb.Dispose();

        await base.OnDisconnectedAsync(exception);
    }

    private ConcurrentDictionary<string, ISheetBook> Workbooks
        => WorkbooksOfConnections.GetValueOrDefault(Context.ConnectionId)
           ?? throw new ConnectionNotFoundException(Context.ConnectionId);

    /// <summary>
    /// Processes a sheet request based on type.
    /// </summary>
    public async Task ProcessRequest(JsonElement message)
    {
        Response response;
        var filePath = string.Empty;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();
            filePath = message.GetProperty("filePath").GetString() ?? string.Empty;

            response = typeStr switch
            {
                "openfile" => ExecuteOpenFile(
                    Deserialize<SheetWorkbookOpen>(message)),
                "closefile" => ExecuteCloseFile(
                    Deserialize<SheetWorkbookClose>(message)),
                "gettables" => ExecuteGetSheets(
                    Deserialize<SheetWorkbookGetSheetInfo>(message)),
                "getheaders" => ExecuteGetHeaders(
                    Deserialize<SheetWorksheetGetHeaders>(message)),
                "getrow" => ExecuteGetRow(
                    Deserialize<SheetWorksheetGetRow>(message)),
                "getworkbookinfo" => ExecuteGetWorkbookInfo(
                    Deserialize<GetWorkbookInfoRequest>(message)),
                _ => throw new ArgumentOutOfRangeException(nameof(typeStr), typeStr, null)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing sheet request");
            response = new SheetError(filePath, ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    private T Deserialize<T>(JsonElement message)
    {
        return JsonSerializer.Deserialize<T>(message.GetRawText(), SerializerOptions)
               ?? throw new InvalidRequestFormatException(typeof(T).Name);
    }

    private OpenBookSheetSuccess ExecuteOpenFile(SheetWorkbookOpen request)
    {
        GetOrOpenWorkbook(request.FilePath);
        return new OpenBookSheetSuccess(request.FilePath);
    }

    private SheetWorkbookCloseSuccess ExecuteCloseFile(SheetWorkbookClose request)
    {
        if (Workbooks.TryRemove(request.FilePath, out var wb)) wb.Dispose();

        return new SheetWorkbookCloseSuccess(request.FilePath);
    }

    private SheetWorkbookGetSheetInfoSuccess ExecuteGetSheets(SheetWorkbookGetSheetInfo request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new SheetWorkbookGetSheetInfoSuccess
        (
            request.FilePath,
            sheetService.GetSheets(workbook)
        );
    }

    private SheetWorksheetGetHeadersSuccess ExecuteGetHeaders(SheetWorksheetGetHeaders request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new SheetWorksheetGetHeadersSuccess
        (
            request.FilePath,
            request.SheetName,
            sheetService.GetHeaders(workbook, request.SheetName).ToList()
        );
    }

    private SheetWorksheetGetRowSuccess ExecuteGetRow(SheetWorksheetGetRow request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new SheetWorksheetGetRowSuccess
        (
            request.FilePath,
            request.TableName,
            request.RowNumber,
            sheetService.GetRow(workbook, request.TableName, request.RowNumber)
        );
    }

    private SheetWorkbookGetInfoSuccess ExecuteGetWorkbookInfo(GetWorkbookInfoRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);
        var sheetsInfo = sheetService.GetSheets(workbook);

        var sheets = new List<SheetWorksheetInfo>();
        foreach (var (sheetName, rowCount) in sheetsInfo)
        {
            var headers = sheetService.GetHeaders(workbook, sheetName).ToList();
            sheets.Add(new SheetWorksheetInfo(sheetName, headers, rowCount));
        }

        return new SheetWorkbookGetInfoSuccess(request.FilePath, workbook.Name, sheets);
    }

    private ISheetBook GetOrOpenWorkbook(string sheetPath)
    {
        if (Workbooks.TryGetValue(sheetPath, out var workbook))
            return workbook;
        workbook = sheetService.OpenFile(sheetPath);
        Workbooks[sheetPath] = workbook;

        return workbook;
    }
}