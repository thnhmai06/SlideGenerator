using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Slide.Components;
using SlideGenerator.Presentation.Hubs;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Presentation;

[TestClass]
public sealed class SlideHubTests
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
            [1] = new ImagePreview("ShapeA", [0x01, 0x02])
        };
        var template = new TestTemplatePresentation("template.pptx", shapes: shapes, imageShapes: imageShapes);
        var templateManager = new FakeSlideTemplateManager(template);
        var jobManager = new FakeJobManager(new FakeActiveJobCollection());

        var hub = new SlideHub(jobManager, templateManager, NullLogger<SlideHub>.Instance);
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
    public async Task ProcessRequest_GroupCreate_ReturnsGroupInfo()
    {
        var active = new FakeActiveJobCollection();
        var hub = new SlideHub(new FakeJobManager(active),
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-2");

        var json = "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\",\"customSheet\":[\"Sheet1\"]}";
        await hub.ProcessRequest(JsonHelper.Parse(json));

        var response = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual("C:\\out", response.OutputFolder);
        Assert.HasCount(1, response.JobIds);
    }

    [TestMethod]
    public async Task ProcessRequest_GroupStatus_ReturnsStatus()
    {
        var active = new FakeActiveJobCollection();
        var jobManager = new FakeJobManager(active);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-3");

        var createJson = "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
        await hub.ProcessRequest(JsonHelper.Parse(createJson));
        var created = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(created);

        var statusJson = $"{{\"type\":\"groupstatus\",\"groupId\":\"{created.GroupId}\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(statusJson));

        var response = proxy.GetPayload<SlideGroupStatusSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(created.GroupId, response.GroupId);
        Assert.AreEqual(GroupStatus.Running, response.Status);
        Assert.HasCount(created.JobIds.Count, response.Jobs);
    }

    [TestMethod]
    public async Task ProcessRequest_GroupControl_PausesGroup()
    {
        var active = new FakeActiveJobCollection();
        var jobManager = new FakeJobManager(active);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-4");

        var createJson = "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
        await hub.ProcessRequest(JsonHelper.Parse(createJson));
        var created = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(created);

        var controlJson = $"{{\"type\":\"groupcontrol\",\"groupId\":\"{created.GroupId}\",\"action\":\"Pause\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(controlJson));

        var response = proxy.GetPayload<SlideGroupControlSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(created.GroupId, response.GroupId);
        Assert.AreEqual(GroupStatus.Paused, jobManager.GetGroup(created.GroupId)!.Status);
    }

    [TestMethod]
    public async Task ProcessRequest_JobStatus_ReturnsJobInfo()
    {
        var active = new FakeActiveJobCollection();
        var jobManager = new FakeJobManager(active);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-5");

        var createJson = "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
        await hub.ProcessRequest(JsonHelper.Parse(createJson));
        var created = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(created);
        var jobId = created.JobIds.Values.First();

        var statusJson = $"{{\"type\":\"jobstatus\",\"jobId\":\"{jobId}\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(statusJson));

        var response = proxy.GetPayload<SlideJobStatusSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(jobId, response.JobId);
        Assert.AreEqual(SheetJobStatus.Pending, response.Status);
    }

    [TestMethod]
    public async Task ProcessRequest_JobControl_PausesJob()
    {
        var active = new FakeActiveJobCollection();
        var jobManager = new FakeJobManager(active);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-6");

        var createJson = "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
        await hub.ProcessRequest(JsonHelper.Parse(createJson));
        var created = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(created);
        var jobId = created.JobIds.Values.First();

        var controlJson = $"{{\"type\":\"jobcontrol\",\"jobId\":\"{jobId}\",\"action\":\"Pause\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(controlJson));

        var response = proxy.GetPayload<SlideJobControlSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(jobId, response.JobId);
        Assert.AreEqual(SheetJobStatus.Paused, jobManager.GetSheet(jobId)!.Status);
    }

    [TestMethod]
    public async Task ProcessRequest_GlobalControl_CountsActiveJobs()
    {
        var active = new FakeActiveJobCollection();
        var jobManager = new FakeJobManager(active);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-7");

        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}"));
        var group1 = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(group1);
        var group1Entity = jobManager.GetGroup(group1.GroupId) as JobGroup;
        Assert.IsNotNull(group1Entity);
        group1Entity.SetStatus(GroupStatus.Running);

        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out2\"}"));
        var group2 = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(group2);
        var group2Entity = jobManager.GetGroup(group2.GroupId) as JobGroup;
        Assert.IsNotNull(group2Entity);
        group2Entity.SetStatus(GroupStatus.Completed);

        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"globalcontrol\",\"action\":\"Pause\"}"));
        var response = proxy.GetPayload<SlideGlobalControlSuccess>();

        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.AffectedGroups);
        Assert.AreEqual(2, response.AffectedJobs);
    }

    [TestMethod]
    public async Task ProcessRequest_GetAllGroups_ReturnsSummaries()
    {
        var active = new FakeActiveJobCollection();
        var hub = new SlideHub(new FakeJobManager(active),
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-8");

        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}"));
        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out2\"}"));

        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"getallgroups\"}"));
        var response = proxy.GetPayload<SlideGlobalGetGroupsSuccess>();

        Assert.IsNotNull(response);
        Assert.HasCount(2, response.Groups);
    }
}
