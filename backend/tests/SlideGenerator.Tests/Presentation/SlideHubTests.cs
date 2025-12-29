using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;
using SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Job.States;
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
            [1] = new("ShapeA", [0x01, 0x02])
        };
        var template = new TestTemplatePresentation("template.pptx", shapes: shapes, imageShapes: imageShapes);
        var templateManager = new FakeSlideTemplateManager(template);
        var jobManager = new FakeJobManager(new FakeActiveJobCollection());

        var hub = new SlideHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<SlideHub>.Instance);
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

        var hub = new SlideHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<SlideHub>.Instance);
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

        var hub = new SlideHub(jobManager, templateManager, new FakeJobStateStore(), NullLogger<SlideHub>.Instance);
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
    public async Task ProcessRequest_GroupCreate_ReturnsGroupInfo()
    {
        var active = new FakeActiveJobCollection();
        var hub = new SlideHub(new FakeJobManager(active),
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-2");

        var json =
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\",\"customSheet\":[\"Sheet1\"]}";
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
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-3");

        var createJson =
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
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
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-4");

        var createJson =
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
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
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-5");

        var createJson =
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
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
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.OutputPath));
    }

    [TestMethod]
    public async Task ProcessRequest_JobControl_PausesJob()
    {
        var active = new FakeActiveJobCollection();
        var jobManager = new FakeJobManager(active);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-6");

        var createJson =
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}";
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
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-7");

        await hub.ProcessRequest(JsonHelper.Parse(
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}"));
        var group1 = proxy.GetPayload<SlideGroupCreateSuccess>();
        Assert.IsNotNull(group1);
        var group1Entity = jobManager.GetGroup(group1.GroupId) as JobGroup;
        Assert.IsNotNull(group1Entity);
        group1Entity.SetStatus(GroupStatus.Running);

        await hub.ProcessRequest(JsonHelper.Parse(
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out2\"}"));
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
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-8");

        await hub.ProcessRequest(JsonHelper.Parse(
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out\"}"));
        await hub.ProcessRequest(JsonHelper.Parse(
            "{\"type\":\"groupcreate\",\"templatePath\":\"template.pptx\",\"spreadsheetPath\":\"book.xlsx\",\"path\":\"C:\\\\out2\"}"));

        await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"getallgroups\"}"));
        var response = proxy.GetPayload<SlideGlobalGetGroupsSuccess>();

        Assert.IsNotNull(response);
        Assert.HasCount(2, response.Groups);
    }

    [TestMethod]
    public async Task ProcessRequest_GroupRemove_RemovesCompletedGroup()
    {
        var completed = new InMemoryCompletedJobCollection();
        var jobManager = new FakeJobManagerWithCompleted(new FakeActiveJobCollection(), completed);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-9a");

        var group = completed.AddCompletedGroup("book.xlsx", "template.pptx", "C:\\out", "Sheet1");

        var removeJson = $"{{\"type\":\"groupremove\",\"groupId\":\"{group.Id}\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(removeJson));

        var response = proxy.GetPayload<SlideGroupRemoveSuccess>();
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Removed);
        Assert.IsFalse(completed.ContainsGroup(group.Id));
    }

    [TestMethod]
    public async Task ProcessRequest_JobRemove_RemovesCompletedSheet()
    {
        var completed = new InMemoryCompletedJobCollection();
        var jobManager = new FakeJobManagerWithCompleted(new FakeActiveJobCollection(), completed);
        var hub = new SlideHub(jobManager,
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-9");

        var group = completed.AddCompletedGroup("book.xlsx", "template.pptx", "C:\\out", "Sheet1");
        var sheetId = group.Sheets.Values.First().Id;

        var removeJson = $"{{\"type\":\"jobremove\",\"jobId\":\"{sheetId}\"}}";
        await hub.ProcessRequest(JsonHelper.Parse(removeJson));

        var response = proxy.GetPayload<SlideJobRemoveSuccess>();
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Removed);
        Assert.IsFalse(completed.ContainsSheet(sheetId));
    }

    [TestMethod]
    public async Task ProcessRequest_JobLogs_ReturnsLogs()
    {
        var jobStateStore = new FakeJobStateStore();
        await jobStateStore.AppendJobLogAsync(
            new JobLogEntry(
                "job-1",
                DateTimeOffset.UtcNow,
                "Info",
                "Row 1 completed",
                new Dictionary<string, object?> { ["row"] = 1 }),
            CancellationToken.None);

        var hub = new SlideHub(new FakeJobManager(new FakeActiveJobCollection()),
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            jobStateStore,
            NullLogger<SlideHub>.Instance);
        var proxy = HubTestHelper.Attach(hub, "conn-10");

        var logsJson = "{\"type\":\"joblogs\",\"jobId\":\"job-1\"}";
        await hub.ProcessRequest(JsonHelper.Parse(logsJson));

        var response = proxy.GetPayload<SlideJobLogsSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual("job-1", response.JobId);
        Assert.HasCount(1, response.Logs);
        Assert.AreEqual("Info", response.Logs[0].Level);
    }

    private sealed class InMemoryCompletedJobCollection : ICompletedJobCollection
    {
        private readonly Dictionary<string, JobGroup> _groups = new();
        private readonly Dictionary<string, JobSheet> _sheets = new();

        public IJobGroup? GetGroup(string groupId)
        {
            return _groups.GetValueOrDefault(groupId);
        }

        public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
        {
            return _groups.ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
        }

        public IEnumerable<IJobGroup> EnumerateGroups()
        {
            return _groups.Values;
        }

        public int GroupCount => _groups.Count;

        public IJobSheet? GetSheet(string sheetId)
        {
            return _sheets.GetValueOrDefault(sheetId);
        }

        public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
        {
            return _sheets.ToDictionary(kv => kv.Key, kv => (IJobSheet)kv.Value);
        }

        public IEnumerable<IJobSheet> EnumerateSheets()
        {
            return _sheets.Values;
        }

        public int SheetCount => _sheets.Count;

        public bool ContainsGroup(string groupId)
        {
            return _groups.ContainsKey(groupId);
        }

        public bool ContainsSheet(string sheetId)
        {
            return _sheets.ContainsKey(sheetId);
        }

        public bool IsEmpty => _groups.Count == 0;

        public bool RemoveGroup(string groupId)
        {
            if (!_groups.Remove(groupId, out var group)) return false;
            foreach (var sheet in group.InternalJobs.Values)
                _sheets.Remove(sheet.Id);
            return true;
        }

        public bool RemoveSheet(string sheetId)
        {
            if (!_sheets.Remove(sheetId, out var sheet)) return false;
            if (_groups.TryGetValue(sheet.GroupId, out var group))
            {
                group.RemoveJob(sheetId);
                if (group.InternalJobs.Count == 0)
                    _groups.Remove(group.Id);
            }

            return true;
        }

        public void ClearAll()
        {
            _groups.Clear();
            _sheets.Clear();
        }

        public IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups()
        {
            return _groups.Where(kv => kv.Value.Status == GroupStatus.Completed)
                .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
        }

        public IReadOnlyDictionary<string, IJobGroup> GetFailedGroups()
        {
            return _groups.Where(kv => kv.Value.Status == GroupStatus.Failed)
                .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
        }

        public IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups()
        {
            return _groups.Where(kv => kv.Value.Status == GroupStatus.Cancelled)
                .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
        }

        public JobGroup AddCompletedGroup(string workbookPath, string templatePath, string outputFolder,
            string sheetName)
        {
            var workbook = new TestSheetBook(workbookPath, new TestSheet(sheetName, 1));
            var template = new TestTemplatePresentation(templatePath);
            var outputDir = new DirectoryInfo(outputFolder);
            var group = new JobGroup(workbook, template, outputDir, [], []);
            var sheet = group.AddJob(sheetName, Path.Combine(outputFolder, $"{sheetName}.pptx"));
            sheet.SetStatus(SheetJobStatus.Completed);
            group.SetStatus(GroupStatus.Completed);
            _groups[group.Id] = group;
            _sheets[sheet.Id] = sheet;
            return group;
        }
    }

    private sealed class FakeJobManagerWithCompleted : IJobManager
    {
        public FakeJobManagerWithCompleted(IActiveJobCollection active, ICompletedJobCollection completed)
        {
            Active = active;
            Completed = completed;
        }

        public IActiveJobCollection Active { get; }
        public ICompletedJobCollection Completed { get; }

        public IJobGroup? GetGroup(string groupId)
        {
            return Active.GetGroup(groupId) ?? Completed.GetGroup(groupId);
        }

        public IJobSheet? GetSheet(string sheetId)
        {
            return Active.GetSheet(sheetId) ?? Completed.GetSheet(sheetId);
        }

        public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
        {
            var result = new Dictionary<string, IJobGroup>();
            foreach (var kv in Active.GetAllGroups())
                result[kv.Key] = kv.Value;
            foreach (var kv in Completed.GetAllGroups())
                result[kv.Key] = kv.Value;
            return result;
        }
    }
}