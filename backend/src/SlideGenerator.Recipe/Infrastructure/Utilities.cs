/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Utilities.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Data.Sqlite;

namespace SlideGenerator.Recipe.Infrastructure;

/// <summary>
///     General utility helpers for the infrastructure layer, including SQLite connection extensions.
/// </summary>
public static class Utilities
{
    extension(SqliteConnectionStringBuilder builder)
    {
        /// <summary>
        ///     Opens a fresh <see cref="SqliteConnection" /> from the builder's connection string.
        ///     The caller must dispose the returned connection (use <c>await using</c>).
        /// </summary>
        public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new SqliteConnection(builder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            return conn;
        }

        /// <summary>
        ///     Opens a fresh <see cref="SqliteConnection" /> synchronously.
        ///     The caller must dispose the returned connection (use <c>using</c>).
        /// </summary>
        public SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(builder.ConnectionString);
            conn.Open();
            return conn;
        }
    }
}