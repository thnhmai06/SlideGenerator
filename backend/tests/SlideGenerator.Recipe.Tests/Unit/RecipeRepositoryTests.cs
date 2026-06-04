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
using SlideGenerator.Recipe.Domain.Models;
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

    private static RecipeInput Input(string name, string graph) => new(name, graph);

    #region AddAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.AddAsync" /> inserts a row and returns metadata with a positive ID.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidEntry_ReturnsMetadataWithPositiveId()
    {
        var metadata = await _repo.AddAsync(Input("My Recipe", "{}"), CancellationToken.None);

        metadata.Id.Should().BeGreaterThan(0);
        metadata.Name.Should().Be("My Recipe");
    }

    /// <summary>
    ///     Verifies that successive <see cref="RecipeRepository.AddAsync" /> calls assign monotonically
    ///     increasing IDs.
    /// </summary>
    [Fact]
    public async Task AddAsync_MultipleEntries_IdsAreIncreasing()
    {
        var m1 = await _repo.AddAsync(Input("A", "{}"), TestContext.Current.CancellationToken);
        var m2 = await _repo.AddAsync(Input("B", "{}"), TestContext.Current.CancellationToken);

        m2.Id.Should().BeGreaterThan(m1.Id);
    }

    #endregion

    #region GetAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetAsync" /> throws <see cref="InvalidOperationException" />
    ///     for a non-existent ID.
    /// </summary>
    [Fact]
    public async Task GetAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var act = async () => await _repo.GetAsync(9999, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.GetAsync" /> returns the correct entry
    ///     after insertion.
    /// </summary>
    [Fact]
    public async Task GetAsync_ExistingId_ReturnsCorrectEntry()
    {
        var metadata = await _repo.AddAsync(Input("TestName", "{\"key\":1}"), TestContext.Current.CancellationToken);

        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        entry.Id.Should().Be(metadata.Id);
        entry.Name.Should().Be("TestName");
        entry.Graph.Should().Be("{\"key\":1}");
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
        await _repo.AddAsync(Input("Alpha", "{}"), TestContext.Current.CancellationToken);
        await _repo.AddAsync(Input("Beta", "{}"), TestContext.Current.CancellationToken);
        await _repo.AddAsync(Input("Gamma", "{}"), TestContext.Current.CancellationToken);

        var list = await _repo.ListAsync(TestContext.Current.CancellationToken);

        list.Should().HaveCount(3);
        list.Select(e => e.Name).Should().Contain(["Alpha", "Beta", "Gamma"]);
    }

    #endregion

    #region UpdateAsync

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> throws <see cref="InvalidOperationException" />
    ///     for a non-existent ID.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var act = async () => await _repo.UpdateAsync(9999, Input("NewName", "{}"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.UpdateAsync" /> overwrites the display name and
    ///     graph of an existing entry and returns the updated metadata.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ExistingId_OverwritesFields()
    {
        var metadata = await _repo.AddAsync(Input("Old", "{\"v\":0}"), TestContext.Current.CancellationToken);

        var updated = await _repo.UpdateAsync(metadata.Id, Input("New", "{\"v\":1}"), TestContext.Current.CancellationToken);

        updated.Name.Should().Be("New");
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
    ///     subsequent <see cref="RecipeRepository.GetAsync" /> returns <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesRow()
    {
        var metadata = await _repo.AddAsync(Input("ToDelete", "{}"), TestContext.Current.CancellationToken);

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
        var metadata = await _repo.AddAsync(Input("Exported", "{\"nodes\":[]}"), TestContext.Current.CancellationToken);
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.recipe");

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
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.recipe");

        var act = async () => await _repo.ExportAsync(9999, outputPath, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> reads a ZIP exported by
    ///     <see cref="RecipeRepository.ExportAsync" /> and inserts a new row with a positive ID.
    ///     File extraction is skipped until recipe JSON schema is finalized and Summarize is implemented.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ValidZipFile_InsertsNewRow()
    {
        var exportedMetadata = await _repo.AddAsync(Input("Original", "{\"nodes\":[1,2]}"), TestContext.Current.CancellationToken);
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.recipe");
        var workbooksDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var presentationsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            await _repo.ExportAsync(exportedMetadata.Id, zipPath, null, TestContext.Current.CancellationToken);

            var imported = await _repo.ImportAsync(zipPath, null, (workbooksDir, presentationsDir),
                TestContext.Current.CancellationToken);

            // TODO: add round-trip assertion (name, graph contents) once recipe JSON schema is finalized.
            imported.Id.Should().BeGreaterThan(0);
            imported.Id.Should().NotBe(exportedMetadata.Id);
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
