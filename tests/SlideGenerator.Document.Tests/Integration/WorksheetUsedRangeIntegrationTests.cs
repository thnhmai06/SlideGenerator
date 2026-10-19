/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: WorksheetUsedRangeIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Document.Workbooks.Components;
using SlideGenerator.Document.Workbooks.Identifiers;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;
using Xunit;
using SfWorksheet = Syncfusion.XlsIO.IWorksheet;

namespace SlideGenerator.Document.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="IReadOnlyWorksheet.RowCount" />, <see cref="IReadOnlyWorksheet.ColumnCount" />
///     and <see cref="IReadOnlyWorksheet.GetRow" /> against real workbooks whose used range does not start at A1.
///     Requires a valid Syncfusion license.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WorksheetUsedRangeIntegrationTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "SlideGenerator.Tests", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // Temp files are best-effort cleanup; a stuck handle must not fail the test run.
        }
    }

    /// <summary>
    ///     Verifies that a header at row 3 followed by two data rows counts as three used-range rows, and that
    ///     row 1 is the header relative to the used range rather than absolute worksheet row 1.
    /// </summary>
    [Fact]
    public async Task RowCount_HeaderAtRow3WithTwoDataRows_CountsOnlyUsedRangeRows()
    {
        using var workbook = await CreateAndOpenAsync("header-at-a3.xlsx", worksheet =>
        {
            worksheet.Range["A3"].Text = "Name";
            worksheet.Range["B3"].Text = "Value";
            worksheet.Range["A4"].Text = "alpha";
            worksheet.Range["B4"].Text = "1";
            worksheet.Range["A5"].Text = "beta";
            worksheet.Range["B5"].Text = "2";
        });
        var worksheet = workbook.Worksheets.Single();

        worksheet.RowCount.Should().Be(3);
        worksheet.GetRow(1).Should().Equal("Name", "Value");
        worksheet.GetRow(2).Should().Equal("alpha", "1");
        worksheet.GetRow(3).Should().Equal("beta", "2");
    }

    /// <summary>
    ///     Verifies that a header at A1 followed by four data rows counts as five used-range rows.
    /// </summary>
    [Fact]
    public async Task RowCount_HeaderAtRow1WithFourDataRows_ReturnsFive()
    {
        using var workbook = await CreateAndOpenAsync("header-at-a1.xlsx", worksheet =>
        {
            worksheet.Range["A1"].Text = "Name";
            worksheet.Range["A2"].Text = "one";
            worksheet.Range["A3"].Text = "two";
            worksheet.Range["A4"].Text = "three";
            worksheet.Range["A5"].Text = "four";
        });
        var worksheet = workbook.Worksheets.Single();

        worksheet.RowCount.Should().Be(5);
        worksheet.GetRow(1).Should().Equal("Name");
    }

    /// <summary>
    ///     Verifies that a used range starting at column C reports only its three used columns and that
    ///     <see cref="IReadOnlyWorksheet.GetRow" /> returns exactly that many cells.
    /// </summary>
    [Fact]
    public async Task ColumnCount_UsedRangeStartingAtColumnC_CountsOnlyUsedColumns()
    {
        using var workbook = await CreateAndOpenAsync("columns-at-c.xlsx", worksheet =>
        {
            worksheet.Range["C1"].Text = "First";
            worksheet.Range["D1"].Text = "Second";
            worksheet.Range["E1"].Text = "Third";
            worksheet.Range["C2"].Text = "a";
            worksheet.Range["D2"].Text = "b";
            worksheet.Range["E2"].Text = "c";
        });
        var worksheet = workbook.Worksheets.Single();

        worksheet.ColumnCount.Should().Be(3);
        worksheet.GetRow(1).Should().Equal("First", "Second", "Third");
        worksheet.GetRow(2).Should().Equal("a", "b", "c");
    }

    /// <summary>
    ///     Verifies that a completely empty worksheet reports no rows or columns and that its first row reads
    ///     as empty instead of throwing.
    /// </summary>
    [Fact]
    public async Task EmptyWorksheet_CountsAndFirstRow_AreEmpty()
    {
        using var workbook = await CreateAndOpenAsync("empty.xlsx", _ => { });
        var worksheet = workbook.Worksheets.Single();

        worksheet.RowCount.Should().Be(0);
        worksheet.ColumnCount.Should().Be(0);
        worksheet.GetRow(1).Should().BeEmpty();
    }

    /// <summary>
    ///     Creates a one-sheet workbook in a temp folder, fills it through the supplied callback, then opens it
    ///     read-only through the production opener.
    /// </summary>
    /// <param name="fileName">The file name to create inside the test's temp folder.</param>
    /// <param name="configure">Callback that writes the worksheet's content.</param>
    /// <returns>The opened read-only workbook, owned by the caller.</returns>
    private async Task<IReadOnlyWorkbook> CreateAndOpenAsync(string fileName, Action<SfWorksheet> configure)
    {
        if (string.IsNullOrEmpty(SyncfusionLicense.Key))
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");

        SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicense.Key);
        Directory.CreateDirectory(_folder);

        var path = Path.Combine(_folder, fileName);
        using (var engine = new ExcelEngine())
        {
            var workbook = engine.Excel.Workbooks.Create(1);
            configure(workbook.Worksheets[0]);
            workbook.SaveAs(path);
        }

        return await new SfWorkbookOpener()
            .OpenWorkbookReadOnlyAsync(new WorkbookIdentifier(path), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }
}
