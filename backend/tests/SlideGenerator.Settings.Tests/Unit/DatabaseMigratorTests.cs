/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings.Tests
 * File: DatabaseMigratorTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Settings.Database;
using Xunit;

namespace SlideGenerator.Settings.Tests.Unit;

/// <summary>
///     Integration tests for <see cref="DatabaseMigrator" />, verifying it creates the <c>Recipes</c>/
///     <c>Requests</c>/<c>Jobs</c> tables against a real temp-file SQLite database and is idempotent
///     on a second run.
/// </summary>
public sealed class DatabaseMigratorTests : IDisposable
{
    private readonly string _dbPath;

    /// <summary>Points a fresh migrator run at a unique temp SQLite file for each test.</summary>
    public DatabaseMigratorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"DatabaseMigratorTests_{Guid.NewGuid():N}.db");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    /// <summary>Verifies that migrating a fresh database creates all 3 expected tables.</summary>
    [Fact]
    public void Migrate_FreshDatabase_CreatesAllTables()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ConnectionString;

        DatabaseMigrator.Migrate(connectionString);

        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        using var reader = cmd.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read()) tables.Add(reader.GetString(0));

        tables.Should().Contain(["Recipes", "Requests", "Jobs"]);
    }

    /// <summary>Verifies that running the migration twice against the same database does not throw.</summary>
    [Fact]
    public void Migrate_RunTwice_IsIdempotent()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ConnectionString;

        DatabaseMigrator.Migrate(connectionString);
        var act = () => DatabaseMigrator.Migrate(connectionString);

        act.Should().NotThrow();
    }
}