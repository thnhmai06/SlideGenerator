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
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     Buffered current-state store for the <c>Jobs</c> table — the only frequently-changing table left
///     (Requests are write-once, Recipes barely change). Writers call <see cref="Enqueue" /> as often as
///     they like; a background loop coalesces and flushes to SQLite roughly once per second.
/// </summary>
public interface IJobsRepository
{
    /// <summary>Marks <paramref name="snapshot" /> dirty for the next flush — last write for a given key wins.</summary>
    void Enqueue(JobSnapshot snapshot);

    /// <summary>Flushes every dirty record to storage immediately, bypassing the ~1s tick.</summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>Raised after each successful flush with the batch that was just persisted.</summary>
    event Action<IReadOnlyList<JobSnapshot>>? Flushed;

    /// <summary>Gets every job belonging to <paramref name="workflowId" />, ordered by job id.</summary>
    Task<IReadOnlyList<JobSnapshot>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);

    /// <summary>Gets every job across all requests whose status is not yet terminal (for crash recovery).</summary>
    Task<IReadOnlyList<JobSnapshot>> GetNonTerminalAsync(CancellationToken ct = default);

    /// <summary>Gets every job row across every request, regardless of status (for request-group listing).</summary>
    Task<IReadOnlyList<JobSnapshot>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes every job row belonging to <paramref name="workflowId" />.</summary>
    Task DeleteByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
}

