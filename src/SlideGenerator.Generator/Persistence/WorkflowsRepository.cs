/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowsRepository.cs
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
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     Buffered current-state store for the <c>Workflows</c> table — the only frequently-changing table left
///     (Requests are write-once, Recipes barely change). Writers call <see cref="Enqueue" /> as often as
///     they like; a background loop coalesces and flushes to SQLite roughly once per second.
/// </summary>
public interface IWorkflowsRepository
{
    /// <summary>Marks <paramref name="snapshot" /> dirty for the next flush — last write for a given key wins.</summary>
    void Enqueue(WorkflowSnapshot snapshot);

    /// <summary>Flushes every dirty record to storage immediately, bypassing the ~1s tick.</summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>Raised after each successful flush with the batch that was just persisted.</summary>
    event Action<IReadOnlyList<WorkflowSnapshot>>? Flushed;

    /// <summary>Gets every workflow belonging to <paramref name="requestId" />, ordered by workflow id.</summary>
    Task<IReadOnlyList<WorkflowSnapshot>> GetByRequestIdAsync(string requestId, CancellationToken ct = default);

    /// <summary>Gets every workflow across all requests whose status is not yet terminal (for crash recovery).</summary>
    Task<IReadOnlyList<WorkflowSnapshot>> GetNonTerminalAsync(CancellationToken ct = default);

    /// <summary>Gets every workflow row across every request, regardless of status (for request-group listing).</summary>
    Task<IReadOnlyList<WorkflowSnapshot>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes every workflow row belonging to <paramref name="requestId" />.</summary>
    Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default);
}

