using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Application.Features.Slides.DTOs.Enums;
using SlideGenerator.Application.Features.Slides.DTOs.Responses.Successes;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Slides.Components;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Presentation;

[TestClass]
public sealed class TaskHubTests
{
    [TestMethod]
    public async Task ProcessRequest_ScanShapes_ReturnsShapes()
    {
        var shapes = new List<ShapeInfo>
        {
            new(1, "ShapeA", "Shape", true),
            new(2, "ShapeB", "Shape", false)
        };
        var imageShapes = new Dictionary<uint, ImagePreview>
        {
            [1] = new("ShapeA", [0x01, 0x02])
        };
        var template = new TestTemplatePresentation("template.pptx", shapes: shapes, imageShapes: imageShapes);
        var templateManager = new FakeSlideTemplateManager(template);
        var jobManager = new FakeJobManager(new FakeActiveJobCollection());

        var hub = new TaskHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<TaskHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-1");

        var message = JsonHelper.Parse("{\"type\":\"scanshapes\",\"filePath\":\"template.pptx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SlideScanShapesSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual("template.pptx", response.FilePath);
        Assert.HasCount(2, response.Shapes);
        Assert.AreEqual("ShapeA", response.Shapes[0].Name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.Shapes[0].Data));
    }

    [TestMethod]
    public async Task ProcessRequest_ScanPlaceholders_ReturnsPlaceholders()
    {
        var placeholders = new[] { "{{Name}}", "{{Code}}" };
        var template = new TestTemplatePresentation("template.pptx", placeholders: placeholders);
        var templateManager = new FakeSlideTemplateManager(template);
        var jobManager = new FakeJobManager(new FakeActiveJobCollection());

        var hub = new TaskHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<TaskHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-1b");

        var message = JsonHelper.Parse("{\"type\":\"scanplaceholders\",\"filePath\":\"template.pptx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SlideScanPlaceholdersSuccess>();
        Assert.IsNotNull(response);
        CollectionAssert.AreEquivalent(placeholders, response.Placeholders);
    }

    [TestMethod]
    public async Task ProcessRequest_ScanTemplate_ReturnsShapesAndPlaceholders()
    {
        var shapes = new List<ShapeInfo>
        {
            new(10, "CoverImage", "Picture", true)
        };
        var imageShapes = new Dictionary<uint, ImagePreview>
        {
            [10] = new("CoverImage", [0x10, 0x20])
        };
        var placeholders = new[] { "{{Title}}", "{{Date}}" };
        var template = new TestTemplatePresentation(
            "template.pptx",
            shapes: shapes,
            imageShapes: imageShapes,
            placeholders: placeholders);
        var templateManager = new FakeSlideTemplateManager(template);
        var jobManager = new FakeJobManager(new FakeActiveJobCollection());

        var hub = new TaskHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<TaskHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-1c");

        var message = JsonHelper.Parse("{\"type\":\"scantemplate\",\"filePath\":\"template.pptx\"}");
        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<SlideScanTemplateSuccess>();
        Assert.IsNotNull(response);
        Assert.HasCount(1, response.Shapes);
        Assert.AreEqual("CoverImage", response.Shapes[0].Name);
        CollectionAssert.AreEquivalent(placeholders, response.Placeholders);
    }

    [TestMethod]
    public async Task ProcessRequest_TaskCreate_Group_ReturnsSummaryAndSheetIds()
    {
        var hub = CreateHub(out var proxy, out _);

        var json =
            "{\"type\":\"taskcreate\",\"taskType\":\"Group\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"outputPath\":\"C:\\\\out\",\"sheetNames\":[\"Sheet1\"]}";
        await hub.ProcessRequest(JsonHelper.Parse(json));

        var response = proxy.GetPayload<TaskCreateSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(TaskType.Group, response.Task.TaskType);
        Assert.AreEqual(TaskState.Processing, response.Task.Status);
        Assert.IsNotNull(response.SheetTaskIds);
        Assert.HasCount(1, response.SheetTaskIds);
        Assert.AreEqual(Path.GetFullPath("C:\\out"), response.Task.OutputPath);
    }

    [TestMethod]
    public async Task ProcessRequest_TaskCreate_Sheet_ReturnsSheetSummary()
    {
        var hub = CreateHub(out var proxy, out var jobManager);

        var json =
            "{\"type\":\"taskcreate\",\"taskType\":\"Sheet\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"outputPath\":\"C:\\\\out\",\"sheetName\":\"Sheet2\"}";
        await hub.ProcessRequest(JsonHelper.Parse(json));

        var response = proxy.GetPayload<TaskCreateSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(TaskType.Sheet, response.Task.TaskType);
        Assert.AreEqual("Sheet2", response.Task.SheetName);
        Assert.IsNull(response.SheetTaskIds);
        Assert.IsNotNull(jobManager.GetSheet(response.Task.TaskId));
    }

    [TestMethod]
    public async Task ProcessRequest_TaskQuery_ReturnsDetailWithSheets()
    {
        var hub = CreateHub(out var proxy, out _);

        var createJson =
            "{\"type\":\"taskcreate\",\"taskType\":\"Group\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"outputPath\":\"C:\\\\out\",\"sheetNames\":[\"Sheet1\"]}";
        await hub.ProcessRequest(JsonHelper.Parse(createJson));
        var created = proxy.GetPayload<TaskCreateSuccess>();
        Assert.IsNotNull(created);

        var queryJson =
            $"{{\"type\":\"taskquery\",\"taskId\":\"{created.Task.TaskId}\",\"taskType\":\"Group\",\"includeSheets\":true}}";
        await hub.ProcessRequest(JsonHelper.Parse(queryJson));

        var response = proxy.GetPayload<TaskQuerySuccess>();
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Task);
        Assert.AreEqual(created.Task.TaskId, response.Task.TaskId);
        Assert.IsNotNull(response.Task.Sheets);
        Assert.HasCount(1, response.Task.Sheets);
    }

    [TestMethod]
    public async Task ProcessRequest_TaskControl_PausesGroup()
    {
        var hub = CreateHub(out var proxy, out var jobManager);

        var createJson =
            "{\"type\":\"taskcreate\",\"taskType\":\"Group\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"outputPath\":\"C:\\\\out\"}";
        await hub.ProcessRequest(JsonHelper.Parse(createJson));
        var created = proxy.GetPayload<TaskCreateSuccess>();
        Assert.IsNotNull(created);

        var controlJson =
            $"{{\"type\":\"taskcontrol\",\"taskId\":\"{created.Task.TaskId}\",\"taskType\":\"Group\",\"action\":\"Pause\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(controlJson));

        var response = proxy.GetPayload<TaskControlSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(ControlAction.Pause, response.Action);
        Assert.AreEqual(GroupStatus.Paused, jobManager.GetGroup(created.Task.TaskId)!.Status);
    }

    private static TaskHub CreateHub(out CaptureClientProxy proxy, out FakeJobManager jobManager)
    {
        var active = new FakeActiveJobCollection();
        jobManager = new FakeJobManager(active);
        var templateManager = new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx"));

        var hub = new TaskHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<TaskHub>.Instance);
        proxy = HubTestHelper.Attach(hub, "conn-2");
        return hub;
    }
}