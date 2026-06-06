/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipeRepositoryPackageTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Models.Graphs;
using SlideGenerator.Recipe.Domain.Rules;
using SlideGenerator.Recipe.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Tests for <see cref="RecipeRepository" /> Export and Import path-rewriting behaviour:
///     exported zip archives must store relative filenames in <c>Graph.json</c>, and imported
///     graphs must reconstruct absolute paths pointing to the save folder.
/// </summary>
public sealed class RecipeRepositoryPackageTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly List<string> _cleanupDirs = [];
    private readonly List<string> _cleanupFiles = [];
    private readonly RecipeRepository _repo;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database with an anchor connection.
    /// </summary>
    public RecipeRepositoryPackageTests()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"memory_{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };
        _anchor = new SqliteConnection(builder.ConnectionString);
        _anchor.Open();
        _repo = new RecipeRepository(builder);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
        foreach (var f in _cleanupFiles)
            try
            {
                if (File.Exists(f)) File.Delete(f);
            }
            catch
            {
                /* ignore */
            }

        foreach (var d in _cleanupDirs)
            try
            {
                if (Directory.Exists(d)) Directory.Delete(d, true);
            }
            catch
            {
                /* ignore */
            }
    }

    #region Export

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ExportAsync" /> writes <c>Graph.json</c> with a
    ///     relative filename for a <see cref="WorkbookNode" />, not the original absolute path.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WorkbookNodeInGraph_ZipGraphJsonContainsRelativeFilename()
    {
        var dir = NewTempDir();
        var wbPath = CreateDummyFile(dir, "sales.xlsx");
        var graph = new RecipeGraph(
            [new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath))], []);
        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var zipPath = NewZipPath(dir);

        await _repo.ExportAsync(metadata.Id, zipPath, null, TestContext.Current.CancellationToken);

        var graphJson = ReadGraphJsonFromZip(zipPath);
        graphJson.Should().Contain("\"sales.xlsx\"");
        graphJson.Should().NotContain(wbPath.Replace("\\", "\\\\"),
            "absolute path must not appear JSON-escaped in Graph.json");
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ExportAsync" /> writes <c>Graph.json</c> with a
    ///     relative filename for a <see cref="PresentationNode" />, not the original absolute path.
    /// </summary>
    [Fact]
    public async Task ExportAsync_PresentationNodeInGraph_ZipGraphJsonContainsRelativeFilename()
    {
        var dir = NewTempDir();
        var presPath = CreateDummyFile(dir, "template.pptx");
        var graph = new RecipeGraph(
            [new PresentationNode("pres1", new Point(0, 0), new PresentationIdentifier(presPath))], []);
        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var zipPath = NewZipPath(dir);

        await _repo.ExportAsync(metadata.Id, zipPath, null, TestContext.Current.CancellationToken);

        var graphJson = ReadGraphJsonFromZip(zipPath);
        graphJson.Should().Contain("\"template.pptx\"");
        graphJson.Should().NotContain(presPath.Replace("\\", "\\\\"),
            "absolute path must not appear JSON-escaped in Graph.json");
    }

    /// <summary>
    ///     Verifies that when two workbooks share the same filename stem, the zip <c>Graph.json</c>
    ///     records deduplicated relative names using the stem-based scheme (e.g. <c>data.xlsx</c>,
    ///     <c>data_1.xlsx</c>) and contains no absolute paths.
    /// </summary>
    [Fact]
    public async Task ExportAsync_TwoWorkbooksWithSameStem_ZipGraphJsonContainsDeduplicatedFilenames()
    {
        var dirA = NewTempDir();
        var dirB = NewTempDir();
        var wbPath1 = CreateDummyFile(dirA, "data.xlsx");
        var wbPath2 = CreateDummyFile(dirB, "data.xlsx");
        var graph = new RecipeGraph(
        [
            new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath1)),
            new WorkbookNode("wb2", new Point(100, 0), new WorkbookIdentifier(wbPath2))
        ], []);
        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var zipPath = NewZipPath(dirA);

        await _repo.ExportAsync(metadata.Id, zipPath, null, TestContext.Current.CancellationToken);

        var graphJson = ReadGraphJsonFromZip(zipPath);
        graphJson.Should().Contain("\"data.xlsx\"");
        graphJson.Should().Contain("\"data_1.xlsx\"");
        graphJson.Should().NotContain(wbPath1.Replace("\\", "\\\\"));
        graphJson.Should().NotContain(wbPath2.Replace("\\", "\\\\"));
    }

    /// <summary>
    ///     Verifies that a workbook filename containing spaces is serialised as valid JSON in
    ///     <c>Graph.json</c>.
    /// </summary>
    [Fact]
    public async Task ExportAsync_FilenameWithSpaces_ZipGraphJsonContainsValidJson()
    {
        var dir = NewTempDir();
        var wbPath = CreateDummyFile(dir, "my file.xlsx");
        var graph = new RecipeGraph(
            [new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath))], []);
        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var zipPath = NewZipPath(dir);

        await _repo.ExportAsync(metadata.Id, zipPath, null, TestContext.Current.CancellationToken);

        var graphJson = ReadGraphJsonFromZip(zipPath);
        graphJson.Should().Contain("\"my file.xlsx\"");
        var act = () => JsonDocument.Parse(graphJson);
        act.Should().NotThrow("Graph.json must remain valid JSON");
    }

    #endregion

    #region Import

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> reconstructs an absolute path in
    ///     the workbooks save folder when the zip <c>Graph.json</c> used a relative filename, and that
    ///     the file is physically extracted there.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ZipWithWorkbookRelativePath_GraphHasAbsolutePathInSaveFolder()
    {
        var sandbox = NewTempDir();
        var workbooksDir = Path.Combine(sandbox, "workbooks");
        var presentationsDir = Path.Combine(sandbox, "presentations");
        Directory.CreateDirectory(workbooksDir);
        Directory.CreateDirectory(presentationsDir);

        const string graphJson =
            """{"nodes":[{"id":"wb1","type":"Workbook","position":{"x":0,"y":0},"workbook":{"bookPath":"data.xlsx"}}],"edges":[]}""";
        var zipPath = NewZipPath(sandbox);
        await BuildZipAsync(zipPath, graphJson, ("Workbooks/data.xlsx", new byte[64]));

        var metadata = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
            TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var node = entry.Graph.Nodes.OfType<WorkbookNode>().Single();
        var expectedPath = Path.GetFullPath(Path.Combine(workbooksDir, "data.xlsx"));
        node.Workbook.BookPath.Should().Be(expectedPath);
        File.Exists(node.Workbook.BookPath).Should().BeTrue("workbook file must be physically extracted");
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> reconstructs an absolute path in
    ///     the presentations save folder when the zip <c>Graph.json</c> used a relative filename, and
    ///     that the file is physically extracted there.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ZipWithPresentationRelativePath_GraphHasAbsolutePathInSaveFolder()
    {
        var sandbox = NewTempDir();
        var workbooksDir = Path.Combine(sandbox, "workbooks");
        var presentationsDir = Path.Combine(sandbox, "presentations");
        Directory.CreateDirectory(workbooksDir);
        Directory.CreateDirectory(presentationsDir);

        const string graphJson =
            """{"nodes":[{"id":"pres1","type":"Presentation","position":{"x":0,"y":0},"presentation":{"presentationPath":"template.pptx"}}],"edges":[]}""";
        var zipPath = NewZipPath(sandbox);
        await BuildZipAsync(zipPath, graphJson, ("Presentations/template.pptx", new byte[64]));

        var metadata = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
            TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var node = entry.Graph.Nodes.OfType<PresentationNode>().Single();
        var expectedPath = Path.GetFullPath(Path.Combine(presentationsDir, "template.pptx"));
        node.Presentation.PresentationPath.Should().Be(expectedPath);
        File.Exists(node.Presentation.PresentationPath).Should()
            .BeTrue("presentation file must be physically extracted");
    }

    /// <summary>
    ///     Verifies that a zip entry not referenced by any node in <c>Graph.json</c> is silently
    ///     skipped and is not written to the save folder (allowlist enforced).
    /// </summary>
    [Fact]
    public async Task ImportAsync_ExtraFileNotInGraph_IsNotExtracted()
    {
        var sandbox = NewTempDir();
        var workbooksDir = Path.Combine(sandbox, "workbooks");
        var presentationsDir = Path.Combine(sandbox, "presentations");
        Directory.CreateDirectory(workbooksDir);
        Directory.CreateDirectory(presentationsDir);

        const string graphJson = """{"nodes":[],"edges":[]}""";
        var zipPath = NewZipPath(sandbox);
        await BuildZipAsync(zipPath, graphJson, ("Workbooks/extra.xlsx", new byte[32]));

        await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
            TestContext.Current.CancellationToken);

        File.Exists(Path.Combine(workbooksDir, "extra.xlsx")).Should().BeFalse(
            "zip entries not referenced by the graph must not be extracted");
    }

    /// <summary>
    ///     Round-trip test: exports a recipe that references a real workbook file, then imports the
    ///     archive into a different folder and verifies the imported graph's path resolves to the
    ///     extracted file in the new save folder.
    /// </summary>
    [Fact]
    public async Task ImportAsync_RoundTrip_WorkbookNodePathResolvesToExtractedFile()
    {
        var srcDir = NewTempDir();
        var wbPath = CreateDummyFile(srcDir, "report.xlsx");
        var graph = new RecipeGraph(
            [new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath))], []);
        var exported = await _repo.AddAsync(new RecipeInput("Round-trip", graph),
            TestContext.Current.CancellationToken);
        var zipPath = NewZipPath(srcDir);
        await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

        var importSandbox = NewTempDir();
        var workbooksDir = Path.Combine(importSandbox, "workbooks");
        var presentationsDir = Path.Combine(importSandbox, "presentations");
        var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
            TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

        var node = entry.Graph.Nodes.OfType<WorkbookNode>().Single();
        var expectedPath = Path.GetFullPath(Path.Combine(workbooksDir, "report.xlsx"));
        node.Workbook.BookPath.Should().Be(expectedPath);
        File.Exists(node.Workbook.BookPath).Should()
            .BeTrue("exported file must be physically present after round-trip import");
    }

    #endregion

    #region Test Helpers

    /// <summary>Creates a temp directory and registers it for cleanup.</summary>
    private string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rpkg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _cleanupDirs.Add(dir);
        return dir;
    }

    /// <summary>Creates a dummy file with the given name inside <paramref name="dir" />.</summary>
    private static string CreateDummyFile(string dir, string name)
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        File.WriteAllBytes(path, new byte[64]);
        return path;
    }

    /// <summary>Registers and returns a new <c>.recipe</c> zip path inside <paramref name="dir" />.</summary>
    private string NewZipPath(string dir)
    {
        var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + RecipePackageRules.PackageExtension);
        _cleanupFiles.Add(path);
        return path;
    }

    /// <summary>Reads and returns the content of <c>Graph.json</c> from the given zip file.</summary>
    private static string ReadGraphJsonFromZip(string zipPath)
    {
        using var fs = File.OpenRead(zipPath);
        using var zipFile = new ZipFile(fs);
        foreach (ZipEntry entry in zipFile)
        {
            if (!entry.IsFile) continue;
            if (!string.Equals(entry.Name, "Graph.json", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = zipFile.GetInputStream(entry);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        throw new InvalidOperationException("Graph.json not found in zip.");
    }

    /// <summary>
    ///     Builds a <c>.recipe</c> zip at <paramref name="zipPath" /> containing <c>Graph.json</c>
    ///     and any additional entries supplied via <paramref name="extraEntries" />.
    /// </summary>
    private static async Task BuildZipAsync(string zipPath, string graphJson,
        params (string EntryName, byte[] Content)[] extraEntries)
    {
        await using var fs = File.Create(zipPath);
        await using var zos = new ZipOutputStream(fs);
        zos.SetLevel(0);

        var graphBytes = Encoding.UTF8.GetBytes(graphJson);
        await zos.PutNextEntryAsync(
            new ZipEntry("Graph.json") { DateTime = DateTime.UtcNow, Size = graphBytes.Length },
            CancellationToken.None);
        zos.Write(graphBytes, 0, graphBytes.Length);
        zos.CloseEntry();

        foreach (var (name, content) in extraEntries)
        {
            await zos.PutNextEntryAsync(
                new ZipEntry(name) { DateTime = DateTime.UtcNow, Size = content.Length },
                CancellationToken.None);
            zos.Write(content, 0, content.Length);
            zos.CloseEntry();
        }

        zos.Finish();
    }

    #endregion
}