/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: DatabaseMigrator.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using DbUp.Sqlite;
using Serilog;

namespace SlideGenerator.Settings.Database;

/// <summary>
///     Runs DbUp schema migrations (embedded <c>.sql</c> scripts under <c>Database/Scripts/</c>) against the
///     shared <c>Data.db</c> SQLite database. Replaces the old per-repository <c>CREATE TABLE IF NOT EXISTS</c>
///     pattern with a single, ordered, tracked migration history (DbUp's own <c>SchemaVersions</c> table).
/// </summary>
public static class DatabaseMigrator
{
    /// <summary>
    ///     Applies every not-yet-applied embedded migration script to the database at <paramref name="connectionString" />.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string of the target database.</param>
    /// <exception cref="InvalidOperationException">Thrown if the upgrade fails.</exception>
    public static void Migrate(string connectionString)
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(new SerilogUpgradeLog())
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
            throw new InvalidOperationException("Database migration failed.", result.Error);
    }

    /// <summary>Forwards DbUp's log output to the ambient Serilog logger.</summary>
    private sealed class SerilogUpgradeLog : IUpgradeLog
    {
        public void LogTrace(string format, params object[] args) => Log.Verbose(format, args);
        public void LogDebug(string format, params object[] args) => Log.Debug(format, args);
        public void LogInformation(string format, params object[] args) => Log.Information(format, args);
        public void LogWarning(string format, params object[] args) => Log.Warning(format, args);
        public void LogError(string format, params object[] args) => Log.Error(format, args);
        public void LogError(Exception ex, string format, params object[] args) => Log.Error(ex, format, args);
    }
}
