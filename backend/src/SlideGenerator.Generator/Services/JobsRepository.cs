/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobsRepository.cs
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
using Microsoft.Extensions.Logging;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Adapters;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Generator.Models.Enum;
using SlideGenerator.Recipe.Models.Components;

namespace SlideGenerator.Generator.Services;

/// <summary>
///     Buffered SQLite-backed implementation of <see cref="IJobsRepository" /> for the <c>Jobs</c> table —
///     the only table in the shared database that changes on every row/phase transition. Every field of
///     <see cref="JobSpecification" /> gets an explicit column; only the free-form nested lists
///     (<c>UsedColumns</c>/<c>TextInstructions</c>/<c>ImageInstructions</c>) are stored as named JSON
///     columns — see the schema comment on <see cref="DbEnsureCreated" /> for the reasoning.
/// </summary>
internal sealed class JobsRepository : BufferedRepository<(string RequestId, int JobId), JobRecord>, IJobsRepository
{
    private readonly SqliteConnectionStringBuilder _builder;

    public JobsRepository(SqliteConnectionStringBuilder builder, ILogger<JobsRepository> logger) : base(logger)
    {
        _builder = builder;
        DbEnsureCreated();
    }

    /// <inheritdoc cref="IJobsRepository.Enqueue" />
    public void Enqueue(JobRecord record) => Enqueue((record.RequestId, record.JobId), record);

