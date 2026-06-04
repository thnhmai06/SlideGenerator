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
using Microsoft.Data.Sqlite;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Models.Summary;

namespace SlideGenerator.Recipe.Infrastructure.Services;

/// <summary>
///     SQLite-backed implementation of <see cref="IRecipeRepository" />.
///     Creates a short-lived connection per CRUD operation; no shared long-lived connection.
/// </summary>
internal sealed partial class RecipeRepository : IRecipeRepository
{
    private readonly SqliteConnectionStringBuilder _builder;

    /// <summary>Ensures the database schema exists using a one-shot connection.</summary>
    public RecipeRepository(SqliteConnectionStringBuilder builder)
    {
        _builder = builder;
        DbEnsureCreated();
    }

    /// <inheritdoc />
    public async Task<IRecipeMetadata> AddAsync(RecipeInput input, CancellationToken ct = default)
    {
        var now = DbFormatUtc(DateTimeOffset.UtcNow);
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO Recipes (Name, Graph, CreatedTimestamp, UpdatedTimestamp) " +
            "VALUES (@name, @graph, @now, @now); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", input.Name);
        cmd.Parameters.AddWithValue("@graph", input.Graph);
        cmd.Parameters.AddWithValue("@now", now);
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
        var ts = DateTimeOffset.Parse(now, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new RecipeEntry(id, input.Name, input.Graph, ts, ts);
    }

    /// <inheritdoc />
    public async Task<RecipeEntry> GetAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, Name, Graph, CreatedTimestamp, UpdatedTimestamp FROM Recipes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Recipe {id} not found.");

        return DbReadEntry(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IRecipeMetadata>> ListAsync(CancellationToken ct = default)
    {
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, Name, Graph, CreatedTimestamp, UpdatedTimestamp FROM Recipes ORDER BY UpdatedTimestamp DESC, CreatedTimestamp DESC, Id DESC";

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var results = new List<IRecipeMetadata>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            results.Add(DbReadEntry(reader));

        return results;
    }

    /// <inheritdoc />
    public async Task<IRecipeMetadata> UpdateAsync(int id, RecipeInput input, CancellationToken ct = default)
    {
        var now = DbFormatUtc(DateTimeOffset.UtcNow);
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "UPDATE Recipes SET Name = @name, Graph = @graph, UpdatedTimestamp = @now WHERE Id = @id";
        cmd.Parameters.AddWithValue("@name", input.Name);
        cmd.Parameters.AddWithValue("@graph", input.Graph);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (affected == 0) throw new InvalidOperationException($"Recipe {id} not found.");
        return await GetAsync(id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Recipes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
    }

    /// <inheritdoc />
    public async Task<RecipeSummary> SummarizeAsync(int id, CancellationToken ct = default)
    {
        var entry = await GetAsync(id, ct).ConfigureAwait(false);
        return Summarize(entry.Graph);
    }

    #region Helpers

    private static string DbFormatUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture);
    }

    private static RecipeEntry DbReadEntry(SqliteDataReader reader)
    {
        return new RecipeEntry(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            DateTimeOffset.Parse(reader.GetString(4),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
    }

    private void DbEnsureCreated()
    {
        using var conn = _builder.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = """
                          CREATE TABLE IF NOT EXISTS Recipes (
                              Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                              Name      TEXT NOT NULL,
                              Graph            TEXT NOT NULL,
                              CreatedTimestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                              UpdatedTimestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
                          );
                          """;
        cmd.ExecuteNonQuery();
    }

    #endregion
}
