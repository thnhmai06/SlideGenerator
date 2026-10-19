/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipePackageImageTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Settings.Database;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Tests covering how <see cref="RecipePackageService" /> bundles and restores
///     <see cref="ImageInstruction.FallbackImagePath" /> values under the <c>Images/</c> package folder.
/// </summary>
public sealed class RecipePackageImageTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly IRecipePackageService _packageService;
    private readonly IRecipeRepository _repository;
    private readonly string _root;

    /// <summary>Sets up an in-memory shared-cache database and a scratch directory for archive I/O.</summary>
    public RecipePackageImageTests()
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

        _root = Path.Combine(Path.GetTempPath(), $"recipe-images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string PathIn(string fileName)
    {
        return Path.Combine(_root, fileName);
    }

    private static Mapping MappingWithImages(
        string workbookPath, string presentationPath, params string?[] fallbackImagePaths)
    {
        return new Mapping(
            [new WorksheetSource(new WorkbookIdentifier(workbookPath), new WorksheetIdentifier("Sheet1"))],
            new PresentationSource(new PresentationIdentifier(presentationPath), new SlideIdentifier(1)),
            [],
            [
                .. fallbackImagePaths.Select(path => new ImageInstruction(
                    new HashSet<ShapeIdentifier> { new("Picture 1") },
                    [new ColumnIdentifier("Photo")],
                    new ImageEditInstruction([]),
                    path))
            ]);
    }

    private static Models.Recipe RecipeWithImages(params Mapping[] mappings)
    {
        return new Models.Recipe(mappings);
    }

    private static async Task CreateArchiveAsync(
        string archivePath, string recipeJson, CancellationToken ct,
        params (string EntryName, byte[] Bytes)[] entries)
    {
        await using var fs = File.Create(archivePath);
        await using var archive = new ZipArchive(fs, ZipArchiveMode.Create);
        var recipeEntry = archive.CreateEntry(RecipePackageFormat.Data.Recipe.FileName);
        await using (var recipeStream = await recipeEntry.OpenAsync(ct))
        {
            await recipeStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(recipeJson), ct);
        }

        foreach (var (entryName, bytes) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            await using var entryStream = await entry.OpenAsync(ct);
            await entryStream.WriteAsync(bytes, ct);
        }
    }

    #region Export

    /// <summary>
    ///     Exporting a recipe whose fallback image exists on disk bundles its bytes under <c>Images/</c>
    ///     and stores only the bare filename in <c>Recipe.json</c>.
    /// </summary>
    [Fact]
    public async Task ExportAsync_FallbackImageExists_BundlesImageAndStoresBareFileName()
    {
        var ct = TestContext.Current.CancellationToken;
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var imagePath = PathIn("fallback.png");
        await File.WriteAllBytesAsync(imagePath, imageBytes, ct);

        var recipe = RecipeWithImages(
            MappingWithImages(PathIn("book.xlsx"), PathIn("template.pptx"), imagePath));
        var original = await _repository.AddAsync(new RecipeInput("WithImage", recipe), ct);
        var archivePath = PathIn("export.recipe");

        await _packageService.ExportAsync(original.Id, archivePath, ct);

        using var archive = ZipFile.OpenRead(archivePath);
        var imageEntry = archive.GetEntry(RecipePackageFormat.Data.Images.FolderPrefix + "fallback.png");
        imageEntry.Should().NotBeNull("the fallback image must be bundled into the package");
        await using (var imageStream = imageEntry!.Open())
        {
            using var buffer = new MemoryStream();
            await imageStream.CopyToAsync(buffer, ct);
            buffer.ToArray().Should().Equal(imageBytes);
        }

        var jsonEntry = archive.GetEntry(RecipePackageFormat.Data.Recipe.FileName);
        jsonEntry.Should().NotBeNull();
        await using var jsonStream = jsonEntry!.Open();
        using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: ct);
        var fallback = document.RootElement
            .GetProperty("mappings")[0]
            .GetProperty("imageInstructions")[0]
            .GetProperty("fallbackImagePath")
            .GetString();
        fallback.Should().NotBeNull();
        Path.GetFileName(fallback).Should().Be("fallback.png");
    }

    /// <summary>
    ///     A fallback image that no longer exists on disk is skipped during export and does not throw;
    ///     the archive contains no <c>Images/</c> entry for it.
    /// </summary>
    [Fact]
    public async Task ExportAsync_FallbackImageMissing_DoesNotBundleImageAndDoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var recipe = RecipeWithImages(
            MappingWithImages(PathIn("book.xlsx"), PathIn("template.pptx"), PathIn("missing.png")));
        var original = await _repository.AddAsync(new RecipeInput("MissingImage", recipe), ct);
        var archivePath = PathIn("export.recipe");

        var act = async () => await _packageService.ExportAsync(original.Id, archivePath, ct);

        await act.Should().NotThrowAsync();

        using var archive = ZipFile.OpenRead(archivePath);
        archive.Entries
            .Where(e => e.FullName.StartsWith(RecipePackageFormat.Data.Images.FolderPrefix,
                StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }

    #endregion

    #region Round trip

    /// <summary>
    ///     A fallback image bundled by export is extracted into the images folder on import, and the
    ///     imported recipe points at that extracted copy while workbook/presentation paths keep working.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ExportedFallbackImage_ExtractsImageAndPointsRecipeAtIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var imageBytes = new byte[] { 0x01, 0x02, 0x03 };
        var imagePath = PathIn("fallback.png");
        var workbookPath = PathIn("book.xlsx");
        var presentationPath = PathIn("template.pptx");
        await File.WriteAllBytesAsync(imagePath, imageBytes, ct);
        await File.WriteAllBytesAsync(workbookPath, [0x0A], ct);
        await File.WriteAllBytesAsync(presentationPath, [0x0B], ct);

        var recipe = RecipeWithImages(
            MappingWithImages(workbookPath, presentationPath, imagePath));
        var original = await _repository.AddAsync(new RecipeInput("RoundTripImage", recipe), ct);
        var archivePath = PathIn("export.recipe");
        await _packageService.ExportAsync(original.Id, archivePath, ct);

        var workbooksDir = PathIn("workbooks");
        var presentationsDir = PathIn("presentations");
        var imagesDir = PathIn("images");

        var imported = await _packageService.ImportAsync(
            archivePath, (workbooksDir, presentationsDir, imagesDir), ct);
        var entry = await _repository.GetAsync(imported.Id, ct);
        var mapping = entry.Recipe.Mappings.Single();

        var importedFallback = mapping.ImageInstructions.Single().FallbackImagePath;
        importedFallback.Should().Be(Path.GetFullPath(Path.Combine(imagesDir, "fallback.png")));
        File.Exists(importedFallback).Should().BeTrue();
        (await File.ReadAllBytesAsync(importedFallback!, ct)).Should().Equal(imageBytes);

        var importedWorkbook = mapping.Sources.Single().Workbook.BookPath;
        var importedPresentation = mapping.Template.Presentation.PresentationPath;
        importedWorkbook.Should().StartWith(workbooksDir);
        importedPresentation.Should().StartWith(presentationsDir);
        File.Exists(importedWorkbook).Should().BeTrue();
        File.Exists(importedPresentation).Should().BeTrue();
    }

    /// <summary>
    ///     Two fallback images sharing the same file name are exported as suffixed copies; importing into
    ///     a folder that already holds the original name must not overwrite the existing file.
    /// </summary>
    [Fact]
    public async Task ExportAsync_DuplicateImageNames_BundlesSuffixedCopiesAndImportAvoidsOverwritingExisting()
    {
        var ct = TestContext.Current.CancellationToken;
        var dir1 = PathIn("dir1");
        var dir2 = PathIn("dir2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        var image1 = Path.Combine(dir1, "photo.png");
        var image2 = Path.Combine(dir2, "photo.png");
        await File.WriteAllBytesAsync(image1, [0x01], ct);
        await File.WriteAllBytesAsync(image2, [0x02], ct);

        var recipe = RecipeWithImages(
            MappingWithImages(PathIn("book1.xlsx"), PathIn("template.pptx"), image1),
            MappingWithImages(PathIn("book2.xlsx"), PathIn("template.pptx"), image2));
        var original = await _repository.AddAsync(new RecipeInput("DuplicateImages", recipe), ct);
        var archivePath = PathIn("export.recipe");
        await _packageService.ExportAsync(original.Id, archivePath, ct);

        using (var archive = ZipFile.OpenRead(archivePath))
        {
            archive.GetEntry(RecipePackageFormat.Data.Images.FolderPrefix + "photo.png").Should().NotBeNull();
            archive.GetEntry(RecipePackageFormat.Data.Images.FolderPrefix + "photo_1.png").Should().NotBeNull();
        }

        var workbooksDir = PathIn("workbooks");
        var presentationsDir = PathIn("presentations");
        var imagesDir = PathIn("images");
        Directory.CreateDirectory(imagesDir);
        var conflictingPath = Path.Combine(imagesDir, "photo.png");
        await File.WriteAllBytesAsync(conflictingPath, [0xFF], ct);

        var imported = await _packageService.ImportAsync(
            archivePath, (workbooksDir, presentationsDir, imagesDir), ct);
        var entry = await _repository.GetAsync(imported.Id, ct);

        var importedImages = entry.Recipe.Mappings
            .SelectMany(m => m.ImageInstructions)
            .Select(i => i.FallbackImagePath)
            .ToList();

        importedImages.Should().HaveCount(2);
        importedImages.Should().OnlyHaveUniqueItems();
        importedImages.Should().AllSatisfy(p => File.Exists(p).Should().BeTrue());
        (await File.ReadAllBytesAsync(conflictingPath, ct)).Should().Equal(0xFF);
    }

    #endregion

    #region Import

    /// <summary>
    ///     A package whose recipe names a fallback image that the archive does not ship leaves the
    ///     imported instruction without a fallback path instead of pointing at a local file.
    /// </summary>
    [Fact]
    public async Task ImportAsync_FallbackImageMissingFromArchive_ClearsFallbackImagePath()
    {
        var ct = TestContext.Current.CancellationToken;
        const string recipeJson = """
                                  {
                                    "mappings": [
                                      {
                                        "sources": [],
                                        "template": {
                                          "presentation": { "presentationPath": "template.pptx" },
                                          "slide": { "slideIndex": 1 }
                                        },
                                        "textInstructions": [],
                                        "imageInstructions": [
                                          {
                                            "shapes": [],
                                            "columns": [],
                                            "imageEditInstruction": { "roiOptions": [] },
                                            "fallbackImagePath": "missing.png"
                                          }
                                        ]
                                      }
                                    ]
                                  }
                                  """;
        var archivePath = PathIn("missing-image.recipe");
        await CreateArchiveAsync(archivePath, recipeJson, ct);

        var imagesDir = PathIn("images");
        var imported = await _packageService.ImportAsync(
            archivePath, (PathIn("workbooks"), PathIn("presentations"), imagesDir), ct);
        var entry = await _repository.GetAsync(imported.Id, ct);

        entry.Recipe.Mappings.Single().ImageInstructions.Single().FallbackImagePath.Should().BeNull();
        File.Exists(Path.Combine(imagesDir, "missing.png")).Should().BeFalse();
    }

    /// <summary>An Images/ entry with a disallowed extension is ignored and nothing is extracted.</summary>
    [Fact]
    public async Task ImportAsync_DisallowedImageExtension_FileNotExtracted()
    {
        var ct = TestContext.Current.CancellationToken;
        const string recipeJson = """{"mappings":[]}""";
        var archivePath = PathIn("bad-extension.recipe");
        await CreateArchiveAsync(archivePath, recipeJson, ct,
            ("Images/payload.exe", [0xFF, 0xD8]));

        var imagesDir = PathIn("images");
        var imported = await _packageService.ImportAsync(
            archivePath, (PathIn("workbooks"), PathIn("presentations"), imagesDir), ct);

        imported.Id.Should().BeGreaterThan(0);
        Directory.Exists(imagesDir).Should().BeFalse();
    }

    /// <summary>
    ///     An <c>Images/../escape.png</c> entry is rejected with <see cref="InvalidDataException" /> (Zip
    ///     Slip) and nothing is written outside the images folder.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ImageEntryWithDotDot_ThrowsInvalidDataExceptionAndWritesNothingOutside()
    {
        var ct = TestContext.Current.CancellationToken;
        const string recipeJson = """{"mappings":[]}""";
        var archivePath = PathIn("zip-slip.recipe");
        await CreateArchiveAsync(archivePath, recipeJson, ct,
            ("Images/../escape.png", [0x42]));

        var imagesDir = Path.Combine(PathIn("target"), "images");
        var act = async () => await _packageService.ImportAsync(
            archivePath, (PathIn("workbooks"), PathIn("presentations"), imagesDir), ct);

        await act.Should().ThrowAsync<InvalidDataException>();
        File.Exists(PathIn("escape.png")).Should().BeFalse();
        File.Exists(Path.Combine(PathIn("target"), "escape.png")).Should().BeFalse();
    }

    #endregion
}
