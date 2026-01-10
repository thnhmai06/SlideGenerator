using SlideGenerator.Domain.Features.Jobs.Entities;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Domain;

[TestClass]
public sealed class JobGroupTests
{
    [TestMethod]
    public void UpdateStatus_FailedBeatsAll()
    {
        var group = CreateGroup(out var sheet1, out var sheet2);
        sheet1.SetStatus(SheetJobStatus.Completed);
        sheet2.SetStatus(SheetJobStatus.Failed);

        group.UpdateStatus();

        Assert.AreEqual(GroupStatus.Failed, group.Status);
    }

    [TestMethod]
    public void UpdateStatus_RunningBeatsPaused()
    {
        var group = CreateGroup(out var sheet1, out var sheet2);
        sheet1.SetStatus(SheetJobStatus.Paused);
        sheet2.SetStatus(SheetJobStatus.Running);

        group.UpdateStatus();

        Assert.AreEqual(GroupStatus.Running, group.Status);
    }

    [TestMethod]
    public void UpdateStatus_PausedWhenAnyPaused()
    {
        var group = CreateGroup(out var sheet1, out var sheet2);
        sheet1.SetStatus(SheetJobStatus.Paused);
        sheet2.SetStatus(SheetJobStatus.Pending);

        group.UpdateStatus();

        Assert.AreEqual(GroupStatus.Paused, group.Status);
    }

    [TestMethod]
    public void UpdateStatus_CancelledWhenOnlyCancelledOrCompleted()
    {
        var group = CreateGroup(out var sheet1, out var sheet2);
        sheet1.SetStatus(SheetJobStatus.Completed);
        sheet2.SetStatus(SheetJobStatus.Cancelled);

        group.UpdateStatus();

        Assert.AreEqual(GroupStatus.Cancelled, group.Status);
    }

    [TestMethod]
    public void UpdateStatus_CompletedWhenAllCompleted()
    {
        var group = CreateGroup(out var sheet1, out var sheet2);
        sheet1.SetStatus(SheetJobStatus.Completed);
        sheet2.SetStatus(SheetJobStatus.Completed);

        group.UpdateStatus();

        Assert.AreEqual(GroupStatus.Completed, group.Status);
    }

    [TestMethod]
    public void Progress_UsesTotalRowsAcrossSheets()
    {
        var group = CreateGroup(out var sheet1, out var sheet2);

        sheet1.UpdateProgress(5); // 50% of 10
        sheet2.UpdateProgress(5); // 25% of 20

        Assert.AreEqual(33.33f, group.Progress, 0.05f);
    }

    private static JobGroup CreateGroup(out JobSheet sheet1, out JobSheet sheet2)
    {
        var workbook = new TestSheetBook("book.xlsx",
            new TestSheet("Sheet1", 10),
            new TestSheet("Sheet2", 20));
        var template = new TestTemplatePresentation("template.pptx");

        var group = new JobGroup(
            workbook,
            template,
            new DirectoryInfo(Path.GetTempPath()),
            [],
            []);

        sheet1 = group.AddJob("Sheet1", "sheet1.pptx");
        sheet2 = group.AddJob("Sheet2", "sheet2.pptx");

        return group;
    }
}