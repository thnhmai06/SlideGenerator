using System.Collections.Concurrent;
using System.Text.Json;
using Application.Contracts;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Exceptions;
using Domain.Models;
using Presentation.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

/// <summary>
/// SignalR Hub for spreadsheet operations.
/// </summary>
public class SheetHub(ISheetService sheetService, ILogger<SheetHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Workbook>>
        WorkbooksOfConnections = new();

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
            foreach (var key in workbooks.Keys)
                if (workbooks.TryRemove(key, out var wb))
                    wb.Dispose();

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
        Response response;

        var filePath = string.Empty;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(typeStr))
                throw new TypeNotIncludedException(typeof(SheetRequestType));

            filePath = message.GetProperty("filePath").GetString() ?? string.Empty;

            response = typeStr switch
            {
                "openfile" => ExecuteOpenFile(
                    JsonSerializer.Deserialize<OpenFileSheetRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(OpenFileSheetRequest))),
                "closefile" => ExecuteCloseFile(
                    JsonSerializer.Deserialize<CloseFileSheetRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(CloseFileSheetRequest))),
                "gettables" => ExecuteGetTables(
                    JsonSerializer.Deserialize<GetTablesSheetRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(GetTablesSheetRequest))),
                "getheaders" => ExecuteGetHeaders(
                    JsonSerializer.Deserialize<GetTableHeadersSheetRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(GetTableHeadersSheetRequest))),
                "getrow" => ExecuteGetRow(
                    JsonSerializer.Deserialize<GetTableRowSheetRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(GetTableRowSheetRequest))),
                _ => throw new TypeNotIncludedException(typeof(SheetRequestType))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing sheet request");
            response = new SheetError(filePath, ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    private OpenBookSheetSuccess ExecuteOpenFile(OpenFileSheetRequest request)
    {
        GetOrOpenWorkbook(request.FilePath);
        return new OpenBookSheetSuccess(request.FilePath);
    }

    private CloseBookSheetSuccess ExecuteCloseFile(CloseFileSheetRequest request)
    {
        if (Workbooks.TryRemove(request.FilePath, out var wb)) wb.Dispose();

        return new CloseBookSheetSuccess(request.FilePath);
    }

    private GetSheetsSheetSuccess ExecuteGetTables(GetTablesSheetRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new GetSheetsSheetSuccess
        (
            request.FilePath,
            sheetService.GetSheets(workbook)
        );
    }

    private GetHeadersSheetSuccess ExecuteGetHeaders(GetTableHeadersSheetRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new GetHeadersSheetSuccess
        (
            request.FilePath,
            request.SheetName,
            sheetService.GetHeaders(workbook, request.SheetName).ToList()
        );
    }

    private GetRowSheetSuccess ExecuteGetRow(GetTableRowSheetRequest request)
    {
        var workbook = GetOrOpenWorkbook(request.FilePath);

        return new GetRowSheetSuccess
        (
            request.FilePath,
            request.TableName,
            request.RowNumber,
            sheetService.GetRow(workbook, request.TableName, request.RowNumber)
        );
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