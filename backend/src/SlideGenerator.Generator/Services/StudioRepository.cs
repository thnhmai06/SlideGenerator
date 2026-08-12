/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: StudioRepository.cs
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
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Generator.Models.Enum;
using SlideGenerator.Settings.Rules;
using static SlideGenerator.Settings.Rules.NameAndPaths.DataFolder.StudioFile.TableNames;

namespace SlideGenerator.Generator.Services;

/// <summary>
///     SQLite-backed implementation of <see cref="IStudioRepository" />. Creates a short-lived connection
///     per operation; no shared long-lived connection (mirrors <see cref="SqliteCache" />/<c>RecipeRepository</c>).
/// </summary>
internal sealed class StudioRepository : IStudioRepository
{
    private readonly SqliteConnectionStringBuilder _builder;

    /// <summary>Uses the app-wide Studio progress database configured in the Settings module.</summary>
    public StudioRepository() : this(new SqliteConnectionStringBuilder(NameAndPaths.DataFolder.StudioFile.ConnectionString))
    {
    }

    /// <summary>Ensures the database schema exists using a one-shot connection.</summary>
    internal StudioRepository(SqliteConnectionStringBuilder builder)
    {
        _builder = builder;
        DbEnsureCreated();
    }

    /// <inheritdoc />
    public async Task UpsertRequestAsync(RequestProgress progress, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            $"""
             INSERT INTO {RequestsTable} (RequestId, Phase, Timestamp) VALUES (@RequestId, @Phase, @Timestamp)
             ON CONFLICT(RequestId) DO UPDATE SET Phase = excluded.Phase, Timestamp = excluded.Timestamp
             """,
            new { progress.RequestId, Phase = progress.Phase.ToString(), Timestamp = DbFormatUtc(progress.Timestamp) },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertJobsAsync(IReadOnlyList<JobProgress> progress, CancellationToken ct = default)
    {
        if (progress.Count == 0) return;

        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            $"""
             INSERT INTO {JobsTable} (RequestId, JobId, Status, Timestamp) VALUES (@RequestId, @JobId, @Status, @Timestamp)
             ON CONFLICT(RequestId, JobId) DO UPDATE SET Status = excluded.Status, Timestamp = excluded.Timestamp
             """,
            progress.Select(p => new { p.RequestId, p.JobId, Status = p.Status.ToString(), Timestamp = DbFormatUtc(p.Timestamp) }),
            tx, cancellationToken: ct)).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpsertRowsAsync(IReadOnlyList<RowProgress> progress, CancellationToken ct = default)
    {
        if (progress.Count == 0) return;

        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            $"""
             INSERT INTO {RowsTable} (RequestId, JobId, RowIndex, Status, Stage, Note, Timestamp)
             VALUES (@RequestId, @JobId, @RowIndex, @Status, @Stage, @Note, @Timestamp)
             ON CONFLICT(RequestId, JobId, RowIndex) DO UPDATE SET
                 Status = excluded.Status, Stage = excluded.Stage, Note = excluded.Note, Timestamp = excluded.Timestamp
             """,
            progress.Select(p => new
            {
                p.RequestId, p.JobId, p.RowIndex,
                Status = p.Status.ToString(), Stage = p.Stage.ToString(), p.Note,
                Timestamp = DbFormatUtc(p.Timestamp)
            }),
            tx, cancellationToken: ct)).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequestProgress?> GetRequestAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var row = await conn.QuerySingleOrDefaultAsync<(string RequestId, string Phase, string Timestamp)?>(
            new CommandDefinition(
                $"SELECT RequestId, Phase, Timestamp FROM {RequestsTable} WHERE RequestId = @requestId",
                new { requestId }, cancellationToken: ct)).ConfigureAwait(false);
        if (row is null) return null;

        return new RequestProgress
        {
            RequestId = row.Value.RequestId,
            Phase = System.Enum.Parse<RequestPhase>(row.Value.Phase),
            Timestamp = DbParseUtc(row.Value.Timestamp)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, JobProgress>> GetJobsAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<(string RequestId, string JobId, string Status, string Timestamp)>(
            new CommandDefinition(
                $"SELECT RequestId, JobId, Status, Timestamp FROM {JobsTable} WHERE RequestId = @requestId",
                new { requestId }, cancellationToken: ct)).ConfigureAwait(false);

        return rows.ToDictionary(r => r.JobId, r => new JobProgress
        {
            RequestId = r.RequestId,
            JobId = r.JobId,
            Status = System.Enum.Parse<Status>(r.Status),
            Timestamp = DbParseUtc(r.Timestamp)
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, RowProgress>> GetRowsAsync(string requestId, string jobId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<(string RequestId, string JobId, long RowIndex, string Status, string Stage, string? Note, string Timestamp)>(
            new CommandDefinition(
                $"SELECT RequestId, JobId, RowIndex, Status, Stage, Note, Timestamp FROM {RowsTable} " +
                "WHERE RequestId = @requestId AND JobId = @jobId",
                new { requestId, jobId }, cancellationToken: ct)).ConfigureAwait(false);

        return rows.ToDictionary(r => (int)r.RowIndex, r => new RowProgress
        {
            RequestId = r.RequestId,
            JobId = r.JobId,
            RowIndex = (int)r.RowIndex,
            Status = System.Enum.Parse<RowStatus>(r.Status),
            Stage = System.Enum.Parse<RowStage>(r.Stage),
            Note = r.Note,
            Timestamp = DbParseUtc(r.Timestamp)
        });
    }

    /// <inheritdoc />
    public async Task DeleteRequestAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        foreach (var table in new[] { RowsTable, JobsTable, RequestsTable })
            await conn.ExecuteAsync(new CommandDefinition(
                $"DELETE FROM {table} WHERE RequestId = @requestId", new { requestId }, tx, cancellationToken: ct))
                .ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    #region Helpers

    private static string DbFormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static DateTimeOffset DbParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(_builder.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_builder.ConnectionString);
        conn.Open();
        return conn;
    }

    private void DbEnsureCreated()
    {
        using var conn = OpenConnection();
        conn.Execute("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");
        conn.Execute($"""
                      CREATE TABLE IF NOT EXISTS {RequestsTable} (
                          RequestId TEXT PRIMARY KEY,
                          Phase     TEXT NOT NULL,
                          Timestamp TEXT NOT NULL
                      );
                      CREATE TABLE IF NOT EXISTS {JobsTable} (
                          RequestId TEXT NOT NULL,
                          JobId     TEXT NOT NULL,
                          Status    TEXT NOT NULL,
                          Timestamp TEXT NOT NULL,
                          PRIMARY KEY (RequestId, JobId)
                      );
                      CREATE TABLE IF NOT EXISTS {RowsTable} (
                          RequestId TEXT NOT NULL,
                          JobId     TEXT NOT NULL,
                          RowIndex  INTEGER NOT NULL,
                          Status    TEXT NOT NULL,
                          Stage     TEXT NOT NULL,
                          Note      TEXT NULL,
                          Timestamp TEXT NOT NULL,
                          PRIMARY KEY (RequestId, JobId, RowIndex)
                      );
                      """);
    }

    #endregion
}