/// <summary>
///     Buffered SQLite-backed implementation of <see cref="IJobsRepository" /> for the <c>Jobs</c> table —
///     the only table in the shared database that changes on every row/phase transition. Every field of
///     <see cref="JobSpecification" /> gets an explicit column; only the free-form nested lists
///     (<c>UsedColumns</c>/<c>TextInstructions</c>/<c>ImageInstructions</c>) are stored as named JSON
///     columns — see <c>001_2.0.0.sql</c> (<see cref="SlideGenerator.Settings.Database.DatabaseMigrator" />) for the
///     consolidated schema (including the <c>TotalRows</c> column)
///     (an existing <c>Data.db</c> only gets a new column via a follow-up migration — <c>CREATE TABLE IF NOT
///     EXISTS</c> alone would silently no-op on a database that already has the table).
/// </summary>
internal sealed class JobsRepository(SqliteConnectionStringBuilder builder, ILogger<JobsRepository> logger)
    : BufferedRepository<(string WorkflowId, int JobId), JobSnapshot>(logger), IJobsRepository
{
    /// <inheritdoc cref="IJobsRepository.Enqueue" />
    public void Enqueue(JobSnapshot snapshot)
    {
        Enqueue((snapshot.WorkflowId, snapshot.JobId), snapshot);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobSnapshot>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JobRow>(new CommandDefinition(
            "SELECT * FROM Jobs WHERE WorkflowId = @workflowId ORDER BY JobId",
            new { workflowId }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobSnapshot>> GetNonTerminalAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JobRow>(new CommandDefinition(
            "SELECT * FROM Jobs WHERE Status IN (@pending, @running, @paused)",
            new
            {
                pending = JobStatus.Pending.ToString(),
                running = JobStatus.Running.ToString(),
                paused = JobStatus.Paused.ToString()
            }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobSnapshot>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<JobRow>(new CommandDefinition(
            "SELECT * FROM Jobs ORDER BY WorkflowId, JobId", cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task DeleteByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Jobs WHERE WorkflowId = @workflowId", new { workflowId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task UpsertBatchAsync(IReadOnlyList<JobSnapshot> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO Jobs (
                WorkflowId, JobId, Status, Phase, CurrentIndex,
                WorkbookPath, WorksheetName, UsedColumnsJson,
                RowFilterType, RowFilterStart, RowFilterEnd, RowFilterPartitionIndex, RowFilterPartitionCount,
                TemplatePresentationPath, TemplateSlideIndex, TextInstructionsJson, ImageInstructionsJson,
                OutputPath, Timestamp, TotalRows
            ) VALUES (
                @WorkflowId, @JobId, @Status, @Phase, @CurrentIndex,
                @WorkbookPath, @WorksheetName, @UsedColumnsJson,
                @RowFilterType, @RowFilterStart, @RowFilterEnd, @RowFilterPartitionIndex, @RowFilterPartitionCount,
                @TemplatePresentationPath, @TemplateSlideIndex, @TextInstructionsJson, @ImageInstructionsJson,
                @OutputPath, @Timestamp, @TotalRows
            )
            ON CONFLICT(WorkflowId, JobId) DO UPDATE SET
                Status = excluded.Status, Phase = excluded.Phase, CurrentIndex = excluded.CurrentIndex,
                OutputPath = excluded.OutputPath, Timestamp = excluded.Timestamp, TotalRows = excluded.TotalRows
            """,
            batch.Select(ToRow), tx, cancellationToken: ct)).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    #region Connection / schema

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(builder.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    #endregion

    #region Row mapping

    // Dapper's DefaultTypeMap.FindConstructor matches parameters to the SELECT * column
    // order positionally — keep this list in the same order as the Jobs columns in 001_2.0.0.sql.
    private sealed record JobRow(
        string WorkflowId,
        long JobId,
        string Status,
        string Phase,
        long CurrentIndex,
        long? TotalRows,
        string WorkbookPath,
        string WorksheetName,
        string? UsedColumnsJson,
        string? RowFilterType,
        long? RowFilterStart,
        long? RowFilterEnd,
        long? RowFilterPartitionIndex,
        long? RowFilterPartitionCount,
        string TemplatePresentationPath,
        long TemplateSlideIndex,
        string TextInstructionsJson,
        string ImageInstructionsJson,
        string OutputPath,
        string Timestamp);

    private static object ToRow(JobSnapshot r)
    {
        var spec = r.Specification;
        var rowFilterType = spec.RowFilter switch
        {
            null or AllRowFilter => null,
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
            r.WorkflowId,
            r.JobId,
            Status = r.JobStatus.ToString(),
            Phase = r.Phase.ToString(),
            r.CurrentIndex,
            spec.WorkbookPath,
            spec.WorksheetName,
            UsedColumnsJson = spec.UsedColumns is null
                ? null
                : JsonSerializer.Serialize(spec.UsedColumns, JobSpecificationJson.Options),
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
            Timestamp = DbFormatUtc(r.Timestamp),
            r.TotalRows
        };
    }

    private static JobSnapshot FromRow(JobRow row)
    {
        RowFilter? rowFilter = row.RowFilterType switch
        {
            "IndexRange" => new IndexRangeFilter((int)row.RowFilterStart!.Value, (int)row.RowFilterEnd!.Value),
            "PartitionBlock" => new PartitionBlockFilter((int)row.RowFilterPartitionIndex!.Value,
                (int)row.RowFilterPartitionCount!.Value),
            _ => null
        };

        var spec = new JobSpecification(
            row.WorkbookPath,
            row.WorksheetName,
            row.UsedColumnsJson is null
                ? null
                : JsonSerializer.Deserialize<IReadOnlySet<ColumnIdentifier>>(row.UsedColumnsJson,
                    JobSpecificationJson.Options),
            rowFilter,
            row.TemplatePresentationPath,
            (int)row.TemplateSlideIndex,
            JsonSerializer.Deserialize<IReadOnlyList<TextInstruction>>(row.TextInstructionsJson,
                JobSpecificationJson.Options) ?? [],
            JsonSerializer.Deserialize<IReadOnlyList<ImageInstruction>>(row.ImageInstructionsJson,
                JobSpecificationJson.Options) ?? [],
            row.OutputPath);

        return new JobSnapshot(
            row.WorkflowId,
            (int)row.JobId,
            Enum.Parse<JobStatus>(row.Status),
            Enum.Parse<JobPhase>(row.Phase),
            (int)row.CurrentIndex,
            spec,
            DbParseUtc(row.Timestamp),
            (int?)row.TotalRows);
    }

    private static string DbFormatUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset DbParseUtc(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    #endregion
}