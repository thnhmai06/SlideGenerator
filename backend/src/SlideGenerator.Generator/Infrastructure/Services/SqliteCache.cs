/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: SqliteCache.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using SlideGenerator.Cloud.Models;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Rules;
using SlideGenerator.Settings.Rules;
using SlideGenerator.Utilities;
using static SlideGenerator.Settings.Rules.NameAndPaths.DataFolder.CacheFile.TableNames;

namespace SlideGenerator.Generator.Infrastructure.Services;

/// <summary>
///     SQLite-backed implementation of <see cref="ICache" />. Creates a short-lived connection per
///     batch operation; no shared long-lived connection. The two parts (<see cref="InspectedUrls" />,
///     <see cref="DownloadedFiles" />) share the same underlying <see cref="Storage" /> (connection
///     builder and TTL/size maintenance), but implement their own table-specific read/write logic.
///     Every public method is batch-shaped (<c>*ManyAsync</c>): callers gather every key up front and
///     issue a single SELECT/transaction instead of one round-trip per key.
/// </summary>
internal sealed class SqliteCache : ICache
{
    /// <inheritdoc />
    public IInspectedUrlCache InspectedUrls { get; }

    /// <inheritdoc />
    public IDownloadedFileCache DownloadedFiles { get; }

    /// <summary>Uses the app-wide shared cache database configured in the Settings module.</summary>
    public SqliteCache()
        : this(new SqliteConnectionStringBuilder(NameAndPaths.DataFolder.CacheFile.ConnectionString))
    {
    }

    /// <summary>Ensures the database schema exists using a one-shot connection.</summary>
    internal SqliteCache(SqliteConnectionStringBuilder builder,
        long maxSizeBytesOverride = CacheEvictionRules.MaxSizeBytes)
    {
        var storage = new Storage(builder, maxSizeBytesOverride);
        InspectedUrls = new InspectedUrlCache(storage);
        DownloadedFiles = new DownloadedFileCache(storage);
    }

    private sealed class InspectedUrlCache(Storage storage) : IInspectedUrlCache
    {
        public async Task<IReadOnlyDictionary<string, ContentInfo?>> GetOrUpdateManyAsync(
            IReadOnlyCollection<string> sources,
            Func<string, CancellationToken, Task<(bool Cacheable, ContentInfo? Value)>> factory,
            CancellationToken ct)
        {
            var result = new Dictionary<string, ContentInfo?>();
            if (sources.Count == 0) return result;

            var hashBySource = sources.Distinct().ToDictionary(s => s, s => s.HashText());

            await using (var conn = await storage.OpenConnectionAsync(ct).ConfigureAwait(false))
            {
                var hashes = hashBySource.Values.ToList();
                var rows = await conn.QueryAsync<(string KeyHash, string? Value, string Timestamp)>(
                    new CommandDefinition(
                        $"SELECT KeyHash, Value, Timestamp FROM {InspectedUrlsTable} WHERE KeyHash IN @hashes",
                        new { hashes }, cancellationToken: ct)).ConfigureAwait(false);
                var rowsByHash = rows.ToDictionary(r => r.KeyHash,
                    r => (r.Value, Timestamp: DateTimeOffset.Parse(r.Timestamp)));

                var now = DateTimeOffset.UtcNow;
                foreach (var (source, hash) in hashBySource)
                    if (rowsByHash.TryGetValue(hash, out var row) && !CacheEvictionRules.IsExpired(row.Timestamp, now))
                        result[source] = row.Value is null ? null : JsonSerializer.Deserialize<ContentInfo>(row.Value);
            }

            var misses = hashBySource.Keys.Where(s => !result.ContainsKey(s)).ToList();
            if (misses.Count == 0) return result;

            var toUpsert = new List<(string Hash, ContentInfo? Value)>();
            foreach (var source in misses)
            {
                var (cacheable, value) = await factory(source, ct).ConfigureAwait(false);
                if (!cacheable) continue;
                result[source] = value;
                toUpsert.Add((hashBySource[source], value));
            }

            if (toUpsert.Count > 0)
            {
                await storage.UpsertManyAsync(InspectedUrlsTable,
                    toUpsert.Select(e => (e.Hash, e.Value is null ? null : JsonSerializer.Serialize(e.Value))).ToList(),
                    ct).ConfigureAwait(false);
                await storage.MaintainAsync(InspectedUrlsTable, ct).ConfigureAwait(false);
            }

            return result;
        }
    }

