using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Sheets;
using SlideGenerator.Application.Features.Sheets.DTOs.Components;
using SlideGenerator.Application.Features.Sheets.DTOs.Requests.Workbook;
using SlideGenerator.Application.Features.Sheets.DTOs.Requests.Worksheet;
using SlideGenerator.Application.Features.Sheets.DTOs.Responses.Errors;
using SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Workbook;
using SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Worksheet;
using SlideGenerator.Domain.Features.Sheets.Interfaces;
using SlideGenerator.Presentation.Common.Exceptions.Hubs;
using HubBase = SlideGenerator.Presentation.Common.Hubs.Hub;

namespace SlideGenerator.Presentation.Features.Sheets;

/// <summary>
///     SignalR Hub for spreadsheet operations.
/// </summary>
public class SheetHub(ISheetService sheetService, ILogger<SheetHub> logger) : HubBase
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ISheetBook>>
        WorkbooksOfConnections = new();

    private ConcurrentDictionary<string, ISheetBook> Workbooks
        => WorkbooksOfConnections.GetValueOrDefault(Context.ConnectionId)
           ?? throw new ConnectionNotFound(Context.ConnectionId);

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        WorkbooksOfConnections[Context.ConnectionId] = new ConcurrentDictionary<string, ISheetBook>();
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

        // Cleanup open workbooks for this connection
        if (WorkbooksOfConnections.TryRemove(Context.ConnectionId, out var workbooks))
            foreach (var key in workbooks.Keys)
                if (workbooks.TryRemove(key, out var wb))
                    wb.Dispose();

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     Processes a sheet request based on type.
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

    private OpenBookSheetSuccess ExecuteOpenFile(SheetWorkbookOpen request)
    {
        GetOrOpenWorkbook(request.FilePath);
        return new OpenBookSheetSuccess(request.FilePath);
    }

    private SheetWorkbookCloseSuccess ExecuteCloseFile(SheetWorkbookClose request)
    {
        if (Workbooks.TryRemove(request.FilePath, out var wb))
            wb.Dispose();

        return new SheetWorkbookCloseSuccess(request.FilePath);
    }

    private SheetWorkbookGetSheetInfoSuccess ExecuteGetSheets(SheetWorkbookGetSheetInfo request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new SheetWorkbookGetSheetInfoSuccess
        (
            request.FilePath,
            sheetService.GetSheetsInfo(workbook)
        );
    }

    private SheetWorksheetGetHeadersSuccess ExecuteGetHeaders(SheetWorksheetGetHeaders request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new SheetWorksheetGetHeadersSuccess
        (
            request.FilePath,
            request.SheetName,
            sheetService.GetHeaders(workbook, request.SheetName)
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
        var sheetsInfo = sheetService.GetSheetsInfo(workbook);

        var sheets = new List<SheetWorksheetInfo>();
        foreach (var (sheetName, rowCount) in sheetsInfo)
        {
            var headers = sheetService.GetHeaders(workbook, sheetName);
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