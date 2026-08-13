/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: RequestsRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Slides;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     Unbuffered store for the <c>Requests</c> table — written once at request creation and read back
///     for <c>Summary</c>/resume; never updated afterward, so no buffering is needed (mirrors
///     <c>RecipeRepository</c>'s direct-connection-per-operation pattern).
/// </summary>
public interface IRequestsRepository
{
    /// <summary>Inserts a new request row.</summary>
    Task CreateAsync(RequestRecord record, CancellationToken ct = default);

    /// <summary>Gets a request row by id, or <see langword="null" /> if not found.</summary>
    Task<RequestRecord?> GetAsync(string requestId, CancellationToken ct = default);

    /// <summary>Deletes a request row.</summary>
    Task DeleteAsync(string requestId, CancellationToken ct = default);
}

/// <summary>
///     Unbuffered SQLite-backed implementation of <see cref="IRequestsRepository" />. Mirrors
///     <c>RecipeRepository</c>'s short-lived-connection-per-operation pattern — no buffering, since a
///     request row is written once at creation and never updated.
/// </summary>
internal sealed class RequestsRepository : IRequestsRepository
{
    private readonly SqliteConnectionStringBuilder _builder;

    public RequestsRepository(SqliteConnectionStringBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public async Task CreateAsync(RequestRecord record, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO Requests (RequestId, RecipeId, Name, OutputType, SaveFolder, AllowLocalPaths, LogPath, CreatedAt)
            VALUES (@RequestId, @RecipeId, @Name, @OutputType, @SaveFolder, @AllowLocalPaths, @LogPath, @CreatedAt)
            """,
            new
            {
                record.RequestId,
                record.Request.RecipeId,
                record.Request.Name,
                OutputType = record.Request.OutputType.ToString(),
                record.Request.SaveFolder,
                AllowLocalPaths = record.Request.AllowLocalPaths ? 1 : 0,
                record.LogPath,
                CreatedAt = DbFormatUtc(record.CreatedAt)
            }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequestRecord?> GetAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var row = await conn.QuerySingleOrDefaultAsync<RequestRow>(new CommandDefinition(
            "SELECT * FROM Requests WHERE RequestId = @requestId", new { requestId }, cancellationToken: ct))
            .ConfigureAwait(false);
        return row is null ? null : FromRow(row);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Requests WHERE RequestId = @requestId", new { requestId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    #region Row mapping

    private sealed record RequestRow(
        string RequestId, long RecipeId, string Name, string OutputType, string SaveFolder,
        long AllowLocalPaths, string LogPath, string CreatedAt);

    private static RequestRecord FromRow(RequestRow row) => new(
        row.RequestId,
        new Request(
            (int)row.RecipeId,
            row.Name,
            System.Enum.Parse<PresentationType>(row.OutputType),
            row.SaveFolder,
            row.AllowLocalPaths != 0),
        row.LogPath,
        DbParseUtc(row.CreatedAt));

    private static string DbFormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static DateTimeOffset DbParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    #endregion

    #region Connection / schema

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(_builder.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    #endregion
}