    /// <inheritdoc />
    protected override async Task UpsertBatchAsync(IReadOnlyList<JobRecord> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO Jobs (
                RequestId, JobId, Status, Phase, CurrentIndex,
                WorkbookPath, WorksheetName, UsedColumnsJson,
                RowFilterType, RowFilterStart, RowFilterEnd, RowFilterPartitionIndex, RowFilterPartitionCount,
                TemplatePresentationPath, TemplateSlideIndex, TextInstructionsJson, ImageInstructionsJson,
                OutputPath, Timestamp
            ) VALUES (
                @RequestId, @JobId, @Status, @Phase, @CurrentIndex,
                @WorkbookPath, @WorksheetName, @UsedColumnsJson,
                @RowFilterType, @RowFilterStart, @RowFilterEnd, @RowFilterPartitionIndex, @RowFilterPartitionCount,
                @TemplatePresentationPath, @TemplateSlideIndex, @TextInstructionsJson, @ImageInstructionsJson,
                @OutputPath, @Timestamp
            )
            ON CONFLICT(RequestId, JobId) DO UPDATE SET
                Status = excluded.Status, Phase = excluded.Phase, CurrentIndex = excluded.CurrentIndex,
                OutputPath = excluded.OutputPath, Timestamp = excluded.Timestamp
            """,
            batch.Select(ToRow), tx, cancellationToken: ct)).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobRecord>> GetByRequestIdAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JobRow>(new CommandDefinition(
            "SELECT * FROM Jobs WHERE RequestId = @requestId ORDER BY JobId",
            new { requestId }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobRecord>> GetNonTerminalAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JobRow>(new CommandDefinition(
            "SELECT * FROM Jobs WHERE Status IN (@pending, @running, @paused)",
            new
            {
                pending = Status.Pending.ToString(),
                running = Status.Running.ToString(),
                paused = Status.Paused.ToString()
            }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JobRow>(new CommandDefinition(
            "SELECT * FROM Jobs ORDER BY RequestId, JobId", cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Jobs WHERE RequestId = @requestId", new { requestId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    #region Row mapping

    private sealed record JobRow(
        string RequestId, long JobId, string Status, string Phase, long CurrentIndex,
        string WorkbookPath, string WorksheetName, string? UsedColumnsJson,
        string? RowFilterType, long? RowFilterStart, long? RowFilterEnd,
        long? RowFilterPartitionIndex, long? RowFilterPartitionCount,
        string TemplatePresentationPath, long TemplateSlideIndex,
        string TextInstructionsJson, string ImageInstructionsJson,
        string OutputPath, string Timestamp);

    private static object ToRow(JobRecord r)
    {
        var spec = r.Specification;
        var rowFilterType = spec.RowFilter switch
        {
            null or AllRowFilter => (string?)null,
            IndexRangeFilter => "IndexRange",
            PartitionBlockFilter => "PartitionBlock",
            _ => throw new NotSupportedException($"Unsupported RowFilter type: {spec.RowFilter.GetType().Name}")
        };
        var (start, end) = spec.RowFilter is IndexRangeFilter irf ? (irf.Start, irf.End) : ((int?)null, (int?)null);
        var (pIdx, pCount) = spec.RowFilter is PartitionBlockFilter pbf
            ? (pbf.PartitionIndex, pbf.PartitionCount)
            : ((int?)null, (int?)null);

        return new
        {
            r.RequestId,
            r.JobId,
            Status = r.Status.ToString(),
            Phase = r.Phase.ToString(),
            r.CurrentIndex,
            spec.WorkbookPath,
            spec.WorksheetName,
            UsedColumnsJson = spec.UsedColumns is null ? null : JsonSerializer.Serialize(spec.UsedColumns, JobSpecificationJson.Options),
            RowFilterType = rowFilterType,
            RowFilterStart = start,
            RowFilterEnd = end,
            RowFilterPartitionIndex = pIdx,
            RowFilterPartitionCount = pCount,
            spec.TemplatePresentationPath,
            spec.TemplateSlideIndex,
            TextInstructionsJson = JsonSerializer.Serialize(spec.TextInstructions, JobSpecificationJson.Options),
            ImageInstructionsJson = JsonSerializer.Serialize(spec.ImageInstructions, JobSpecificationJson.Options),
            spec.OutputPath,
            Timestamp = DbFormatUtc(r.Timestamp)
        };
    }

    private static JobRecord FromRow(JobRow row)
    {
        RowFilter? rowFilter = row.RowFilterType switch
        {
            "IndexRange" => new IndexRangeFilter((int)row.RowFilterStart!.Value, (int)row.RowFilterEnd!.Value),
            "PartitionBlock" => new PartitionBlockFilter((int)row.RowFilterPartitionIndex!.Value, (int)row.RowFilterPartitionCount!.Value),
            _ => null
        };

        var spec = new JobSpecification(
            row.WorkbookPath,
            row.WorksheetName,
            row.UsedColumnsJson is null
                ? null
                : JsonSerializer.Deserialize<IReadOnlySet<ColumnIdentifier>>(row.UsedColumnsJson, JobSpecificationJson.Options),
            rowFilter,
            row.TemplatePresentationPath,
            (int)row.TemplateSlideIndex,
            JsonSerializer.Deserialize<IReadOnlyList<TextInstruction>>(row.TextInstructionsJson, JobSpecificationJson.Options) ?? [],
            JsonSerializer.Deserialize<IReadOnlyList<ImageInstruction>>(row.ImageInstructionsJson, JobSpecificationJson.Options) ?? [],
            row.OutputPath);

        return new JobRecord(
            row.RequestId,
            (int)row.JobId,
            System.Enum.Parse<Status>(row.Status),
            System.Enum.Parse<JobPhase>(row.Phase),
            (int)row.CurrentIndex,
            spec,
            DbParseUtc(row.Timestamp));
    }

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

    /// <summary>
    ///     Creates the <c>Jobs</c> table if missing. The 3 <c>*Json</c> columns are a deliberate, named
    ///     exception to "every field its own column": <c>UsedColumns</c>/<c>TextInstructions</c>/
    ///     <c>ImageInstructions</c> are variable-length lists of nested, sometimes-polymorphic objects
    ///     (e.g. one <c>ImageInstruction</c> carries a set of shapes, a list of columns, and an ordered
    ///     list of polymorphic ROI options) — splitting those into normalized child tables would only
    ///     serve a query pattern nobody uses (they're read once, whole, when a job runs), mirroring how
    ///     <c>Recipes</c> already stores an entire <see cref="Recipe.Models.Recipe" /> under one JSON column.
    ///     <see cref="RowFilter" /> stayed a handful of explicit columns instead, since it is a small,
    ///     closed set of 3 shapes.
    /// </summary>
    private void DbEnsureCreated()
    {
        using var conn = new SqliteConnection(_builder.ConnectionString);
        conn.Open();
        conn.Execute("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");
        conn.Execute("""
                     CREATE TABLE IF NOT EXISTS Jobs (
                         RequestId  TEXT NOT NULL,
                         JobId      INTEGER NOT NULL,
                         Status     TEXT NOT NULL,
                         Phase      TEXT NOT NULL DEFAULT 'CreatingOutput',
                         CurrentIndex INTEGER NOT NULL DEFAULT 0,

                         WorkbookPath   TEXT NOT NULL,
                         WorksheetName  TEXT NOT NULL,
                         UsedColumnsJson TEXT NULL,
                         RowFilterType  TEXT NULL,
                         RowFilterStart INTEGER NULL, RowFilterEnd INTEGER NULL,
                         RowFilterPartitionIndex INTEGER NULL, RowFilterPartitionCount INTEGER NULL,

                         TemplatePresentationPath TEXT NOT NULL,
                         TemplateSlideIndex       INTEGER NOT NULL,
                         TextInstructionsJson     TEXT NOT NULL,
                         ImageInstructionsJson    TEXT NOT NULL,

                         OutputPath TEXT NOT NULL,
                         Timestamp  TEXT NOT NULL,
                         PRIMARY KEY (RequestId, JobId)
                     );
                     """);
    }

    #endregion
}
