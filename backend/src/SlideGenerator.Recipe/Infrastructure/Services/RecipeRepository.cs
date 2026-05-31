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

using System.Globalization;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Data.Sqlite;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Rules;

namespace SlideGenerator.Recipe.Infrastructure.Services;

/// <summary>
///     SQLite-backed implementation of <see cref="IRecipeRepository" />.
///     Creates a short-lived connection per CRUD operation; no shared long-lived connection.
/// </summary>
internal sealed class RecipeRepository : IRecipeRepository
{
    private const string WorkbooksPrefix = "Workbooks/";
    private const string PresentationsPrefix = "Presentations/";
    private const string RecipeJsonEntryName = "recipe.json";

    private readonly SqliteConnectionStringBuilder _builder;
    private readonly IRecipeFileManifestExtractor _manifestExtractor;

    /// <summary>Ensures the database schema exists using a one-shot connection.</summary>
    public RecipeRepository(SqliteConnectionStringBuilder builder, IRecipeFileManifestExtractor manifestExtractor)
    {
        _builder = builder;
        _manifestExtractor = manifestExtractor;
        EnsureCreated();
    }

    /// <inheritdoc />
    public async Task<int> AddAsync(string? displayName, string? recipe, CancellationToken ct = default)
    {
        var now = FormatUtc(DateTimeOffset.UtcNow);
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
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
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
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
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
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
        var now = FormatUtc(DateTimeOffset.UtcNow);
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
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
        await using var conn = await _builder.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
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
            // Zip-bomb preflight: bounded compressed size.
            var compressedSize = new FileInfo(filePath).Length;
            if (compressedSize > ZipImportRules.MaxCompressedArchiveBytes)
                throw new InvalidDataException(
                    $"Archive rejected: compressed size {compressedSize} exceeds limit " +
                    $"{ZipImportRules.MaxCompressedArchiveBytes} bytes.");

            using var inputStream = File.OpenRead(filePath);
            using var zipFile = new ZipFile(inputStream);
            if (!string.IsNullOrEmpty(password))
                zipFile.Password = password;

            if (zipFile.Count > ZipImportRules.MaxEntryCount)
                throw new InvalidDataException(
                    $"Archive rejected: entry count {zipFile.Count} exceeds limit " +
                    $"{ZipImportRules.MaxEntryCount}.");

            // Pass 1: extract recipe.json and derive the file manifest.
            recipeJson = ReadRecipeJson(zipFile);
            if (recipeJson == null)
                throw new InvalidDataException(
                    "Archive rejected: required entry 'recipe.json' is missing.");
            var manifest = _manifestExtractor.ExtractReferencedFiles(recipeJson);

            // Pass 2: extract whitelisted entries with zip-bomb + path-traversal guards.
            var workbooksFull = Path.GetFullPath(workbooksDirectory) + Path.DirectorySeparatorChar;
            var presentationsFull = Path.GetFullPath(presentationsDirectory) + Path.DirectorySeparatorChar;
            ExtractZipEntries(zipFile, manifest, workbooksFull, presentationsFull);

            // TODO: Rewrite zip-relative file paths in recipeJson to absolute paths.
            // Blocked on: IRecipeSummarizer implementation + ReactFlow JSON schema.
        }, ct).ConfigureAwait(false);

        return await AddAsync(displayName, recipeJson, ct).ConfigureAwait(false);
    }

    private static void ExtractZipEntries(
        ZipFile zipFile, IReadOnlySet<string>? manifest,
        string workbooksFull, string presentationsFull)
    {
        var totalUncompressed = 0L;
        foreach (ZipEntry zipEntry in zipFile)
        {
            if (!zipEntry.IsFile) continue;
            var entryName = zipEntry.Name;
            if (string.Equals(entryName, RecipeJsonEntryName, StringComparison.OrdinalIgnoreCase)) continue;
            EnforceEntrySizeLimits(zipEntry, ref totalUncompressed);
            ExtractSingleEntry(zipFile, zipEntry, entryName, manifest, workbooksFull, presentationsFull);
        }
    }

    private static void ExtractSingleEntry(
        ZipFile zipFile, ZipEntry zipEntry, string entryName,
        IReadOnlySet<string>? manifest, string workbooksFull, string presentationsFull)
    {
        string targetDirFull;
        string relativeName;
        IReadOnlySet<string> allowedExtensions;

        if (entryName.StartsWith(WorkbooksPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[WorkbooksPrefix.Length..];
            targetDirFull = workbooksFull;
            allowedExtensions = ZipImportRules.AllowedWorkbookExtensions;
        }
        else if (entryName.StartsWith(PresentationsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[PresentationsPrefix.Length..];
            targetDirFull = presentationsFull;
            allowedExtensions = ZipImportRules.AllowedPresentationExtensions;
        }
        else return;

        if (string.IsNullOrEmpty(relativeName)) return;
        var ext = Path.GetExtension(relativeName);
        if (!allowedExtensions.Contains(ext)) return;
        if (manifest != null && !manifest.Contains(NormalizeManifestPath(entryName))) return;

        var targetPath = Path.Combine(targetDirFull, relativeName);
        var targetFull = Path.GetFullPath(targetPath);
        if (!targetFull.StartsWith(targetDirFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Archive rejected: entry '{entryName}' escapes the target directory.");

        Directory.CreateDirectory(Path.GetDirectoryName(targetFull)!);
        using var entryStream = zipFile.GetInputStream(zipEntry);
        using var targetStream = File.Create(targetFull);
        StreamUtils.Copy(entryStream, targetStream, new byte[4096]);
    }

    private static string? ReadRecipeJson(ZipFile zipFile)
    {
        foreach (ZipEntry entry in zipFile)
        {
            if (!entry.IsFile) continue;
            if (!string.Equals(entry.Name, RecipeJsonEntryName, StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = zipFile.GetInputStream(entry);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return null;
    }

    private static void EnforceEntrySizeLimits(ZipEntry entry, ref long totalUncompressed)
    {
        if (entry.Size > ZipImportRules.MaxEntryUncompressedBytes)
            throw new InvalidDataException(
                $"Archive rejected: entry '{entry.Name}' uncompressed size {entry.Size} exceeds limit " +
                $"{ZipImportRules.MaxEntryUncompressedBytes}.");

        if (entry.CompressedSize > 0)
        {
            var ratio = (double)entry.Size / entry.CompressedSize;
            if (ratio > ZipImportRules.MaxEntryCompressionRatio)
                throw new InvalidDataException(
                    $"Archive rejected: entry '{entry.Name}' compression ratio {ratio:F1} exceeds limit " +
                    $"{ZipImportRules.MaxEntryCompressionRatio:F1}.");
        }

        totalUncompressed += Math.Max(0, entry.Size);
        if (totalUncompressed > ZipImportRules.MaxTotalUncompressedBytes)
            throw new InvalidDataException(
                $"Archive rejected: total uncompressed size {totalUncompressed} exceeds limit " +
                $"{ZipImportRules.MaxTotalUncompressedBytes}.");
    }

    private static string NormalizeManifestPath(string entryName)
    {
        return entryName.Replace('\\', '/');
    }

    private static string FormatUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture);
    }

    private static RecipeEntry ReadEntry(SqliteDataReader reader)
    {
        return new RecipeEntry(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            DateTimeOffset.Parse(reader.GetString(4),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
    }

    private void EnsureCreated()
    {
        using var conn = _builder.OpenConnection();
        using var cmd = conn.CreateCommand();
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