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
using Dapper;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Generator.Jobs.Models;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     Store for the <c>Workflows</c> table: one row per workflow (the execution of one <see cref="Request" />),
///     holding the request it came from, its log file and its lifecycle status. Rows are written once and only
///     their status changes afterwards, so — unlike <see cref="IJobsRepository" /> — nothing here is buffered.
/// </summary>
public interface IWorkflowsRepository
{
    /// <summary>Inserts a new workflow row.</summary>
    Task CreateAsync(WorkflowRecord record, CancellationToken ct = default);

    /// <summary>Gets the workflow row for <paramref name="workflowId" />, or <see langword="null" /> if absent.</summary>
    Task<WorkflowRecord?> GetAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    ///     Gets every workflow still <see cref="JobStatus.Pending" />, <see cref="JobStatus.Running" /> or
    ///     <see cref="JobStatus.Paused" /> — at startup, by definition crash leftovers to resume.
    /// </summary>
    Task<IReadOnlyList<WorkflowRecord>> GetNonTerminalAsync(CancellationToken ct = default);

    /// <summary>Updates the lifecycle status of <paramref name="workflowId" />.</summary>
    Task UpdateStatusAsync(string workflowId, JobStatus status, CancellationToken ct = default);

    /// <summary>Deletes the workflow row for <paramref name="workflowId" />.</summary>
    Task DeleteAsync(string workflowId, CancellationToken ct = default);
}

/// <summary>SQLite-backed implementation of <see cref="IWorkflowsRepository" /> for the shared <c>Data.db</c>.</summary>
internal sealed class WorkflowsRepository(SqliteConnectionStringBuilder builder) : IWorkflowsRepository
{
    public async Task CreateAsync(WorkflowRecord record, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO Workflows (WorkflowId, RecipeId, Name, OutputType, SaveFolder, AllowLocalPaths, LogPath, CreatedAt, Status)
            VALUES (@WorkflowId, @RecipeId, @Name, @OutputType, @SaveFolder, @AllowLocalPaths, @LogPath, @CreatedAt, @Status)
            """,
            new
            {
                record.WorkflowId,
                record.Request.RecipeId,
                record.Request.Name,
                OutputType = record.Request.OutputType.ToString(),
                record.Request.SaveFolder,
                AllowLocalPaths = record.Request.AllowLocalPaths ? 1 : 0,
                record.LogPath,
                CreatedAt = DbFormatUtc(record.CreatedAt),
                Status = record.Status.ToString()
            }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<WorkflowRecord?> GetAsync(string workflowId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var row = await conn.QuerySingleOrDefaultAsync<WorkflowRow>(new CommandDefinition(
                "SELECT * FROM Workflows WHERE WorkflowId = @workflowId", new { workflowId }, cancellationToken: ct))
            .ConfigureAwait(false);
        return row is null ? null : FromRow(row);
    }

    public async Task<IReadOnlyList<WorkflowRecord>> GetNonTerminalAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<WorkflowRow>(new CommandDefinition(
            "SELECT * FROM Workflows WHERE Status IN (@pending, @running, @paused)",
            new
            {
                pending = JobStatus.Pending.ToString(),
                running = JobStatus.Running.ToString(),
                paused = JobStatus.Paused.ToString()
            }, cancellationToken: ct)).ConfigureAwait(false);
        return [.. rows.Select(FromRow)];
    }

    public async Task UpdateStatusAsync(string workflowId, JobStatus status, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE Workflows SET Status = @status WHERE WorkflowId = @workflowId",
                new { workflowId, status = status.ToString() }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(string workflowId, CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Workflows WHERE WorkflowId = @workflowId", new { workflowId }, cancellationToken: ct))
            .ConfigureAwait(false);
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
        string WorkflowId,
        long RecipeId,
        string Name,
        string OutputType,
        string SaveFolder,
        long AllowLocalPaths,
        string LogPath,
        string CreatedAt,
        string Status);

    private static WorkflowRecord FromRow(WorkflowRow row)
    {
        return new WorkflowRecord(
            row.WorkflowId,
            new Request(
                (int)row.RecipeId,
                row.Name,
                Enum.Parse<PresentationType>(row.OutputType),
                row.SaveFolder,
                row.AllowLocalPaths != 0),
            row.LogPath,
            DbParseUtc(row.CreatedAt),
            Enum.Parse<JobStatus>(row.Status));
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
