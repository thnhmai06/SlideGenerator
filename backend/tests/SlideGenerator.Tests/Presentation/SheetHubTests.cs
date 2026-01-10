using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Workbook;
using SlideGenerator.Application.Features.Sheets.DTOs.Responses.Successes.Worksheet;
using SlideGenerator.Presentation.Features.Sheets;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Presentation;

[TestClass]
public sealed class SheetHubTests
{
    [TestMethod]
    public async Task ProcessRequest_OpenFile_ReturnsSuccess()
    {
        var hub = CreateHub(out var proxy);
        await hub.OnConnectedAsync();

        var message = JsonHelper.Parse("{\"type\":\"openfile\",\"filePath\":\"book.xlsx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<OpenBookSheetSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual("book.xlsx", response.FilePath);
    }

    [TestMethod]
    public async Task ProcessRequest_GetTables_ReturnsSheetInfo()
    {
        var hub = CreateHub(out var proxy);
        await hub.OnConnectedAsync();

        var message = JsonHelper.Parse("{\"type\":\"gettables\",\"filePath\":\"book.xlsx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SheetWorkbookGetSheetInfoSuccess>();
        Assert.IsNotNull(response);
        Assert.HasCount(1, response.Sheets);
        Assert.AreEqual(2, response.Sheets["Sheet1"]);
    }

    [TestMethod]
    public async Task ProcessRequest_GetHeaders_ReturnsHeaders()
    {
        var hub = CreateHub(out var proxy);
        await hub.OnConnectedAsync();

        var message = JsonHelper.Parse("{\"type\":\"getheaders\",\"filePath\":\"book.xlsx\",\"sheetName\":\"Sheet1\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SheetWorksheetGetHeadersSuccess>();
        Assert.IsNotNull(response);
        Assert.HasCount(2, response.Headers);
        Assert.AreEqual("Name", response.Headers[0]);
    }

    [TestMethod]
    public async Task ProcessRequest_GetRow_ReturnsRowData()
    {
        var hub = CreateHub(out var proxy);
        await hub.OnConnectedAsync();

        var message =
            JsonHelper.Parse(
                "{\"type\":\"getrow\",\"filePath\":\"book.xlsx\",\"tableName\":\"Sheet1\",\"rowNumber\":1}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SheetWorksheetGetRowSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual("Alice", response.Row["Name"]);
    }

    [TestMethod]
    public async Task ProcessRequest_GetWorkbookInfo_ReturnsDetails()
    {
        var hub = CreateHub(out var proxy);
        await hub.OnConnectedAsync();

        var message = JsonHelper.Parse("{\"type\":\"getworkbookinfo\",\"filePath\":\"book.xlsx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SheetWorkbookGetInfoSuccess>();
        Assert.IsNotNull(response);
        Assert.HasCount(1, response.Sheets);
        Assert.AreEqual("Sheet1", response.Sheets[0].Name);
    }

    [TestMethod]
    public async Task ProcessRequest_CloseFile_ReturnsSuccess()
    {
        var hub = CreateHub(out var proxy);
        await hub.OnConnectedAsync();

        var message = JsonHelper.Parse("{\"type\":\"closefile\",\"filePath\":\"book.xlsx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SheetWorkbookCloseSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual("book.xlsx", response.FilePath);
    }

    private static SheetHub CreateHub(out CaptureClientProxy proxy)
    {
        var headers = new List<string?> { "Name", "Url" };
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["Name"] = "Alice", ["Url"] = "http://a" },
            new() { ["Name"] = "Bob", ["Url"] = "http://b" }
        };
        var sheet = new TestSheet("Sheet1", rows.Count, headers, rows);
        var workbook = new TestSheetBook("book.xlsx", sheet);
        var sheetService = new FakeSheetService(workbook);

        var hub = new SheetHub(sheetService, NullLogger<SheetHub>.Instance);
        proxy = HubTestHelper.Attach(hub, "conn-1");
        return hub;
    }
}