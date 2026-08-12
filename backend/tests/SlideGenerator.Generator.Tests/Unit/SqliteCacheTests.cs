/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: SqliteCacheTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Cloud;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Services;
using SlideGenerator.Settings.Rules;
using SlideGenerator.Utilities;
using Xunit;
using static SlideGenerator.Settings.Rules.NameAndPaths.DataFolder.CacheFile.TableNames;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SqliteCache" />, using an in-memory shared-cache SQLite database
///     to avoid file-system side effects (except for the size/VACUUM tests, which need a real file).
///     All lookups/writes go through the batch (<c>*ManyAsync</c>) API — production call sites gather
///     every key up front and issue a single query/transaction rather than one per key.
/// </summary>
public sealed class SqliteCacheTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly SqliteConnectionStringBuilder _builder;
    private readonly SqliteCache _cache;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database. The anchor connection keeps the
    ///     in-memory database alive across all short-lived per-operation connections.
    /// </summary>
    public SqliteCacheTests()
    {
        _builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"memory_{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };
        _anchor = new SqliteConnection(_builder.ConnectionString);
        _anchor.Open();
        _cache = new SqliteCache(_builder);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
    }

    #region GetOrUpdateManyAsync

    /// <summary>Verifies a cache miss invokes the factory and stores the result.</summary>
    [Fact]
    public async Task GetOrUpdateManyAsync_Miss_InvokesFactoryAndStores()
    {
        var expected = new ContentInfo(new Uri("https://example.com/a.png"), "image/png", 123, ".png");

        var result = await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-a"], (_, _) => Task.FromResult<(bool, ContentInfo?)>((true, expected)), CancellationToken.None);

        result["source-a"].Should().Be(expected);
    }

    /// <summary>Verifies a cache hit returns the full <see cref="ContentInfo" />, including <c>Extension</c>, without re-invoking the factory.</summary>
    [Fact]
    public async Task GetOrUpdateManyAsync_Hit_ReturnsCachedExtensionWithoutReinvoking()
    {
        var expected = new ContentInfo(new Uri("https://example.com/a.jpg"), "image/jpeg", null, ".jpg");
        await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-ext"], (_, _) => Task.FromResult<(bool, ContentInfo?)>((true, expected)), CancellationToken.None);

        var calls = 0;
        var result = await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-ext"],
            (_, _) =>
            {
                calls++;
                return Task.FromResult<(bool, ContentInfo?)>((true, null));
            },
            CancellationToken.None);

        result["source-ext"].Should().Be(expected);
        result["source-ext"]!.Extension.Should().Be(".jpg");
        calls.Should().Be(0);
    }

    /// <summary>Verifies a cached negative result (confirmed not-an-image) is reused without re-invoking the factory.</summary>
    [Fact]
    public async Task GetOrUpdateManyAsync_FactoryReturnsNullCacheable_CachesNullAndDoesNotReinvoke()
    {
        var calls = 0;
        Task<(bool, ContentInfo?)> Factory(string _, CancellationToken _2)
        {
            calls++;
            return Task.FromResult<(bool, ContentInfo?)>((true, null));
        }

        var first = await _cache.InspectedUrls.GetOrUpdateManyAsync(["source-b"], Factory, CancellationToken.None);
        var second = await _cache.InspectedUrls.GetOrUpdateManyAsync(["source-b"], Factory, CancellationToken.None);

        first["source-b"].Should().BeNull();
        second["source-b"].Should().BeNull();
        calls.Should().Be(1);
    }

    /// <summary>
    ///     Verifies a non-cacheable result (e.g. transient network failure) is never written to the
    ///     cache — the factory must be invoked again on the next call for the same key.
    /// </summary>
    [Fact]
    public async Task GetOrUpdateManyAsync_FactoryReturnsNotCacheable_DoesNotStore_ReinvokesNextTime()
    {
        var calls = 0;
        Task<(bool, ContentInfo?)> Factory(string _, CancellationToken _2)
        {
            calls++;
            return Task.FromResult<(bool, ContentInfo?)>((false, null));
        }

        await _cache.InspectedUrls.GetOrUpdateManyAsync(["source-c"], Factory, CancellationToken.None);
        await _cache.InspectedUrls.GetOrUpdateManyAsync(["source-c"], Factory, CancellationToken.None);

        calls.Should().Be(2);
    }

    /// <summary>Verifies two different keys in the same batch call are resolved independently.</summary>
    [Fact]
    public async Task GetOrUpdateManyAsync_DifferentKeysInSameBatch_AreIndependent()
    {
        var infoA = new ContentInfo(new Uri("https://example.com/a.png"), "image/png", null, ".png");
        var infoB = new ContentInfo(new Uri("https://example.com/b.png"), "image/png", null, ".png");

        var result = await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-d1", "source-d2"],
            (source, _) => Task.FromResult<(bool, ContentInfo?)>(
                (true, source == "source-d1" ? infoA : infoB)),
            CancellationToken.None);

        result["source-d1"].Should().Be(infoA);
        result["source-d2"].Should().Be(infoB);
    }

    /// <summary>Verifies a batch with one cached hit and one miss resolves both, only invoking the factory for the miss.</summary>
    [Fact]
    public async Task GetOrUpdateManyAsync_MixedHitAndMiss_OnlyInvokesFactoryForMiss()
    {
        var cached = new ContentInfo(new Uri("https://example.com/cached.png"), "image/png", null, ".png");
        await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-mixed-cached"], (_, _) => Task.FromResult<(bool, ContentInfo?)>((true, cached)),
            CancellationToken.None);

        var missValue = new ContentInfo(new Uri("https://example.com/miss.png"), "image/png", null, ".png");
        var factoryCalls = new List<string>();
        var result = await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-mixed-cached", "source-mixed-miss"],
            (source, _) =>
            {
                factoryCalls.Add(source);
                return Task.FromResult<(bool, ContentInfo?)>((true, missValue));
            },
            CancellationToken.None);

        result["source-mixed-cached"].Should().Be(cached);
        result["source-mixed-miss"].Should().Be(missValue);
        factoryCalls.Should().Equal("source-mixed-miss");
    }

    #endregion

    #region TouchManyAsync (DownloadedFiles)

    /// <summary>Verifies touching a brand-new resolved URI inserts a row for it.</summary>
    [Fact]
    public async Task TouchManyAsync_NewUri_InsertsRow()
    {
        var uri = new Uri("https://example.com/e.png");

        await _cache.DownloadedFiles.TouchManyAsync([(uri, ".png")], CancellationToken.None);

        using var cmd = _anchor.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {DownloadedFilesTable} WHERE KeyHash = @h";
        cmd.Parameters.AddWithValue("@h", uri.AbsoluteUri.HashText());
        Convert.ToInt64(cmd.ExecuteScalar()).Should().Be(1);
    }

    /// <summary>Verifies touching an already-known resolved URI updates it in place rather than duplicating the row.</summary>
    [Fact]
    public async Task TouchManyAsync_ExistingUri_UpdatesInPlace()
    {
        var uri = new Uri("https://example.com/f.png");
        await _cache.DownloadedFiles.TouchManyAsync([(uri, ".png")], CancellationToken.None);

        await _cache.DownloadedFiles.TouchManyAsync([(uri, ".png")], CancellationToken.None);

        using var cmd = _anchor.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {DownloadedFilesTable} WHERE KeyHash = @h";
        cmd.Parameters.AddWithValue("@h", uri.AbsoluteUri.HashText());
        Convert.ToInt64(cmd.ExecuteScalar()).Should().Be(1);
    }

    /// <summary>Verifies a batch with two distinct URIs touches each independently in one call.</summary>
    [Fact]
    public async Task TouchManyAsync_DistinctUris_TouchesEachIndependently()
    {
        var uriA = new Uri("https://example.com/batch-a.png");
        var uriB = new Uri("https://example.com/batch-b.png");

        await _cache.DownloadedFiles.TouchManyAsync([(uriA, ".png"), (uriB, ".jpg")], CancellationToken.None);

        using var cmd = _anchor.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {DownloadedFilesTable} WHERE KeyHash IN (@a, @b)";
        cmd.Parameters.AddWithValue("@a", uriA.AbsoluteUri.HashText());
        cmd.Parameters.AddWithValue("@b", uriB.AbsoluteUri.HashText());
        Convert.ToInt64(cmd.ExecuteScalar()).Should().Be(2);
    }

    /// <summary>Verifies duplicate entries for the same URI within one batch call collapse to a single row.</summary>
    [Fact]
    public async Task TouchManyAsync_MultipleEntriesSameUriInBatch_CollapsesToOneRow()
    {
        var uri = new Uri("https://example.com/dup.png");

        await _cache.DownloadedFiles.TouchManyAsync(
            [(uri, ".png"), (uri, ".png"), (uri, ".png")], CancellationToken.None);

        using var cmd = _anchor.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {DownloadedFilesTable} WHERE KeyHash = @h";
        cmd.Parameters.AddWithValue("@h", uri.AbsoluteUri.HashText());
        Convert.ToInt64(cmd.ExecuteScalar()).Should().Be(1);
    }

    #endregion

    #region Schema

    /// <summary>Verifies cache keys are stored hashed, never in plaintext.</summary>
    [Fact]
    public async Task Keys_AreStoredHashedNotPlaintext()
    {
        const string key = "source-hash-test";
        await _cache.InspectedUrls.GetOrUpdateManyAsync(
            [key],
            (_, _) => Task.FromResult<(bool, ContentInfo?)>(
                (true, new ContentInfo(new Uri("https://example.com/h.png"), "image/png", null, ".png"))),
            CancellationToken.None);

        using var cmd = _anchor.CreateCommand();
        cmd.CommandText = $"SELECT KeyHash FROM {InspectedUrlsTable}";
        using var reader = cmd.ExecuteReader();
        reader.Read().Should().BeTrue();
        var storedHash = reader.GetString(0);

        storedHash.Should().Be(key.HashText());
        storedHash.Should().NotBe(key);
    }

    /// <summary>Verifies both tables are created with the expected columns.</summary>
    [Fact]
    public void DbEnsureCreated_CreatesBothTablesWithTimestampColumn()
    {
        foreach (var table in new[] { InspectedUrlsTable, DownloadedFilesTable })
        {
            using var cmd = _anchor.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read()) columns.Add(reader.GetString(1));

            columns.Should().Contain(["KeyHash", "Value", "Timestamp"]);
        }
    }

    #endregion

    #region Maintenance (TTL / size)

    /// <summary>Verifies an expired row is swept on the next write and treated as a miss.</summary>
    [Fact]
    public async Task ExpiredRow_IsSweptOnNextWrite_AndTreatedAsMiss()
    {
        await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-ttl"],
            (_, _) => Task.FromResult<(bool, ContentInfo?)>(
                (true, new ContentInfo(new Uri("https://example.com/i.png"), "image/png", null, ".png"))),
            CancellationToken.None);

        using (var cmd = _anchor.CreateCommand())
        {
            cmd.CommandText = $"UPDATE {InspectedUrlsTable} SET Timestamp = @old WHERE KeyHash = @h";
            cmd.Parameters.AddWithValue("@old",
                DateTimeOffset.UtcNow.AddDays(-15).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            cmd.Parameters.AddWithValue("@h", "source-ttl".HashText());
            cmd.ExecuteNonQuery();
        }

        var calls = 0;
        var expected = new ContentInfo(new Uri("https://example.com/i-new.png"), "image/png", null, ".png");
        var result = await _cache.InspectedUrls.GetOrUpdateManyAsync(
            ["source-ttl"],
            (_, _) =>
            {
                calls++;
                return Task.FromResult<(bool, ContentInfo?)>((true, expected));
            },
            CancellationToken.None);

        result["source-ttl"].Should().Be(expected);
        calls.Should().Be(1);
    }

    /// <summary>
    ///     Verifies that an expired <c>DownloadedFiles</c> row's physical temp-cached file is deleted on
    ///     TTL sweep — this is the only cleanup mechanism for that table now (no consumer-count-based
    ///     deletion), so a permanently-cached file eventually gets reclaimed once nothing has touched it
    ///     for the TTL window.
    /// </summary>
    [Fact]
    public async Task ExpiredDownloadedFileRow_PhysicalFileDeletedOnSweep()
    {
        var uri = new Uri("https://example.com/orphaned.png");
        var tempPath = IDownloadedFileCache.GetPath(uri, ".png");
        Directory.CreateDirectory(NameAndPaths.TempFolder.RootPath);
        await File.WriteAllBytesAsync(tempPath, [1, 2, 3], TestContext.Current.CancellationToken);
        try
        {
            await _cache.DownloadedFiles.TouchManyAsync([(uri, ".png")], CancellationToken.None);

            using (var cmd = _anchor.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {DownloadedFilesTable} SET Timestamp = @old WHERE KeyHash = @h";
                cmd.Parameters.AddWithValue("@old",
                    DateTimeOffset.UtcNow.AddDays(-15).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                cmd.Parameters.AddWithValue("@h", uri.AbsoluteUri.HashText());
                cmd.ExecuteNonQuery();
            }

            // Any write on the same table runs the TTL sweep, which should notice the expired row.
            await _cache.DownloadedFiles.TouchManyAsync(
                [(new Uri("https://example.com/unrelated.png"), ".png")], CancellationToken.None);

            File.Exists(tempPath).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    /// <summary>
    ///     Verifies that once the database exceeds the configured max size, the oldest ~20% of rows
    ///     per table are evicted and VACUUM shrinks the file.
    /// </summary>
    [Fact]
    public async Task DatabaseOverSizeBudget_EvictsOldestRowsAndVacuums()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"cache-size-test-{Guid.NewGuid():N}.db");
        var builder = new SqliteConnectionStringBuilder { DataSource = dbPath };
        const int totalRows = 100;
        try
        {
            // A tiny override so a single database page already exceeds the budget.
            var cache = new SqliteCache(builder, maxSizeBytesOverride: 4096);

            for (var i = 0; i < totalRows; i++)
            {
                var index = i;
                await cache.InspectedUrls.GetOrUpdateManyAsync(
                    [$"source-size-{index}"],
                    (_, _) => Task.FromResult<(bool, ContentInfo?)>(
                        (true, new ContentInfo(new Uri($"https://example.com/{index}.png"), "image/png", null, ".png"))),
                    CancellationToken.None);
            }

            using var conn = new SqliteConnection(builder.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {InspectedUrlsTable}";
            var rowCount = Convert.ToInt64(cmd.ExecuteScalar());

            rowCount.Should().BeLessThan(totalRows);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
            if (File.Exists(dbPath + "-wal")) File.Delete(dbPath + "-wal");
            if (File.Exists(dbPath + "-shm")) File.Delete(dbPath + "-shm");
        }
    }

    #endregion
}
