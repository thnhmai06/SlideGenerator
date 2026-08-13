/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SlideGenerator.Recipe;

/// <summary>
///     Provides persistent storage for <see cref="RecipeEntry" /> configurations.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    ///     Inserts a new recipe row and returns its metadata.
    /// </summary>
    /// <param name="input">The recipe input containing the name and mapping data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="IRecipeMetadata" /> of the newly inserted row.</returns>
    Task<IRecipeMetadata> AddAsync(RecipeInput input, CancellationToken ct = default);

    /// <summary>
    ///     Retrieves a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="RecipeEntry" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given id exists.</exception>
    Task<RecipeEntry> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    ///     Returns metadata for all stored recipe entries ordered by the most recently updated.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<IRecipeMetadata>> ListAsync(CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing recipe entry.
    /// </summary>
    /// <param name="id">The database-generated id of the recipe to update.</param>
    /// <param name="input">The new name and mapping data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="IRecipeMetadata" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given id exists.</exception>
    Task<IRecipeMetadata> UpdateAsync(int id, RecipeInput input, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true" /> if a row was deleted; <see langword="false" /> if the id was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
///     SQLite-backed implementation of <see cref="IRecipeRepository" />.
///     Creates a short-lived connection per CRUD operation; no shared long-lived connection.
///     Package export/import lives in <see cref="RecipePackageService" /> instead — this type is CRUD-only.
/// </summary>
internal sealed class SqliteRecipeRepository(SqliteConnectionStringBuilder builder) : IRecipeRepository
{
    /// <inheritdoc />
    public async Task<IRecipeMetadata> AddAsync(RecipeInput input, CancellationToken ct = default)
    {
        var now = DbFormatUtc(DateTimeOffset.UtcNow);
        await using var conn = await builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        var id = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO Recipes (Name, Recipe, CreatedTimestamp, UpdatedTimestamp) " +
            "VALUES (@name, @graph, @now, @now); SELECT last_insert_rowid();",
            new { name = input.Name, graph = JsonSerializer.Serialize(input.Recipe, RecipeGraphJson.Options), now },
            cancellationToken: ct)).ConfigureAwait(false);
        var ts = DateTimeOffset.Parse(now, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new RecipeEntry(id, input.Name, input.Recipe, ts, ts);
    }

    /// <inheritdoc />
    public async Task<RecipeEntry> GetAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        var row = await conn.QuerySingleOrDefaultAsync<RecipeRow>(new CommandDefinition(
            "SELECT Id, Name, Recipe, CreatedTimestamp, UpdatedTimestamp FROM Recipes WHERE Id = @id",
            new { id }, cancellationToken: ct)).ConfigureAwait(false);
        return row is not null ? DbReadEntry(row) : throw new InvalidOperationException($"Recipe {id} not found.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IRecipeMetadata>> ListAsync(CancellationToken ct = default)
    {
        await using var conn = await builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<RecipeRow>(new CommandDefinition(
            "SELECT Id, Name, Recipe, CreatedTimestamp, UpdatedTimestamp FROM Recipes ORDER BY UpdatedTimestamp DESC, CreatedTimestamp DESC, Id DESC",
            cancellationToken: ct)).ConfigureAwait(false);

        return [.. rows.Select(DbReadEntry)];
    }

    /// <inheritdoc />
    public async Task<IRecipeMetadata> UpdateAsync(int id, RecipeInput input, CancellationToken ct = default)
    {
        var now = DbFormatUtc(DateTimeOffset.UtcNow);
        await using var conn = await builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Recipes SET Name = @name, Recipe = @graph, UpdatedTimestamp = @now WHERE Id = @id",
            new { name = input.Name, graph = JsonSerializer.Serialize(input.Recipe, RecipeGraphJson.Options), now, id },
            cancellationToken: ct)).ConfigureAwait(false);
        if (affected == 0) throw new InvalidOperationException($"Recipe {id} not found.");
        return await GetAsync(id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Recipes WHERE Id = @id", new { id }, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }


    #region Helpers

    private static string DbFormatUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture);
    }

    /// <summary>Raw row shape returned by Dapper for the <c>Recipes</c> table (SQLite INTEGER columns bind as <see cref="long" />).</summary>
    private sealed record RecipeRow(
        long Id,
        string Name,
        string Recipe,
        string CreatedTimestamp,
        string UpdatedTimestamp);

    private static RecipeEntry DbReadEntry(RecipeRow row)
    {
        Mappings.Recipe graph;
        try
        {
            graph = JsonSerializer.Deserialize<Mappings.Recipe>(row.Recipe, RecipeGraphJson.Options) ??
                    new Mappings.Recipe([]);
        }
        catch (JsonException)
        {
            graph = new Mappings.Recipe([]);
        }

        // ponytail: missing/null "mappings" on otherwise-valid JSON is treated as an empty recipe,
        // not a crash — matches the "archive rejected? no, just empty" spirit without hiding real parse errors.
        graph = new Mappings.Recipe(Mappings: graph.Mappings);

        return new RecipeEntry(
            (int)row.Id,
            row.Name,
            graph,
            DateTimeOffset.Parse(row.CreatedTimestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            DateTimeOffset.Parse(row.UpdatedTimestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
    }

    #endregion
}