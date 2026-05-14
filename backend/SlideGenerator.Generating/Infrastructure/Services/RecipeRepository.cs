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
using SlideGenerator.Generating.Domain.Models;
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
    public async Task<int> GetOrAddAsync(Recipe recipe, string name, string? flowData, CancellationToken ct = default)
    {
        var json = RecipeSerializer.Serialize(recipe);

        await using (var insertCmd = _conn.CreateCommand())
        {
            insertCmd.CommandText =
                "INSERT OR IGNORE INTO Recipes (Name, FlowData, RecipeJson) VALUES (@name, @flowData, @json)";
            insertCmd.Parameters.AddWithValue("@name", name);
            insertCmd.Parameters.AddWithValue("@flowData", (object?)flowData ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@json", json);
            await insertCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await using var selectCmd = _conn.CreateCommand();
        selectCmd.CommandText = "SELECT Id FROM Recipes WHERE RecipeJson = @json";
        selectCmd.Parameters.AddWithValue("@json", json);
        return Convert.ToInt32(await selectCmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<RecipeEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, FlowData, RecipeJson FROM Recipes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return new RecipeEntry(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            RecipeSerializer.Deserialize(reader.GetString(3)));
    }

    /// <inheritdoc />
    public void Dispose() => _conn.Dispose();

    private void EnsureCreated()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Recipes (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                Name       TEXT    NOT NULL,
                FlowData   TEXT,
                RecipeJson TEXT    NOT NULL UNIQUE
            );
            """;
        cmd.ExecuteNonQuery();
    }
}

