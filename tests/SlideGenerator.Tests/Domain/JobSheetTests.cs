using SlideGenerator.Domain.Job.Components;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Domain;

[TestClass]
public sealed class JobSheetTests
{
    [TestMethod]
    public void UpdateProgress_ClampsToRowCount()
    {
        var sheet = CreateSheet(rowCount: 5);

        sheet.UpdateProgress(-3);
        Assert.AreEqual(0, sheet.CurrentRow);

        sheet.UpdateProgress(10);
        Assert.AreEqual(5, sheet.CurrentRow);
    }

    [TestMethod]
    public void NextRowIndex_TracksCurrentRow()
    {
        var sheet = CreateSheet(rowCount: 5);
        sheet.UpdateProgress(2);

        Assert.AreEqual(3, sheet.NextRowIndex);
    }

    [TestMethod]
    public void Pause_SetsStatusPaused()
    {
        var sheet = CreateSheet(rowCount: 3);

        sheet.SetStatus(SheetJobStatus.Running);
        sheet.Pause();

        Assert.AreEqual(SheetJobStatus.Paused, sheet.Status);
    }

    [TestMethod]
    public void RegisterRowError_IncrementsErrorCount()
    {
        var sheet = CreateSheet(rowCount: 3);

        sheet.RegisterRowError(1, "bad image");
        sheet.RegisterRowError(2, "bad image");

        Assert.AreEqual(2, sheet.ErrorCount);
    }

    private static JobSheet CreateSheet(int rowCount)
    {
        var worksheet = new TestSheet("SheetA", rowCount);
        return new JobSheet(
            "group",
            worksheet,
            "output.pptx",
            [],
            []);
    }
}
