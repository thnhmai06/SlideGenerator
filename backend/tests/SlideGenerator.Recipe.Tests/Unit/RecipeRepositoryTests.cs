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

using System.IO.Compression;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Mappings;
using SlideGenerator.Settings.Database;
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
        DatabaseMigrator.Migrate(builder.ConnectionString);
        _repo = new RecipeRepository(builder);
    }

    /// <inheritdoc />
    public void Dispose() => _anchor.Dispose();

    /// <summary>Returns an input with an empty recipe (no mappings).</summary>
    private static RecipeInput Input(string name) => new(name, new Mappings.Recipe([]));

    /// <summary>Returns an input whose recipe contains one mapping with one worksheet source.</summary>
    private static RecipeInput InputWithMapping(string name, string wbPath, string pptPath) => new(name,
        RecipeWithMapping(wbPath, pptPath));

    /// <summary>Returns an input whose recipe contains one mapping referencing several workbooks.</summary>
    private static RecipeInput InputWithWorkbooks(string name, params string[] wbPaths)
    {
        var pptPath = Path.GetFullPath("template.pptx");
        return new RecipeInput(name, new Mappings.Recipe([
            new Mapping(
                [
                    .. wbPaths.Select(p =>
                        new WorksheetSource(new WorkbookIdentifier(p), new WorksheetIdentifier("Sheet1")))
                ],
                new PresentationSource(new PresentationIdentifier(pptPath), new SlideIdentifier(1)), [], [])
        ]));
    }

    private static Mappings.Recipe RecipeWithMapping(string wbPath, string pptPath) => new([
        new Mapping(
            [new WorksheetSource(new WorkbookIdentifier(wbPath), new WorksheetIdentifier("Sheet1"))],
            new PresentationSource(new PresentationIdentifier(pptPath), new SlideIdentifier(1)), [], [])
    ]);

    #region AddAsync / GetAsync / ListAsync / UpdateAsync / DeleteAsync

    [Fact]
    public async Task AddAsync_ValidEntry_ReturnsMetadataWithPositiveId()
    {
        var metadata = await _repo.AddAsync(Input("My Recipe"), TestContext.Current.CancellationToken);

        metadata.Id.Should().BeGreaterThan(0);
        metadata.Name.Should().Be("My Recipe");
    }

    [Fact]
    public async Task AddAsync_MultipleEntries_IdsAreIncreasing()
    {
        var m1 = await _repo.AddAsync(Input("A"), TestContext.Current.CancellationToken);
        var m2 = await _repo.AddAsync(Input("B"), TestContext.Current.CancellationToken);

        m2.Id.Should().BeGreaterThan(m1.Id);
    }

    [Fact]
    public async Task AddAsync_WithMapping_GraphRoundTripsMappingCount()
    {
        var wbPath = Path.GetFullPath("dummy.xlsx");
        var pptPath = Path.GetFullPath("dummy.pptx");
        var metadata = await _repo.AddAsync(InputWithMapping("WithMapping", wbPath, pptPath),
            TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Recipe.Mappings.Should().HaveCount(1);
        entry.Recipe.Mappings[0].Sources.Should().HaveCount(1);
        entry.Recipe.Mappings[0].Template.Presentation.PresentationPath.Should().Be(pptPath);
    }

    [Fact]
    public async Task AddAsync_WithEmptyRecipe_PersistsEmptyMappingList()
    {
        var metadata = await _repo.AddAsync(Input("Empty"), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Recipe.Mappings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var act = async () => await _repo.GetAsync(9999, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsCorrectNameAndId()
    {
        var metadata = await _repo.AddAsync(Input("TestName"), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Id.Should().Be(metadata.Id);
        entry.Name.Should().Be("TestName");
    }

    [Fact]
    public async Task ListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var list = await _repo.ListAsync(TestContext.Current.CancellationToken);

        list.Should().BeEmpty();
    }

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

    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var act = async () => await _repo.UpdateAsync(9999, Input("NewName"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_OverwritesNameAndMappings()
    {
        var metadata = await _repo.AddAsync(Input("Old"), TestContext.Current.CancellationToken);
        var wbPath = Path.GetFullPath("new.xlsx");
        var pptPath = Path.GetFullPath("new.pptx");

        var updated = await _repo.UpdateAsync(metadata.Id, InputWithMapping("New", wbPath, pptPath),
            TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        updated.Name.Should().Be("New");
        entry.Recipe.Mappings.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var deleted = await _repo.DeleteAsync(9999, TestContext.Current.CancellationToken);

        deleted.Should().BeFalse();
    }

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

    [Fact]
    public async Task ExportAsync_ExistingRecipe_CreatesZipFile()
    {
        var metadata = await _repo.AddAsync(Input("Exported"), TestContext.Current.CancellationToken);
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");

        try
        {
            await _repo.ExportAsync(metadata.Id, outputPath, TestContext.Current.CancellationToken);

            File.Exists(outputPath).Should().BeTrue();
            new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");

        var act = async () => await _repo.ExportAsync(9999, outputPath, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ImportAsync_ValidZipFile_InsertsNewRowWithMatchingName()
    {
        var exported = await _repo.AddAsync(Input("Original"), TestContext.Current.CancellationToken);
        var zipPath = Path.Combine(Path.GetTempPath(), $"Original{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await _repo.ExportAsync(exported.Id, zipPath, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            imported.Id.Should().BeGreaterThan(0);
            imported.Id.Should().NotBe(exported.Id);
            entry.Name.Should().Be("Original");
            entry.Recipe.Mappings.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    [Fact]
    public async Task ImportAsync_MissingGraphJson_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using (var fs = File.Create(zipPath))
            using (new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // empty archive
            }

            var act = async () => await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Recipe.json*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

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

            var exported = await _repo.AddAsync(InputWithMapping("OrigName", wbPath, pptPath),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var mapping = entry.Recipe.Mappings.Single();
            var importedWb = mapping.Sources.Single().Workbook.BookPath;
            var importedPpt = mapping.Template.Presentation.PresentationPath;

            importedWb.Should().StartWith(workbooksDir);
            File.Exists(importedWb).Should().BeTrue();
            importedPpt.Should().StartWith(presentationsDir);
            File.Exists(importedPpt).Should().BeTrue();
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

    [Fact]
    public async Task ImportAsync_InvalidGraphJson_ThrowsInvalidDataException()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var bytes = "not valid json {{{"u8.ToArray();
                var entry = archive.CreateEntry(RecipePackageRules.Data.RecipeFileName);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
            }

            var act = async () => await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Recipe.json*");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task ImportAsync_DisallowedExtensionInWorkbooks_FileNotExtracted()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var graphBytes = "{\"mappings\":[]}"u8.ToArray();
                var graphEntry = archive.CreateEntry(RecipePackageRules.Data.RecipeFileName);
                await using (var graphStream = graphEntry.Open())
                    await graphStream.WriteAsync(graphBytes, TestContext.Current.CancellationToken);

                var payload = new byte[] { 0xFF, 0xD8 };
                var badEntry = archive.CreateEntry("Workbooks/payload.exe");
                await using (var badStream = badEntry.Open())
                    await badStream.WriteAsync(payload, TestContext.Current.CancellationToken);
            }

            await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            Directory.Exists(workbooksDir).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
        }
    }

    [Fact]
    public async Task ImportAsync_WorkbookFileAlreadyExistsInTargetFolder_DeduplicatesExtractedFile()
    {
        var wbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        var pptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pptx");
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await File.WriteAllBytesAsync(wbPath, [0x01], TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(pptPath, [], TestContext.Current.CancellationToken);

            var exported = await _repo.AddAsync(InputWithMapping("DupImport", wbPath, pptPath),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, TestContext.Current.CancellationToken);

            // Pre-create a conflicting file in the target folder.
            Directory.CreateDirectory(workbooksDir);
            var conflictingPath = Path.Combine(workbooksDir, Path.GetFileName(wbPath));
            await File.WriteAllBytesAsync(conflictingPath, [0xFF], TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWb = entry.Recipe.Mappings.Single().Sources.Single().Workbook.BookPath;

            (await File.ReadAllBytesAsync(conflictingPath, TestContext.Current.CancellationToken))[0]
                .Should().Be(0xFF);
            importedWb.Should().NotBe(conflictingPath);
            importedWb.Should().StartWith(workbooksDir);
            File.Exists(importedWb).Should().BeTrue();
            (await File.ReadAllBytesAsync(importedWb, TestContext.Current.CancellationToken))[0].Should().Be(0x01);
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

            var exported = await _repo.AddAsync(InputWithWorkbooks("DupStem", wb1, wb2),
                TestContext.Current.CancellationToken);
            await _repo.ExportAsync(exported.Id, zipPath, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);
            var entry = await _repo.GetAsync(imported.Id, TestContext.Current.CancellationToken);

            var importedWbs = entry.Recipe.Mappings.Single().Sources.Select(s => s.Workbook.BookPath).ToList();
            importedWbs.Should().HaveCount(2);
            importedWbs.Should().AllSatisfy(p => File.Exists(p).Should().BeTrue());
            importedWbs.Distinct().Should().HaveCount(2);
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

    [Fact]
    public async Task ImportAsync_EntryInUnknownFolderPrefix_IsIgnoredAndImportSucceeds()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{RecipePackageRules.PackageExtension}");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var graphBytes = "{\"mappings\":[]}"u8.ToArray();
                var graphEntry = archive.CreateEntry(RecipePackageRules.Data.RecipeFileName);
                await using (var graphStream = graphEntry.Open())
                    await graphStream.WriteAsync(graphBytes, TestContext.Current.CancellationToken);

                var payload = new byte[] { 0x42 };
                var unknownEntry = archive.CreateEntry("Secret/evil.xlsx");
                await using (var unknownStream = unknownEntry.Open())
                    await unknownStream.WriteAsync(payload, TestContext.Current.CancellationToken);
            }

            var imported = await _repo.ImportAsync(zipPath, (workbooksDir, presentationsDir),
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

    #endregion
}
