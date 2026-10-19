/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: WorkbookConcurrencyIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using FluentAssertions;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Document.Workbooks.Identifiers;
using Syncfusion.Licensing;
using Xunit;

namespace SlideGenerator.Document.Tests.Integration;

/// <summary>
///     Integration tests for concurrent use of <see cref="IWorkbookOpener" />, which mirrors how jobs
///     open workbooks concurrently (bounded by <c>MaxConcurrentJobs</c>). Requires a valid Syncfusion license.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WorkbookConcurrencyIntegrationTests
{
    private const int Threads = 8;
    private const int Iterations = 30;
    private static readonly string WorkbookPath = ResolveWorkbookPath();

    /// <summary>
    ///     Verifies that many concurrent read-only opens and disposals never throw. Regression test for a
    ///     shared <c>ExcelEngine</c> whose workbook collection is not thread-safe (concurrent add/remove
    ///     produced <see cref="ArgumentOutOfRangeException" /> from <c>List.RemoveAt</c> during
    ///     <c>WorkbookImpl.Close</c>).
    /// </summary>
    [Fact]
    public async Task OpenWorkbookReadOnlyAsync_ManyConcurrentOpensAndDisposals_NeverThrows()
    {
        if (string.IsNullOrEmpty(SyncfusionLicense.Key))
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");

        SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicense.Key);

        var opener = new SfWorkbookOpener();
        var identifier = new WorkbookIdentifier(WorkbookPath);
        var exceptions = new ConcurrentBag<Exception>();
        var ct = TestContext.Current.CancellationToken;

        await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                try
                {
                    using var workbook = await opener.OpenWorkbookReadOnlyAsync(identifier, ct)
                        .ConfigureAwait(false);
                    _ = workbook.Worksheets.Count();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }, ct)));

        var first = exceptions.FirstOrDefault();
        exceptions.Should().BeEmpty(
            first is null
                ? "no exception may escape a concurrent open/dispose"
                : $"a concurrent open/dispose escaped with:\n{first}");
    }

    /// <summary>
    ///     Resolves the shared <c>Data.xlsx</c> fixture, preferring the copy next to the test binaries and
    ///     falling back to the repository's <c>tests/fixtures</c> folder (this project does not copy it).
    /// </summary>
    /// <returns>The absolute path to <c>Data.xlsx</c>.</returns>
    private static string ResolveWorkbookPath()
    {
        var relative = Path.Combine("fixtures", "data", "Data.xlsx");
        if (File.Exists(relative))
            return Path.GetFullPath(relative);

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "tests", relative);
            if (File.Exists(candidate))
                return candidate;
        }

        return Path.GetFullPath(relative);
    }
}