/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
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

using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Data.Sqlite;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Domain.Models;

namespace SlideGenerator.Recipe.Infrastructure.Services;

/// <summary>
///     SQLite-backed implementation of <see cref="IRecipeRepository" />.
///     Holds a single open connection for the lifetime of the application.
/// </summary>
internal sealed class RecipeRepository : IRecipeRepository, IDisposable
{
    private readonly SqliteConnection _conn;

    /// <summary>Opens the connection and ensures the database schema exists.</summary>
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
    public async Task<int> AddAsync(string? displayName, string? recipe, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO Recipes (DisplayName, Recipe, CreatedTimestamp, UpdatedTimestamp) " +
            "VALUES (@displayName, @recipe, @now, @now); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@recipe", (object?)recipe ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@now", now);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<RecipeEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, DisplayName, Recipe, CreatedTimestamp, UpdatedTimestamp FROM Recipes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return ReadEntry(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeEntry>> ListAsync(CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DisplayName, Recipe, CreatedTimestamp, UpdatedTimestamp FROM Recipes ORDER BY Id";

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var results = new List<RecipeEntry>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            results.Add(ReadEntry(reader));

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(int id, string? displayName, string? recipe, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            "UPDATE Recipes SET DisplayName = @displayName, Recipe = @recipe, UpdatedTimestamp = @now WHERE Id = @id";
        cmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@recipe", (object?)recipe ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@now", now);
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

    /// <inheritdoc />
    public async Task ExportAsync(int recipeId, string outputFilePath, string? password, CancellationToken ct = default)
    {
        var entry = await GetByIdAsync(recipeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Recipe {recipeId} not found.");

        var dir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await Task.Run(() =>
        {
            using var outputStream = File.Create(outputFilePath);
            using var zipStream = new ZipOutputStream(outputStream);
            zipStream.SetLevel(9);
            if (!string.IsNullOrEmpty(password))
                zipStream.Password = password;

            var recipeBytes = Encoding.UTF8.GetBytes(entry.Recipe ?? string.Empty);
            var zipEntry = new ZipEntry("recipe.json")
            {
                DateTime = DateTime.UtcNow,
                Size = recipeBytes.Length
            };
            if (!string.IsNullOrEmpty(password))
                zipEntry.AESKeySize = 256;

            zipStream.PutNextEntry(zipEntry);
            zipStream.Write(recipeBytes, 0, recipeBytes.Length);
            zipStream.CloseEntry();

            // TODO: Bundle Workbooks/ and Presentations/ into the archive.
            // Blocked on: IRecipeSummarizer implementation + ReactFlow JSON schema.

            zipStream.Finish();
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> ImportAsync(string filePath, string? password, string workbooksDirectory,
        string presentationsDirectory, CancellationToken ct = default)
    {
        var displayName = Path.GetFileNameWithoutExtension(filePath);
        string? recipeJson = null;

        await Task.Run(() =>
        {
            using var inputStream = File.OpenRead(filePath);
            using var zipFile = new ZipFile(inputStream);
            if (!string.IsNullOrEmpty(password))
                zipFile.Password = password;

            foreach (ZipEntry zipEntry in zipFile)
            {
                if (!zipEntry.IsFile) continue;

                var entryName = zipEntry.Name;
                using var entryStream = zipFile.GetInputStream(zipEntry);

                if (string.Equals(entryName, "recipe.json", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    recipeJson = reader.ReadToEnd();
                }
                else if (entryName.StartsWith("Workbooks/", StringComparison.OrdinalIgnoreCase))
                {
                    var relativeName = entryName["Workbooks/".Length..];
                    if (string.IsNullOrEmpty(relativeName)) continue;
                    var targetPath = Path.Combine(workbooksDirectory, relativeName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    using var targetStream = File.Create(targetPath);
                    StreamUtils.Copy(entryStream, targetStream, new byte[4096]);
                }
                else if (entryName.StartsWith("Presentations/", StringComparison.OrdinalIgnoreCase))
                {
                    var relativeName = entryName["Presentations/".Length..];
                    if (string.IsNullOrEmpty(relativeName)) continue;
                    var targetPath = Path.Combine(presentationsDirectory, relativeName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    using var targetStream = File.Create(targetPath);
                    StreamUtils.Copy(entryStream, targetStream, new byte[4096]);
                }
            }

            // TODO: Rewrite zip-relative file paths in recipeJson to absolute paths.
            // Blocked on: IRecipeSummarizer implementation + ReactFlow JSON schema.
        }, ct).ConfigureAwait(false);

        return await AddAsync(displayName, recipeJson, ct).ConfigureAwait(false);
    }

    private static RecipeEntry ReadEntry(SqliteDataReader reader)
    {
        return new RecipeEntry(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3)),
            DateTimeOffset.Parse(reader.GetString(4)));
    }

    private void EnsureCreated()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
                          CREATE TABLE IF NOT EXISTS Recipes (
                              Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                              DisplayName      TEXT,
                              Recipe           TEXT,
                              CreatedTimestamp  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                              UpdatedTimestamp  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
                          );
                          """;
        cmd.ExecuteNonQuery();
    }
}