/// <summary>
///     Buffered SQLite-backed implementation of <see cref="IWorkflowsRepository" /> for the <c>Workflows</c> table —
///     the only table in the shared database that changes on every row/phase transition. Every field of
///     <see cref="WorkflowSpecification" /> gets an explicit column; only the free-form nested lists
///     (<c>UsedColumns</c>/<c>TextInstructions</c>/<c>ImageInstructions</c>) are stored as named JSON
///     columns — see <c>001_2.0.0.sql</c> (<see cref="SlideGenerator.Settings.Database.DatabaseMigrator" />) for the
///     original schema, and <c>002_add-total-rows-to-jobs.sql</c> for the <c>TotalRows</c> column added later
///     (an existing <c>Data.db</c> only gets a new column via a follow-up migration — <c>CREATE TABLE IF NOT
///     EXISTS</c> alone would silently no-op on a database that already has the table).
/// </summary>
internal sealed class WorkflowsRepository(SqliteConnectionStringBuilder builder, ILogger<WorkflowsRepository> logger)
    : BufferedRepository<(string RequestId, int WorkflowId), WorkflowSnapshot>(logger), IWorkflowsRepository
{
    /// <inheritdoc cref="IWorkflowsRepository.Enqueue" />
    public void Enqueue(WorkflowSnapshot snapshot)
    {
        Enqueue((snapshot.RequestId, snapshot.WorkflowId), snapshot);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowSnapshot>> GetByRequestIdAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<WorkflowRow>(new CommandDefinition(
            "SELECT * FROM Workflows WHERE RequestId = @requestId ORDER BY WorkflowId",
            new { requestId }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowSnapshot>> GetNonTerminalAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<WorkflowRow>(new CommandDefinition(
            "SELECT * FROM Workflows WHERE Status IN (@pending, @running, @paused)",
            new
            {
                pending = WorkflowStatus.Pending.ToString(),
                running = WorkflowStatus.Running.ToString(),
                paused = WorkflowStatus.Paused.ToString()
            }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowSnapshot>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<WorkflowRow>(new CommandDefinition(
            "SELECT * FROM Workflows ORDER BY RequestId, WorkflowId", cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    /// <inheritdoc />
    public async Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Workflows WHERE RequestId = @requestId", new { requestId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task UpsertBatchAsync(IReadOnlyList<WorkflowSnapshot> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO Workflows (
                RequestId, WorkflowId, Status, Phase, CurrentIndex,
                WorkbookPath, WorksheetName, UsedColumnsJson,
                RowFilterType, RowFilterStart, RowFilterEnd, RowFilterPartitionIndex, RowFilterPartitionCount,
                TemplatePresentationPath, TemplateSlideIndex, TextInstructionsJson, ImageInstructionsJson,
                OutputPath, Timestamp, TotalRows
            ) VALUES (
                @RequestId, @WorkflowId, @Status, @Phase, @CurrentIndex,
                @WorkbookPath, @WorksheetName, @UsedColumnsJson,
                @RowFilterType, @RowFilterStart, @RowFilterEnd, @RowFilterPartitionIndex, @RowFilterPartitionCount,
                @TemplatePresentationPath, @TemplateSlideIndex, @TextInstructionsJson, @ImageInstructionsJson,
                @OutputPath, @Timestamp, @TotalRows
            )
            ON CONFLICT(RequestId, WorkflowId) DO UPDATE SET
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

    private sealed record WorkflowRow(
        string RequestId,
        long WorkflowId,
        string Status,
        string Phase,
        long CurrentIndex,
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
        string Timestamp,
        long? TotalRows);

    private static object ToRow(WorkflowSnapshot r)
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
            r.RequestId,
            r.WorkflowId,
            Status = r.WorkflowStatus.ToString(),
            Phase = r.Phase.ToString(),
            r.CurrentIndex,
            spec.WorkbookPath,
            spec.WorksheetName,
            UsedColumnsJson = spec.UsedColumns is null
                ? null
                : JsonSerializer.Serialize(spec.UsedColumns, WorkflowSpecificationJson.Options),
            RowFilterType = rowFilterType,
            RowFilterStart = start,
            RowFilterEnd = end,
            RowFilterPartitionIndex = pIdx,
            RowFilterPartitionCount = pCount,
            spec.TemplatePresentationPath,
            spec.TemplateSlideIndex,
            TextInstructionsJson = JsonSerializer.Serialize(spec.TextInstructions, WorkflowSpecificationJson.Options),
            ImageInstructionsJson = JsonSerializer.Serialize(spec.ImageInstructions, WorkflowSpecificationJson.Options),
            spec.OutputPath,
            Timestamp = DbFormatUtc(r.Timestamp),
            r.TotalRows
        };
    }

    private static WorkflowSnapshot FromRow(WorkflowRow row)
    {
        RowFilter? rowFilter = row.RowFilterType switch
        {
            "IndexRange" => new IndexRangeFilter((int)row.RowFilterStart!.Value, (int)row.RowFilterEnd!.Value),
            "PartitionBlock" => new PartitionBlockFilter((int)row.RowFilterPartitionIndex!.Value,
                (int)row.RowFilterPartitionCount!.Value),
            _ => null
        };

        var spec = new WorkflowSpecification(
            row.WorkbookPath,
            row.WorksheetName,
            row.UsedColumnsJson is null
                ? null
                : JsonSerializer.Deserialize<IReadOnlySet<ColumnIdentifier>>(row.UsedColumnsJson,
                    WorkflowSpecificationJson.Options),
            rowFilter,
            row.TemplatePresentationPath,
            (int)row.TemplateSlideIndex,
            JsonSerializer.Deserialize<IReadOnlyList<TextInstruction>>(row.TextInstructionsJson,
                WorkflowSpecificationJson.Options) ?? [],
            JsonSerializer.Deserialize<IReadOnlyList<ImageInstruction>>(row.ImageInstructionsJson,
                WorkflowSpecificationJson.Options) ?? [],
            row.OutputPath);

        return new WorkflowSnapshot(
            row.RequestId,
            (int)row.WorkflowId,
            Enum.Parse<WorkflowStatus>(row.Status),
            Enum.Parse<WorkflowPhase>(row.Phase),
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