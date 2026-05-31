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

using FluentAssertions;
using Microsoft.Data.Sqlite;
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
        _repo = new RecipeRepository(builder, new NullRecipeFileManifestExtractor());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
    }

    #region AddAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.AddAsync" /> inserts a row and returns a positive ID.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidEntry_ReturnsPositiveId()
    {
        var id = await _repo.AddAsync("My Recipe", "{}", CancellationToken.None);

        id.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.AddAsync" /> with null display name and null recipe
    ///     still inserts a row and returns a valid ID.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullDisplayNameAndRecipe_ReturnsValidId()
    {
        var id = await _repo.AddAsync(null, null, CancellationToken.None);

        id.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     Verifies that successive <see cref="RecipeRepository.AddAsync" /> calls assign monotonically
    ///     increasing IDs.
    /// </summary>
    [Fact]
    public async Task AddAsync_MultipleEntries_IdsAreIncreasing()
    {
        var id1 = await _repo.AddAsync("A", null, TestContext.Current.CancellationToken);
        var id2 = await _repo.AddAsync("B", null, TestContext.Current.CancellationToken);

        id2.Should().BeGreaterThan(id1);
    }

    #endregion

    #region GetByIdAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetByIdAsync" /> returns <see langword="null" />
    ///     for a non-existent ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var entry = await _repo.GetByIdAsync(9999, TestContext.Current.CancellationToken);

        entry.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetByIdAsync" /> returns the correct entry
    ///     after insertion.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCorrectEntry()
    {
        var id = await _repo.AddAsync("TestName", "{\"key\":1}", TestContext.Current.CancellationToken);

        var entry = await _repo.GetByIdAsync(id, TestContext.Current.CancellationToken);

        entry.Should().NotBeNull();
        entry.Id.Should().Be(id);
        entry.DisplayName.Should().Be("TestName");
        entry.Recipe.Should().Be("{\"key\":1}");
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
    ///     Verifies that <see cref="RecipeRepository.ListAsync" /> returns all inserted entries
    ///     ordered by ascending ID.
    /// </summary>
    [Fact]
    public async Task ListAsync_MultipleEntries_ReturnsAllOrderedById()
    {
        await _repo.AddAsync("Alpha", null, TestContext.Current.CancellationToken);
        await _repo.AddAsync("Beta", null, TestContext.Current.CancellationToken);
        await _repo.AddAsync("Gamma", null, TestContext.Current.CancellationToken);

        var list = await _repo.ListAsync(TestContext.Current.CancellationToken);

        list.Should().HaveCount(3);
        list.Select(e => e.DisplayName).Should().ContainInOrder("Alpha", "Beta", "Gamma");
    }

    #endregion

    #region UpdateAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> returns <see langword="false" />
    ///     for a non-existent ID without throwing.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsFalse()
    {
        var updated = await _repo.UpdateAsync(9999, "NewName", "{}", TestContext.Current.CancellationToken);

        updated.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> overwrites the display name and
    ///     recipe of an existing entry.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ExistingId_OverwritesFields()
    {
        var id = await _repo.AddAsync("Old", "{\"v\":0}", TestContext.Current.CancellationToken);

        var updated = await _repo.UpdateAsync(id, "New", "{\"v\":1}", TestContext.Current.CancellationToken);
        var entry = await _repo.GetByIdAsync(id, TestContext.Current.CancellationToken);

        updated.Should().BeTrue();
        entry!.DisplayName.Should().Be("New");
        entry.Recipe.Should().Be("{\"v\":1}");
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
    ///     subsequent <see cref="RecipeRepository.GetByIdAsync" /> returns <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesRow()
    {
        var id = await _repo.AddAsync("ToDelete", null, TestContext.Current.CancellationToken);

        var deleted = await _repo.DeleteAsync(id, TestContext.Current.CancellationToken);
        var entry = await _repo.GetByIdAsync(id, TestContext.Current.CancellationToken);

        deleted.Should().BeTrue();
        entry.Should().BeNull();
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
        var id = await _repo.AddAsync("Exported", "{\"nodes\":[]}", TestContext.Current.CancellationToken);
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.recipe");

        try
        {
            await _repo.ExportAsync(id, outputPath, null, TestContext.Current.CancellationToken);

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
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.recipe");

        var act = async () => await _repo.ExportAsync(9999, outputPath, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> reads a ZIP exported by
    ///     <see cref="RecipeRepository.ExportAsync" /> and inserts a new row, returning a positive ID.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ValidZipFile_InsertsRowAndReturnsNewId()
    {
        var exportedId = await _repo.AddAsync("Original", "{\"nodes\":[1,2]}", TestContext.Current.CancellationToken);
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.recipe");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await _repo.ExportAsync(exportedId, zipPath, null, TestContext.Current.CancellationToken);

            var importedId = await _repo.ImportAsync(zipPath, null, workbooksDir, presentationsDir,
                TestContext.Current.CancellationToken);

            importedId.Should().BeGreaterThan(0);
            importedId.Should().NotBe(exportedId);

            var imported = await _repo.GetByIdAsync(importedId, TestContext.Current.CancellationToken);
            imported.Should().NotBeNull();
            imported.Recipe.Should().Be("{\"nodes\":[1,2]}");
        }
        finally
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(workbooksDir)) Directory.Delete(workbooksDir, true);
            if (Directory.Exists(presentationsDir)) Directory.Delete(presentationsDir, true);
        }
    }

    #endregion
}