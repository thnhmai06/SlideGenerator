/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: SqliteConnectionFactory.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using Microsoft.Data.Sqlite;

namespace SlideGenerator.Utilities.Database;

/// <summary>
///     Factory helpers for creating short-lived <see cref="SqliteConnection" /> instances.
///     Each caller owns the returned connection and is responsible for disposing it.
/// </summary>
public static class SqliteConnectionFactory
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