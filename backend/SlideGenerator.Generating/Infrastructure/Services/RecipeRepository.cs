/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: RecipeRepository.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using Microsoft.Data.Sqlite;
using SlideGenerator.Generating.Application.Abstractions;
using SlideGenerator.Generating.Domain.Models.Recipes;

namespace SlideGenerator.Generating.Infrastructure.Services;

/// <summary>
///     SQLite-backed implementation of <see cref="IRecipeRepository" />.
///     Holds a single open connection for the lifetime of the application.
/// </summary>
internal sealed class RecipeRepository : IRecipeRepository, IDisposable
{
    private readonly SqliteConnection _conn;

    /// <summary>
    ///     Opens the connection and ensures the database schema exists.
    /// </summary>
    public RecipeRepository(string connectionString)
    {
        _conn = new SqliteConnection(connectionString);
        _conn.Open();
        EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _conn.Dispose();
    }

    /// <inheritdoc />
    public async Task<int> AddAsync(Recipe recipe, string? displayName, string? flowData,
        CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO Recipes (DisplayName, FlowData, RecipeJson) VALUES (@displayName, @flowData, @json); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@flowData", (object?)flowData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@json", RecipeSerializer.Serialize(recipe));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<RecipeEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DisplayName, FlowData, RecipeJson FROM Recipes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return new RecipeEntry(
            reader.GetInt32(0),
            RecipeSerializer.Deserialize(reader.GetString(3)), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeEntry>> ListAsync(CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DisplayName, FlowData, RecipeJson FROM Recipes ORDER BY Id";

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var results = new List<RecipeEntry>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            results.Add(new RecipeEntry(
                reader.GetInt32(0),
                RecipeSerializer.Deserialize(reader.GetString(3)), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(int id, string? displayName, string? flowData, Recipe? recipe = null,
        CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        if (recipe is null)
        {
            cmd.CommandText = "UPDATE Recipes SET DisplayName = @displayName, FlowData = @flowData WHERE Id = @id";
        }
        else
        {
            cmd.CommandText =
                "UPDATE Recipes SET DisplayName = @displayName, FlowData = @flowData, RecipeJson = @recipeJson WHERE Id = @id";
            cmd.Parameters.AddWithValue("@recipeJson", RecipeSerializer.Serialize(recipe));
        }

        cmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@flowData", (object?)flowData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Recipes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
    }

    private void EnsureCreated()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
                          CREATE TABLE IF NOT EXISTS Recipes (
                              Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                              DisplayName TEXT,
                              FlowData    TEXT,
                              RecipeJson  TEXT    NOT NULL
                          );
                          """;
        cmd.ExecuteNonQuery();
    }
}