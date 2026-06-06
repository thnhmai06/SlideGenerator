/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipeRepositoryTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using System.Text;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Core;
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
///     Unit tests for <see cref="RecipeRepository" />, verifying CRUD operations and
///     export/import functionality using an in-memory SQLite database to avoid file-system side effects.
/// </summary>
public sealed class RecipeRepositoryTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly RecipeRepository _repo;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database. The anchor connection keeps the
    ///     in-memory database alive across all short-lived per-CRUD connections.
    /// </summary>
    public RecipeRepositoryTests()
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
    }

    /// <summary>Returns an input with an empty graph.</summary>
    private static RecipeInput Input(string name)
    {
        return new RecipeInput(name, new RecipeGraph([], []));
    }

    /// <summary>Returns an input whose graph contains the given nodes.</summary>
    private static RecipeInput Input(string name, params Node[] nodes)
    {
        return new RecipeInput(name, new RecipeGraph(nodes, []));
    }

    #region AddAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.AddAsync" /> inserts a row and returns metadata
    ///     with a positive ID.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidEntry_ReturnsMetadataWithPositiveId()
    {
        var metadata = await _repo.AddAsync(Input("My Recipe"), TestContext.Current.CancellationToken);

        metadata.Id.Should().BeGreaterThan(0);
        metadata.Name.Should().Be("My Recipe");
    }

    /// <summary>
    ///     Verifies that successive <see cref="RecipeRepository.AddAsync" /> calls assign
    ///     monotonically increasing IDs.
    /// </summary>
    [Fact]
    public async Task AddAsync_MultipleEntries_IdsAreIncreasing()
    {
        var m1 = await _repo.AddAsync(Input("A"), TestContext.Current.CancellationToken);
        var m2 = await _repo.AddAsync(Input("B"), TestContext.Current.CancellationToken);

        m2.Id.Should().BeGreaterThan(m1.Id);
    }

    /// <summary>
    ///     Verifies that a graph with a <see cref="WorkbookNode" /> round-trips through the database
    ///     with the node count preserved.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithWorkbookNode_GraphRoundTripsNodeCount()
    {
        var wbPath = Path.GetFullPath("dummy.xlsx");
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath));
        var metadata = await _repo.AddAsync(Input("WithNode", node), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Graph.Nodes.Should().HaveCount(1);
        entry.Graph.Nodes[0].Should().BeOfType<WorkbookNode>();
    }

    /// <summary>
    ///     Verifies that a graph with a <see cref="PresentationNode" /> round-trips through the
    ///     database preserving the path value.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithPresentationNode_GraphRoundTripsPresentationPath()
    {
        var presPath = Path.GetFullPath("template.pptx");
        var node = new PresentationNode("pres1", new Point(0, 0), new PresentationIdentifier(presPath));
        var metadata = await _repo.AddAsync(Input("WithPres", node), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var restored = entry.Graph.Nodes.OfType<PresentationNode>().Single();
        restored.Presentation.PresentationPath.Should().Be(presPath);
    }

    /// <summary>
    ///     Verifies that an empty graph round-trips without error.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithEmptyGraph_PersistsEmptyNodeList()
    {
        var metadata = await _repo.AddAsync(Input("Empty"), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Graph.Nodes.Should().BeEmpty();
        entry.Graph.Edges.Should().BeEmpty();
    }

    #endregion

    #region GetAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetAsync" /> throws
    ///     <see cref="InvalidOperationException" /> for a non-existent ID.
    /// </summary>
    [Fact]
    public async Task GetAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var act = async () => await _repo.GetAsync(9999, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetAsync" /> returns the correct name and ID
    ///     after insertion.
    /// </summary>
    [Fact]
    public async Task GetAsync_ExistingId_ReturnsCorrectNameAndId()
    {
        var metadata = await _repo.AddAsync(Input("TestName"), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Id.Should().Be(metadata.Id);
        entry.Name.Should().Be("TestName");
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetAsync" /> returns a <see cref="RecipeGraph" />
    ///     whose node count matches what was stored.
    /// </summary>
    [Fact]
    public async Task GetAsync_ExistingId_ReturnsGraphWithCorrectNodeCount()
    {
        var wbPath = Path.GetFullPath("a.xlsx");
        var n1 = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath));
        var n2 = new WorkbookNode("wb2", new Point(100, 0), new WorkbookIdentifier(wbPath));
        var metadata = await _repo.AddAsync(Input("TwoNodes", n1, n2), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Graph.Nodes.Should().HaveCount(2);
    }

    #endregion

    #region ListAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ListAsync" /> returns an empty list when the
    ///     database contains no entries.
    /// </summary>
    [Fact]
    public async Task ListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var list = await _repo.ListAsync(TestContext.Current.CancellationToken);

        list.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ListAsync" /> returns metadata for all inserted entries.
    /// </summary>
    [Fact]
    public async Task ListAsync_MultipleEntries_ReturnsAllMetadata()
    {
        await _repo.AddAsync(Input("Alpha"), TestContext.Current.CancellationToken);
        await _repo.AddAsync(Input("Beta"), TestContext.Current.CancellationToken);
        await _repo.AddAsync(Input("Gamma"), TestContext.Current.CancellationToken);

        var list = await _repo.ListAsync(TestContext.Current.CancellationToken);

        list.Should().HaveCount(3);
        list.Select(e => e.Name).Should().Contain(["Alpha", "Beta", "Gamma"]);
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ListAsync" /> preserves the graph node count
    ///     for each entry.
    /// </summary>
    [Fact]
    public async Task ListAsync_EntryWithNodes_ReturnsGraphWithNodeCount()
    {
        var wbPath = Path.GetFullPath("wb.xlsx");
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath));
        var metadata = await _repo.AddAsync(Input("WithNode", node), TestContext.Current.CancellationToken);

        var list = await _repo.ListAsync(TestContext.Current.CancellationToken);
        var match = list.OfType<RecipeEntry>().Single(e => e.Id == metadata.Id);

        match.Graph.Nodes.Should().HaveCount(1);
    }

    #endregion

    #region UpdateAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> throws
    ///     <see cref="InvalidOperationException" /> for a non-existent ID.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var act = async () => await _repo.UpdateAsync(9999, Input("NewName"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> overwrites the display name.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ExistingId_OverwritesName()
    {
        var metadata = await _repo.AddAsync(Input("Old"), TestContext.Current.CancellationToken);

        var updated = await _repo.UpdateAsync(metadata.Id, Input("New"), TestContext.Current.CancellationToken);

        updated.Name.Should().Be("New");
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> replaces the graph, so that
    ///     the new node count is reflected after a subsequent <see cref="RecipeRepository.GetAsync" />.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ExistingId_OverwritesGraph()
    {
        var metadata = await _repo.AddAsync(Input("Recipe"), TestContext.Current.CancellationToken);
        var wbPath = Path.GetFullPath("new.xlsx");
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath));

        await _repo.UpdateAsync(metadata.Id, Input("Recipe", node), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Graph.Nodes.Should().HaveCount(1);
    }

    #endregion

    #region DeleteAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.DeleteAsync" /> returns <see langword="false" />
    ///     for a non-existent ID without throwing.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var deleted = await _repo.DeleteAsync(9999, TestContext.Current.CancellationToken);

        deleted.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.DeleteAsync" /> removes the row so that a
    ///     subsequent <see cref="RecipeRepository.GetAsync" /> throws.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesRow()
    {
        var metadata = await _repo.AddAsync(Input("ToDelete"), TestContext.Current.CancellationToken);

        var deleted = await _repo.DeleteAsync(metadata.Id, TestContext.Current.CancellationToken);
        var getAfterDelete = async () => await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        deleted.Should().BeTrue();
        await getAfterDelete.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region ExportAsync / ImportAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ExportAsync" /> writes a ZIP file to the
    ///     specified path without throwing.
    /// </summary>
    [Fact]
    public async Task ExportAsync_ExistingRecipe_CreatesZipFile()
    {
        var metadata = await _repo.AddAsync(Input("Exported"), TestContext.Current.CancellationToken);
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");

        try
        {
            await _repo.ExportAsync(metadata.Id, outputPath, null, TestContext.Current.CancellationToken);

            File.Exists(outputPath).Should().BeTrue();
            new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ExportAsync" /> throws
    ///     <see cref="InvalidOperationException" /> for a non-existent recipe ID.
    /// </summary>
    [Fact]
    public async Task ExportAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");

        var act = async () => await _repo.ExportAsync(9999, outputPath, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> reads a ZIP exported by
    ///     <see cref="RecipeRepository.ExportAsync" />, inserts a new row with a distinct positive ID,
    ///     and preserves the recipe name.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ValidZipFile_InsertsNewRowWithMatchingName()
    {
        var exported = await _repo.AddAsync(Input("Original"), TestContext.Current.CancellationToken);
        // Names zip "Original" so ImportAsync derives that name from the filename.
        var zipPath = Path.Combine(Path.GetTempPath(), $"Original{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            imported.Id.Should().BeGreaterThan(0);
            imported.Id.Should().NotBe(exported.Id);
            entry.Name.Should().Be("Original");
            entry.Graph.Nodes.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when the archive is missing <c>Graph.json</c>.
    /// </summary>
    [Fact]
    public async Task ImportAsync_MissingGraphJson_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            // Empty zip (no entries at all)
            await using (var fs = File.Create(zipPath))
            await using (var zos = new ZipOutputStream(fs))
            {
                zos.Finish();
            }

            var act = async () => await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Graph.json*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ExportAsync" /> serializes zip-relative paths
    ///     into <c>Graph.json</c>, and <see cref="RecipeRepository.ImportAsync" /> rewrites them to
    ///     absolute extracted paths so that file nodes in the imported graph point to the extracted
    ///     files on disk.
    /// </summary>
    [Fact]
    public async Task ImportAsync_GraphWithFilePaths_RoundTripsPathsToExtractedDirectories()
    {
        var wbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        var pptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pptx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await File.WriteAllBytesAsync(wbPath, [], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(pptPath, [], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
                [
                    new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath)),
                    new PresentationNode("ppt1", new Point(0, 0), new PresentationIdentifier(pptPath))
                ],
                []);
            var exported = await _repo.AddAsync(new RecipeInput("OrigName", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWb = entry.Graph.Nodes.OfType<WorkbookNode>().Single();
            var importedPpt = entry.Graph.Nodes.OfType<PresentationNode>().Single();

            importedWb.Workbook.BookPath.Should().StartWith(workbooksDir);
            File.Exists(importedWb.Workbook.BookPath).Should().BeTrue();
            importedPpt.Presentation.PresentationPath.Should().StartWith(presentationsDir);
            File.Exists(importedPpt.Presentation.PresentationPath).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(wbPath)) File.Delete(wbPath);
            if (File.Exists(pptPath)) File.Delete(pptPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when <c>Graph.json</c> contains malformed JSON.
    /// </summary>
    [Fact]
    public async Task ImportAsync_InvalidGraphJson_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await using (var fs = File.Create(zipPath))
            await using (var zos = new ZipOutputStream(fs))
            {
                var bytes = Encoding.UTF8.GetBytes("not valid json {{{");
                var entry = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
                    { Size = bytes.Length };
                await zos.PutNextEntryAsync(entry, TestContext.Current.CancellationToken);
                zos.Write(bytes, 0, bytes.Length);
                zos.CloseEntry();
                zos.Finish();
            }

            var act = async () => await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Graph.json*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when a zip entry under <c>Workbooks/</c> uses a
    ///     path-traversal name (Zip Slip attack).
    /// </summary>
    [Fact]
    public async Task ImportAsync_ZipSlipWorkbookEntry_ThrowsInvalidDataException()
    {
        var wbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wbPath, [], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
                [new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath))], []);
            var exported = await _repo.AddAsync(new RecipeInput("SlipTest", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            // Re-open the zip and inject a malicious entry alongside the legitimate ones.
            var maliciousZipPath = Path.Combine(Path.GetTempPath(),
                $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
            try
            {
                // Build malicious zip (streams fully closed before ImportAsync reads it).
                await using (var inFs = File.OpenRead(zipPath))
                using (var inZip = new ZipFile(inFs))
                await using (var outFs = File.Create(maliciousZipPath))
                await using (var outZos = new ZipOutputStream(outFs))
                {
                    foreach (ZipEntry e in inZip)
                    {
                        if (!e.IsFile) continue;
                        var copy = new ZipEntry(e.Name) { Size = e.Size };
                        await outZos.PutNextEntryAsync(copy, TestContext.Current.CancellationToken);
                        StreamUtils.Copy(inZip.GetInputStream(e), outZos, new byte[4096]);
                        outZos.CloseEntry();
                    }

                    // Malicious entry: path traversal out of Workbooks/
                    var payload = new byte[] { 0x00 };
                    var malEntry = new ZipEntry("Workbooks/../../escape.xlsx")
                        { Size = payload.Length };
                    await outZos.PutNextEntryAsync(malEntry, TestContext.Current.CancellationToken);
                    outZos.Write(payload, 0, payload.Length);
                    outZos.CloseEntry();
                    outZos.Finish();
                }

                var act = async () => await _repo.ImportAsync(maliciousZipPath, null,
                    (workbooksDir, presentationsDir), TestContext.Current.CancellationToken);

                await act.Should().ThrowAsync<InvalidDataException>()
                    .WithMessage("*escapes the target directory*");
            }
            finally
            {
                if (File.Exists(maliciousZipPath)) File.Delete(maliciousZipPath);
            }
        }
        finally
        {
            if (File.Exists(wbPath)) File.Delete(wbPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> does not extract a file whose
    ///     extension is not on the allowed list (e.g. <c>.exe</c> inside <c>Workbooks/</c>).
    /// </summary>
    [Fact]
    public async Task ImportAsync_DisallowedExtensionInWorkbooks_FileNotExtracted()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            // Build a zip with a valid Graph.json (empty graph) + a disallowed entry.
            await using (var fs = File.Create(zipPath))
            await using (var zos = new ZipOutputStream(fs))
            {
                var graphBytes = Encoding.UTF8.GetBytes("{\"nodes\":[],\"edges\":[]}");
                var graphEntry = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
                    { Size = graphBytes.Length };
                await zos.PutNextEntryAsync(graphEntry, TestContext.Current.CancellationToken);
                zos.Write(graphBytes, 0, graphBytes.Length);
                zos.CloseEntry();

                var payload = new byte[] { 0xFF, 0xD8 };
                var badEntry = new ZipEntry("Workbooks/payload.exe")
                    { Size = payload.Length };
                await zos.PutNextEntryAsync(badEntry, TestContext.Current.CancellationToken);
                zos.Write(payload, 0, payload.Length);
                zos.CloseEntry();
                zos.Finish();
            }

            await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            Directory.Exists(workbooksDir).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> does not overwrite an existing file
    ///     in the target folder, but instead extracts to a deduplicated path (with <c>_N</c> suffix),
    ///     and that the imported graph node points to the deduplicated path.
    /// </summary>
    [Fact]
    public async Task ImportAsync_WorkbookFileAlreadyExistsInTargetFolder_DeduplicatesExtractedFile()
    {
        var wbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wbPath, [0x01], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
                [new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath))], []);
            var exported = await _repo.AddAsync(new RecipeInput("DupImport", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            // Pre-create a conflicting file in the target folder.
            Directory.CreateDirectory(workbooksDir);
            var conflictingPath = Path.Combine(workbooksDir, Path.GetFileName(wbPath));
            await File.WriteAllBytesAsync(conflictingPath, [0xFF], TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWb = entry.Graph.Nodes.OfType<WorkbookNode>().Single();

            // Pre-existing file must not be overwritten.
            (await File.ReadAllBytesAsync(conflictingPath, TestContext.Current.CancellationToken))[0]
                .Should().Be(0xFF);
            // Graph node must point to a deduplicated path, not the conflicting one.
            importedWb.Workbook.BookPath.Should().NotBe(conflictingPath);
            importedWb.Workbook.BookPath.Should().StartWith(workbooksDir);
            // The extracted file must exist with the content from the zip (0x01).
            File.Exists(importedWb.Workbook.BookPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(importedWb.Workbook.BookPath,
                TestContext.Current.CancellationToken))[0].Should().Be(0x01);
        }
        finally
        {
            if (File.Exists(wbPath)) File.Delete(wbPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ExportAsync" /> deduplicates workbook files
    ///     sharing the same stem (from different directories) by appending <c>_N</c> suffixes, and
    ///     that <see cref="RecipeRepository.ImportAsync" /> restores both files under the target folder.
    /// </summary>
    [Fact]
    public async Task ExportAsync_DuplicateStemWorkbooks_BothFilesExtractedAfterImport()
    {
        var dir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var dir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        var wb1 = Path.Combine(dir1, "report.xlsx");
        var wb2 = Path.Combine(dir2, "report.xlsx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wb1, [0x01], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(wb2, [0x02], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
                [
                    new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wb1)),
                    new WorkbookNode("wb2", new Point(1, 0), new WorkbookIdentifier(wb2))
                ],
                []);
            var exported = await _repo.AddAsync(new RecipeInput("DupStem", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWbs = entry.Graph.Nodes.OfType<WorkbookNode>().ToList();
            importedWbs.Should().HaveCount(2);
            importedWbs.Should().AllSatisfy(n => File.Exists(n.Workbook.BookPath).Should().BeTrue());
            // Both paths must be distinct (deduplication gave them different names).
            importedWbs.Select(n => n.Workbook.BookPath).Distinct().Should().HaveCount(2);
        }
        finally
        {
            if (File.Exists(wb1)) File.Delete(wb1);
            if (File.Exists(wb2)) File.Delete(wb2);
            if (Directory.Exists(dir1)) Directory.Delete(dir1, true);
            if (Directory.Exists(dir2)) Directory.Delete(dir2, true);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that a password-protected export can be re-imported with the same password,
    ///     producing a new recipe row with the correct name.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithPassword_CanBeImportedWithSamePassword()
    {
        const string password = "s3cr3t!";
        var exported = await _repo.AddAsync(Input("Encrypted"), TestContext.Current.CancellationToken);
        var zipPath = Path.Combine(Path.GetTempPath(),
            $"Encrypted{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await _repo.ExportAsync(exported.Id, zipPath, password, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, password, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            imported.Id.Should().NotBe(exported.Id);
            entry.Name.Should().Be("Encrypted");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that importing a password-protected archive without a password (or with the wrong
    ///     password) throws an exception, preventing unauthorized extraction.
    /// </summary>
    [Fact]
    public async Task ImportAsync_WrongPassword_ThrowsException()
    {
        const string correctPassword = "correct";
        var exported = await _repo.AddAsync(Input("EncryptedWrong"), TestContext.Current.CancellationToken);
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await _repo.ExportAsync(exported.Id, zipPath, correctPassword,
                TestContext.Current.CancellationToken);

            var act = async () => await _repo.ImportAsync(zipPath, "wrongpassword",
                (workbooksDir, presentationsDir), TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<Exception>();
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> deduplicates a presentation file
    ///     when a file with the same name already exists in the target presentations folder.
    /// </summary>
    [Fact]
    public async Task ImportAsync_PresentationFileAlreadyExistsInTargetFolder_DeduplicatesExtractedFile()
    {
        var pptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pptx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(pptPath, [0x01], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
                [new PresentationNode("ppt1", new Point(0, 0), new PresentationIdentifier(pptPath))], []);
            var exported = await _repo.AddAsync(new RecipeInput("PptDedup", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            Directory.CreateDirectory(presentationsDir);
            var conflictingPath = Path.Combine(presentationsDir, Path.GetFileName(pptPath));
            await File.WriteAllBytesAsync(conflictingPath, [0xFF], TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedPpt = entry.Graph.Nodes.OfType<PresentationNode>().Single();

            (await File.ReadAllBytesAsync(conflictingPath, TestContext.Current.CancellationToken))[0]
                .Should().Be(0xFF);
            importedPpt.Presentation.PresentationPath.Should().NotBe(conflictingPath);
            importedPpt.Presentation.PresentationPath.Should().StartWith(presentationsDir);
            File.Exists(importedPpt.Presentation.PresentationPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(importedPpt.Presentation.PresentationPath,
                TestContext.Current.CancellationToken))[0].Should().Be(0x01);
        }
        finally
        {
            if (File.Exists(pptPath)) File.Delete(pptPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that when both <c>report.xlsx</c> and <c>report_1.xlsx</c> already exist in the
    ///     target folder, <see cref="RecipeRepository.ImportAsync" /> assigns <c>report_2.xlsx</c>
    ///     (i.e., deduplication skips over all occupied slots).
    /// </summary>
    [Fact]
    public async Task ImportAsync_MultipleConflictsAlreadyExist_DeduplicatesIncrementally()
    {
        var wbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wbPath, [0x01], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
                [new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath))], []);
            var exported = await _repo.AddAsync(new RecipeInput("MultiConflict", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            // Pre-occupy both "name.xlsx" and "name_1.xlsx".
            Directory.CreateDirectory(workbooksDir);
            var stem = Path.GetFileNameWithoutExtension(wbPath);
            var ext = Path.GetExtension(wbPath);
            await File.WriteAllBytesAsync(Path.Combine(workbooksDir, Path.GetFileName(wbPath)), [0xAA],
                TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(workbooksDir, $"{stem}_1{ext}"), [0xBB],
                TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWb = entry.Graph.Nodes.OfType<WorkbookNode>().Single();
            var expectedName = $"{stem}_2{ext}";

            Path.GetFileName(importedWb.Workbook.BookPath).Should().Be(expectedName,
                "first two slots are occupied so _2 must be chosen");
            File.Exists(importedWb.Workbook.BookPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(importedWb.Workbook.BookPath,
                TestContext.Current.CancellationToken))[0].Should().Be(0x01);
        }
        finally
        {
            if (File.Exists(wbPath)) File.Delete(wbPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that when the graph contains two nodes whose files export with the same deduped
    ///     names (e.g. <c>report.xlsx</c> and <c>report_1.xlsx</c>) and the target folder already
    ///     contains <c>report.xlsx</c>, both imported files land at free slots without collision.
    /// </summary>
    [Fact]
    public async Task ImportAsync_DuplicateStemNodesWithPreExistingConflict_AllFilesExtractedDistinct()
    {
        var dir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var dir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        var wb1 = Path.Combine(dir1, "data.xlsx");
        var wb2 = Path.Combine(dir2, "data.xlsx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wb1, [0x01], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(wb2, [0x02], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
            [
                new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wb1)),
                new WorkbookNode("wb2", new Point(1, 0), new WorkbookIdentifier(wb2))
            ], []);
            var exported = await _repo.AddAsync(new RecipeInput("DupConflict", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            // Pre-occupy "data.xlsx" in target folder.
            Directory.CreateDirectory(workbooksDir);
            await File.WriteAllBytesAsync(Path.Combine(workbooksDir, "data.xlsx"), [0xFF],
                TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWbs = entry.Graph.Nodes.OfType<WorkbookNode>().ToList();
            importedWbs.Should().HaveCount(2);
            importedWbs.Should().AllSatisfy(n => File.Exists(n.Workbook.BookPath).Should().BeTrue());
            // All three paths (pre-existing + two imported) must be distinct.
            importedWbs.Select(n => n.Workbook.BookPath)
                .Concat([Path.Combine(workbooksDir, "data.xlsx")])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Should().HaveCount(3);
            // Pre-existing file must be untouched.
            (await File.ReadAllBytesAsync(Path.Combine(workbooksDir, "data.xlsx"),
                TestContext.Current.CancellationToken))[0].Should().Be(0xFF);
        }
        finally
        {
            if (File.Exists(wb1)) File.Delete(wb1);
            if (File.Exists(wb2)) File.Delete(wb2);
            if (Directory.Exists(dir1)) Directory.Delete(dir1, true);
            if (Directory.Exists(dir2)) Directory.Delete(dir2, true);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that a zip entry whose path does not start with a known folder prefix
    ///     (<c>Workbooks/</c> or <c>Presentations/</c>) is silently ignored and does not prevent
    ///     a successful import.
    /// </summary>
    [Fact]
    public async Task ImportAsync_EntryInUnknownFolderPrefix_IsIgnoredAndImportSucceeds()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using (var fs = File.Create(zipPath))
            using (var zos = new ZipOutputStream(fs))
            {
                var graphBytes = Encoding.UTF8.GetBytes("{\"nodes\":[],\"edges\":[]}");
                var graphEntry = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
                    { Size = graphBytes.Length };
                zos.PutNextEntry(graphEntry);
                zos.Write(graphBytes, 0, graphBytes.Length);
                zos.CloseEntry();

                var payload = new byte[] { 0x42 };
                var unknownEntry = new ZipEntry("Secret/evil.xlsx")
                    { Size = payload.Length };
                zos.PutNextEntry(unknownEntry);
                zos.Write(payload, 0, payload.Length);
                zos.CloseEntry();
                zos.Finish();
            }

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            imported.Id.Should().BeGreaterThan(0);
            Directory.Exists(workbooksDir).Should().BeFalse();
            Directory.Exists(presentationsDir).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when the archive contains more entries than
    ///     <see cref="RecipePackageRules.MaxEntryCount" /> allows.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ExceedsMaxEntryCount_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using (var fs = File.Create(zipPath))
            using (var zos = new ZipOutputStream(fs))
            {
                var tiny = new byte[] { 0x00 };
                for (var i = 0; i <= RecipePackageRules.MaxEntryCount; i++)
                {
                    var e = new ZipEntry($"entry_{i}.bin") { Size = tiny.Length };
                    zos.PutNextEntry(e);
                    zos.Write(tiny, 0, tiny.Length);
                    zos.CloseEntry();
                }

                zos.Finish();
            }

            var act = async () => await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*entry count*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when <c>Graph.json</c>'s uncompressed size exceeds
    ///     <see cref="RecipePackageRules.MaxGraphUncompressedBytes" />.
    /// </summary>
    [Fact]
    public async Task ImportAsync_GraphJsonExceedsUncompressedSizeLimit_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var oversized = new byte[RecipePackageRules.MaxGraphUncompressedBytes + 1];
            using (var fs = File.Create(zipPath))
            using (var zos = new ZipOutputStream(fs))
            {
                zos.SetLevel(9);
                var e = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
                    { Size = oversized.Length };
                zos.PutNextEntry(e);
                zos.Write(oversized, 0, oversized.Length);
                zos.CloseEntry();
                zos.Finish();
            }

            var act = async () => await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage($"*{RecipePackageRules.Data.RecipeFileName}*uncompressed size*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when <c>Graph.json</c>'s compression ratio exceeds
    ///     <see cref="RecipePackageRules.MaxEntryCompressionRatio" />.
    /// </summary>
    [Fact]
    public async Task ImportAsync_GraphJsonExceedsCompressionRatio_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            // 100 KB of zeros compresses to a few hundred bytes — ratio far exceeds 100.
            var compressible = new byte[100 * 1024];
            using (var fs = File.Create(zipPath))
            using (var zos = new ZipOutputStream(fs))
            {
                zos.SetLevel(9);
                var e = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
                    { Size = compressible.Length };
                zos.PutNextEntry(e);
                zos.Write(compressible, 0, compressible.Length);
                zos.CloseEntry();
                zos.Finish();
            }

            var act = async () => await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage($"*{RecipePackageRules.Data.RecipeFileName}*compression ratio*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> throws
    ///     <see cref="InvalidDataException" /> when a data file entry's compression ratio exceeds
    ///     <see cref="RecipePackageRules.MaxEntryCompressionRatio" />.
    /// </summary>
    [Fact]
    public async Task ImportAsync_DataEntryExceedsCompressionRatio_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var graphBytes = Encoding.UTF8.GetBytes("{\"nodes\":[],\"edges\":[]}");
            var compressible = new byte[100 * 1024];

            using (var fs = File.Create(zipPath))
            using (var zos = new ZipOutputStream(fs))
            {
                zos.SetLevel(9);

                var graphEntry = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
                    { Size = graphBytes.Length };
                zos.PutNextEntry(graphEntry);
                zos.Write(graphBytes, 0, graphBytes.Length);
                zos.CloseEntry();

                // Data entry with extreme compression ratio (100 KB of zeros → far exceeds 100x).
                var dataEntry = new ZipEntry("Workbooks/bomb.xlsx")
                    { Size = compressible.Length };
                zos.PutNextEntry(dataEntry);
                zos.Write(compressible, 0, compressible.Length);
                zos.CloseEntry();

                zos.Finish();
            }

            var act = async () => await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*compression ratio*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    /// <summary>
    ///     Verifies that when a recipe with real file nodes is exported with a password, the data
    ///     files are AES-256 encrypted and can be fully round-tripped back with the same password.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithPasswordAndFileNodes_FilesEncryptedAndExtractedCorrectly()
    {
        const string password = "p@ssw0rd!";
        var wbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        var pptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pptx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wbPath, [0x01], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(pptPath, [0x02], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
            [
                new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath)),
                new PresentationNode("ppt1", new Point(1, 0), new PresentationIdentifier(pptPath))
            ], []);
            var exported = await _repo.AddAsync(new RecipeInput("EncryptedWithFiles", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, password, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, password, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWb = entry.Graph.Nodes.OfType<WorkbookNode>().Single();
            var importedPpt = entry.Graph.Nodes.OfType<PresentationNode>().Single();

            File.Exists(importedWb.Workbook.BookPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(importedWb.Workbook.BookPath,
                TestContext.Current.CancellationToken))[0].Should().Be(0x01);
            File.Exists(importedPpt.Presentation.PresentationPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(importedPpt.Presentation.PresentationPath,
                TestContext.Current.CancellationToken))[0].Should().Be(0x02);
        }
        finally
        {
            if (File.Exists(wbPath)) File.Delete(wbPath);
            if (File.Exists(pptPath)) File.Delete(pptPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    /// <summary>
    ///     Verifies that three workbooks where the third file's stem collides with a dedup-generated
    ///     name (<c>data_1.xlsx</c>, produced for the second file) are all correctly deduplicated
    ///     during export, and all three files are extracted after import.
    ///     This exercises the fallback loop in <c>Export_ResolveFileName</c> where the original
    ///     name for a new stem is already occupied by a dedup suffix of another stem.
    /// </summary>
    [Fact]
    public async Task ExportAsync_ThreeFilesWithStemCollision_AllDeduplicatedAndExtracted()
    {
        var dirA = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var dirB = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var dirC = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);
        Directory.CreateDirectory(dirC);
        // dirA/data.xlsx → "data.xlsx", dirB/data.xlsx → "data_1.xlsx",
        // dirC/data_1.xlsx → "data_1.xlsx" already taken → fallback to "data_1_1.xlsx"
        var wbA = Path.Combine(dirA, "data.xlsx");
        var wbB = Path.Combine(dirB, "data.xlsx");
        var wbC = Path.Combine(dirC, "data_1.xlsx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wbA, [0x01], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(wbB, [0x02], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(wbC, [0x03], TestContext.Current.CancellationToken);

            var graph = new RecipeGraph(
            [
                new WorkbookNode("a", new Point(0, 0), new WorkbookIdentifier(wbA)),
                new WorkbookNode("b", new Point(1, 0), new WorkbookIdentifier(wbB)),
                new WorkbookNode("c", new Point(2, 0), new WorkbookIdentifier(wbC))
            ], []);
            var exported = await _repo.AddAsync(new RecipeInput("StemCollision", graph),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, null, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWbs = entry.Graph.Nodes.OfType<WorkbookNode>().ToList();
            importedWbs.Should().HaveCount(3);
            importedWbs.Should().AllSatisfy(n => File.Exists(n.Workbook.BookPath).Should().BeTrue());
            importedWbs.Select(n => n.Workbook.BookPath)
                .Distinct(StringComparer.OrdinalIgnoreCase).Should().HaveCount(3);
            // Contents must match original files (order-agnostic check on the set of byte values).
            var extractedFirstBytes = importedWbs
                .Select(n => File.ReadAllBytes(n.Workbook.BookPath)[0])
                .Order().ToList();
            extractedFirstBytes.Should().Equal(0x01, 0x02, 0x03);
        }
        finally
        {
            foreach (var d in new[] { dirA, dirB, dirC })
                if (Directory.Exists(d))
                    Directory.Delete(d, true);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    #endregion
}