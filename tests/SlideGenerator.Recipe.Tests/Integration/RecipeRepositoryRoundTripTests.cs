/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipeRepositoryRoundTripTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Settings.Database;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Integration;

/// <summary>
///     Integration tests for full export/import round-trips of
///     <see cref="RecipePackageService" />
///     using the real <c>Data.xlsx</c> + <c>Template.pptx</c> fixtures bundled into the archive.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RecipeRepositoryRoundTripTests : IDisposable
{
    private static readonly string WorkbookPath =
        Path.GetFullPath(Path.Combine("fixtures", "data", "Data.xlsx"));

    private static readonly string PresentationPath =
        Path.GetFullPath(Path.Combine("fixtures", "data", "Template.pptx"));

    private readonly SqliteConnection _anchor;
    private readonly IRecipePackageService _packageService;
    private readonly IRecipeRepository _repository;
    private readonly string _tempDir;

    /// <summary>Sets up an in-memory shared-cache database and a scratch directory for archive I/O.</summary>
    public RecipeRepositoryRoundTripTests()
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

        _repository = new SqliteRecipeRepository(builder);
        _packageService = new RecipePackageService(_repository);

        _tempDir = Path.Combine(Path.GetTempPath(), $"recipe-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    /// <summary>
    ///     Exporting a recipe that references the real <c>Data.xlsx</c> workbook and <c>Template.pptx</c>
    ///     presentation must bundle those files into the archive. Re-importing the archive must restore the
    ///     files under the target directories and insert a new recipe row whose paths point at the restored
    ///     copies.
    /// </summary>
    [Fact(DisplayName =
        "INTEGRATION: export+import round-trip bundles and restores workbook+presentation")]
    public async Task Export_WithWorkbooksAndPresentations_BundlesAllFiles_AndImportRestores()
    {
        var recipe = new Models.Recipe(
        [
            new Mapping(
                [new WorksheetSource(new WorkbookIdentifier(WorkbookPath), new WorksheetIdentifier("Data"))],
                new PresentationSource(new PresentationIdentifier(PresentationPath), new SlideIdentifier(1)),
                [],
                [])
        ]);
        var ct = TestContext.Current.CancellationToken;
        var original = await _repository.AddAsync(new RecipeInput("Round-trip", recipe), ct);

        var archivePath = Path.Combine(_tempDir, "export.recipe");
        await _packageService.ExportAsync(original.Id, archivePath, ct);

        File.Exists(archivePath).Should().BeTrue("export must produce an archive file");

        var workbooksDir = Path.Combine(_tempDir, "Workbooks");
        var presentationsDir = Path.Combine(_tempDir, "Presentations");
        var imported = await _packageService.ImportAsync(archivePath, (workbooksDir, presentationsDir), ct);

        imported.Id.Should().NotBe(original.Id, "import must insert a new recipe row");

        var importedEntry = await _repository.GetAsync(imported.Id, ct);
        importedEntry.Recipe.Mappings.Should().ContainSingle();
        var mapping = importedEntry.Recipe.Mappings[0];

        var restoredWorkbookPath = mapping.Sources.Single().Workbook.BookPath;
        var restoredPresentationPath = mapping.Template.Presentation.PresentationPath;

        File.Exists(restoredWorkbookPath).Should().BeTrue("workbook must be restored under the target directory");
        File.Exists(restoredPresentationPath).Should()
            .BeTrue("presentation must be restored under the target directory");
        Path.GetDirectoryName(restoredWorkbookPath).Should().Be(Path.GetFullPath(workbooksDir));
        Path.GetDirectoryName(restoredPresentationPath).Should().Be(Path.GetFullPath(presentationsDir));

        new FileInfo(restoredWorkbookPath).Length.Should().Be(new FileInfo(WorkbookPath).Length);
        new FileInfo(restoredPresentationPath).Length.Should().Be(new FileInfo(PresentationPath).Length);
    }
}
