using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Application.Job;
using SlideGenerator.Presentation.Hubs;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Presentation;

[TestClass]
public sealed class SlideHubSubscriptionTests
{
    [TestMethod]
    public async Task SubscribeGroup_AddsConnectionToGroup()
    {
        var groupManager = new TestGroupManager();
        var hub = new SlideHub(new FakeJobManager(new FakeActiveJobCollection()),
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        HubTestHelper.Attach(hub, "conn-1", groupManager);

        await hub.SubscribeGroup("group-1");

        Assert.HasCount(1, groupManager.Added);
        Assert.AreEqual(JobSignalRGroups.GroupGroup("group-1"), groupManager.Added[0].GroupName);
    }

    [TestMethod]
    public async Task SubscribeSheet_AddsConnectionToGroup()
    {
        var groupManager = new TestGroupManager();
        var hub = new SlideHub(new FakeJobManager(new FakeActiveJobCollection()),
            new FakeSlideTemplateManager(new TestTemplatePresentation("template.pptx")),
            new FakeJobStateStore(),
            NullLogger<SlideHub>.Instance);
        HubTestHelper.Attach(hub, "conn-2", groupManager);

        await hub.SubscribeSheet("sheet-1");

        Assert.HasCount(1, groupManager.Added);
        Assert.AreEqual(JobSignalRGroups.SheetGroup("sheet-1"), groupManager.Added[0].GroupName);
    }
}