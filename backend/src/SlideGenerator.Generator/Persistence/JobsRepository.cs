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
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Job;
using SlideGenerator.Recipe.Mappings;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     Buffered current-state store for the <c>Jobs</c> table — the only frequently-changing table left
///     (Requests are write-once, Recipes barely change). Writers call <see cref="Enqueue" /> as often as
///     they like; a background loop coalesces and flushes to SQLite roughly once per second.
/// </summary>
public interface IJobsRepository
{
    /// <summary>Marks <paramref name="record" /> dirty for the next flush — last write for a given key wins.</summary>
    void Enqueue(JobRecord record);

    /// <summary>Flushes every dirty record to storage immediately, bypassing the ~1s tick.</summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>Raised after each successful flush with the batch that was just persisted.</summary>
    event Action<IReadOnlyList<JobRecord>>? Flushed;

    /// <summary>Gets every job belonging to <paramref name="requestId" />, ordered by job id.</summary>
    Task<IReadOnlyList<JobRecord>> GetByRequestIdAsync(string requestId, CancellationToken ct = default);

    /// <summary>Gets every job across all requests whose status is not yet terminal (for crash recovery).</summary>
    Task<IReadOnlyList<JobRecord>> GetNonTerminalAsync(CancellationToken ct = default);

    /// <summary>Gets every job row across every request, regardless of status (for request-group listing).</summary>
    Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes every job row belonging to <paramref name="requestId" />.</summary>
    Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default);
}

/// <summary>
///     Buffered SQLite-backed implementation of <see cref="IJobsRepository" /> for the <c>Jobs</c> table —
///     the only table in the shared database that changes on every row/phase transition. Every field of
///     <see cref="JobSpecification" /> gets an explicit column; only the free-form nested lists
///     (<c>UsedColumns</c>/<c>TextInstructions</c>/<c>ImageInstructions</c>) are stored as named JSON
///     columns — see <c>0003_CreateJobs.sql</c> (<see cref="SlideGenerator.Settings.Database.DatabaseMigrator" />) for the schema.
/// </summary>
internal sealed class JobsRepository : BufferedRepository<(string RequestId, int JobId), JobRecord>, IJobsRepository
{
    private readonly SqliteConnectionStringBuilder _builder;

    public JobsRepository(SqliteConnectionStringBuilder builder, ILogger<JobsRepository> logger) : base(logger)
    {
        _builder = builder;
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
            Enum.Parse<Status>(row.Status),
            Enum.Parse<JobPhase>(row.Phase),
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

    #endregion
}
