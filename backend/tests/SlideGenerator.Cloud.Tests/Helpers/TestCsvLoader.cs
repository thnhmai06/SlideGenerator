/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: TestCsvLoader.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SlideGenerator.Cloud.Tests.Integration.Models;

namespace SlideGenerator.Cloud.Tests.Helpers;

/// <summary>Loads <see cref="TestCase" /> records from a CSV file using CsvHelper.</summary>
internal static class TestCsvLoader
{
    /// <summary>
    ///     Reads all rows from <paramref name="csvFileName" /> located in the test output directory
    ///     and returns them as a read-only list of <see cref="TestCase" /> instances.
    /// </summary>
    /// <param name="csvFileName">File name relative to <see cref="AppContext.BaseDirectory" />.</param>
    public static IReadOnlyList<TestCase> Load(string csvFileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, csvFileName);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<TestCase>().ToList();
    }
}