    private sealed class DownloadedFileCache(Storage storage) : IDownloadedFileCache
    {
        public async Task TouchManyAsync(
            IReadOnlyCollection<(Uri ResolvedUri, string? Extension)> entries, CancellationToken ct)
        {
            if (entries.Count == 0) return;

            var toUpsert = entries
                .GroupBy(e => e.ResolvedUri)
                .Select(g => (g.Key.AbsoluteUri.HashText(), g.First().Extension))
                .ToList();

            await storage.UpsertManyAsync(DownloadedFilesTable, toUpsert, ct).ConfigureAwait(false);
            await storage.MaintainAsync(DownloadedFilesTable, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Low-level SQLite plumbing shared by <see cref="InspectedUrlCache" /> and
    ///     <see cref="DownloadedFileCache" />: connection management, schema creation, batched upsert,
    ///     and TTL/size maintenance. Neither component owns a connection of its own.
    /// </summary>
    private sealed class Storage
    {
        private readonly SqliteConnectionStringBuilder _builder;
        private readonly long _maxSizeBytesOverride;
        private long _writeCounter;

        public Storage(SqliteConnectionStringBuilder builder, long maxSizeBytesOverride)
        {
            _builder = builder;
            _maxSizeBytesOverride = maxSizeBytesOverride;
            DbEnsureCreated();
        }

        /// <summary>Upserts every (KeyHash, Value) pair in <paramref name="entries" /> in a single transaction.</summary>
        public async Task UpsertManyAsync(
            string table, IReadOnlyList<(string KeyHash, string? Value)> entries, CancellationToken ct)
        {
            if (entries.Count == 0) return;

            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            await conn.ExecuteAsync(new CommandDefinition(
                $"""
                 INSERT INTO {table} (KeyHash, Value, Timestamp) VALUES (@h, @v, @now)
                 ON CONFLICT(KeyHash) DO UPDATE SET Value = excluded.Value, Timestamp = excluded.Timestamp
                 """,
                entries.Select(e => new { h = e.KeyHash, v = e.Value, now }),
                tx, cancellationToken: ct)).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }

        /// <summary>Sweeps expired rows for <paramref name="table" /> every write; throttles the size/VACUUM check.</summary>
        public async Task MaintainAsync(string table, CancellationToken ct)
        {
            var cutoff = CacheEvictionRules.ExpiryCutoff(DateTimeOffset.UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await using (var conn = await OpenConnectionAsync(ct).ConfigureAwait(false))
            {
                if (table == DownloadedFilesTable)
                    await CleanupDownloadedFilesAsync(conn, "Timestamp < @cutoff",
                        new Dictionary<string, object> { ["cutoff"] = cutoff }, ct).ConfigureAwait(false);

                await conn.ExecuteAsync(new CommandDefinition(
                    $"DELETE FROM {table} WHERE Timestamp < @cutoff",
                    new { cutoff }, cancellationToken: ct)).ConfigureAwait(false);
            }

            if (Interlocked.Increment(ref _writeCounter) % 50 != 0) return;

            var sizeBytes = await GetDatabaseSizeBytesAsync(ct).ConfigureAwait(false);
            if (sizeBytes < _maxSizeBytesOverride) return;

            foreach (var evictTable in new[] { InspectedUrlsTable, DownloadedFilesTable })
                await EvictOldestAsync(evictTable, ct).ConfigureAwait(false);

            await using var vacuumConn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await vacuumConn.ExecuteAsync(new CommandDefinition("VACUUM", cancellationToken: ct)).ConfigureAwait(false);
        }

        private async Task<long> GetDatabaseSizeBytesAsync(CancellationToken ct)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            // WAL mode defers growth to the -wal file; checkpoint first so page_count reflects it.
            await conn.ExecuteAsync(new CommandDefinition("PRAGMA wal_checkpoint(TRUNCATE);", cancellationToken: ct))
                .ConfigureAwait(false);

            return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
                "SELECT page_count * page_size FROM pragma_page_count(), pragma_page_size()",
                cancellationToken: ct)).ConfigureAwait(false);
        }

        private async Task EvictOldestAsync(string table, CancellationToken ct)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            var rowCount = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
                $"SELECT COUNT(*) FROM {table}", cancellationToken: ct)).ConfigureAwait(false);

            var toEvict = CacheEvictionRules.RowsToEvict(rowCount);
            if (toEvict <= 0) return;

            if (table == DownloadedFilesTable)
                await CleanupDownloadedFilesAsync(conn,
                    $"KeyHash IN (SELECT KeyHash FROM {table} ORDER BY Timestamp ASC LIMIT @n)",
                    new Dictionary<string, object> { ["n"] = toEvict }, ct).ConfigureAwait(false);

            await conn.ExecuteAsync(new CommandDefinition(
                $"""
                 DELETE FROM {table} WHERE KeyHash IN (
                     SELECT KeyHash FROM {table} ORDER BY Timestamp ASC LIMIT @n
                 )
                 """,
                new { n = toEvict }, cancellationToken: ct)).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the physical temp-cached files for <c>DownloadedFiles</c> rows matching
        ///     <paramref name="whereSql" /> — used before evicting/expiring rows so no orphaned file is left behind.
        /// </summary>
        private static async Task CleanupDownloadedFilesAsync(
            SqliteConnection conn, string whereSql, IReadOnlyDictionary<string, object> parameters,
            CancellationToken ct)
        {
            var dynParams = new DynamicParameters();
            foreach (var (name, value) in parameters) dynParams.Add(name, value);

            var rows = await conn.QueryAsync<(string KeyHash, string? Value)>(new CommandDefinition(
                $"SELECT KeyHash, Value FROM {DownloadedFilesTable} WHERE {whereSql}",
                dynParams, cancellationToken: ct)).ConfigureAwait(false);

            var paths = rows.Select(r => Path.Combine(NameAndPaths.TempFolder.RootPath, $"{r.KeyHash}{r.Value}"));
            foreach (var path in paths.Where(File.Exists)) File.Delete(path);
        }

        private void DbEnsureCreated()
        {
            using var conn = OpenConnection();
            conn.Execute("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");

            foreach (var table in new[] { InspectedUrlsTable, DownloadedFilesTable })
                conn.Execute($"""
                              CREATE TABLE IF NOT EXISTS {table} (
                                  KeyHash   TEXT PRIMARY KEY,
                                  Value     TEXT NULL,
                                  Timestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
                              );
                              """);
        }

        public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
        {
            var conn = new SqliteConnection(_builder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await conn.ExecuteAsync(new CommandDefinition("PRAGMA busy_timeout=5000;", cancellationToken: ct))
                .ConfigureAwait(false);
            return conn;
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_builder.ConnectionString);
            conn.Open();
            return conn;
        }
    }
}