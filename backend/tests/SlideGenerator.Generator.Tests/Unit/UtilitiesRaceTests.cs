/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: UtilitiesRaceTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Generator.Application;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Generator.Tests.Unit.Helpers;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Tests for the <see cref="Utilities" /> extension methods on <see cref="GeneratingContext" />,
///     focusing on the check-then-act race condition in
///     <see cref="Utilities.GetOrOpenWorkbook" />.
/// </summary>
public sealed class UtilitiesRaceTests
{
    #region BUG — Check-then-act race leaks file handles

    /// <summary>
    ///     Regression contract for the race fix: <see cref="Utilities.GetOrOpenWorkbook" /> must
    ///     route concurrent calls through a <see cref="Lazy{T}" /> factory so that exactly one
    ///     workbook is opened per identifier. This guarantees no opened handle is leaked outside
    ///     the dictionary and that <see cref="GeneratingContext.Dispose" /> disposes every handle
    ///     that was ever returned to a caller.
    /// </summary>
    [Fact(DisplayName = "GetOrOpenWorkbook race opens only one handle and disposes it cleanly")]
    public async Task GetOrOpenWorkbook_ConcurrentCallsForSameIdentifier_LeaksAtLeastOneUndisposedHandle()
    {
        // Arrange — a temp file so the File.Exists check passes.
        var bookPath = Path.Combine(Path.GetTempPath(), $"race_{Guid.NewGuid():N}.xlsx");
        await File.WriteAllBytesAsync(bookPath, [0x50, 0x4B, 0x03, 0x04], TestContext.Current.CancellationToken);
        try
        {
            var (_, data) = StepHelper.CreateContextPair();
            var identifier = new WorkbookIdentifier(bookPath);

            // Each call to OpenWorkbookReadOnly returns a fresh mock so we can count
            // disposals per opened instance independently.
            var opened = new List<IReadOnlyWorkbook>();
            var sync = new object();

            var provider = Substitute.For<IWorkbookProvider>();
            provider.OpenWorkbookReadOnly(Arg.Any<WorkbookIdentifier>()).Returns(_ =>
            {
                var w = Substitute.For<IReadOnlyWorkbook>();
                lock (sync)
                {
                    opened.Add(w);
                }

                // Small spin to widen the race window so the bug surfaces reliably.
                Thread.SpinWait(2_000);
                return w;
            });

            // Act — heavy concurrent hammering of the same identifier.
            const int parallelism = 64;
            using var startGate = new ManualResetEventSlim();
            var tasks = Enumerable.Range(0, parallelism)
                .Select(_ => Task.Run(() =>
                {
                    startGate.Wait();
                    return data.GetOrOpenWorkbook(provider, identifier);
                }))
                .ToArray();

            startGate.Set();
            await Task.WhenAll(tasks);

            // Lazy factory guarantees a single open even under heavy contention.
            opened.Count.Should().Be(1, "the race fix opens the workbook at most once");

            // Simulate end-of-workflow disposal — same code path as CloseAllHandles.
            data.Dispose();

            var disposed = opened.Count(h => h.ReceivedCalls().Any(c => c.GetMethodInfo().Name == "Dispose"));
            disposed.Should().Be(opened.Count,
                "every opened handle is now stored in the dictionary and is disposed by Dispose()");
        }
        finally
        {
            try
            {
                File.Delete(bookPath);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    /// <summary>
    ///     Correctness contract that the fix must satisfy: under concurrent access for the
    ///     same identifier, <see cref="IWorkbookProvider.OpenWorkbookReadOnly" /> should be
    ///     invoked exactly once, and every caller should receive the same instance.
    ///     <para>
    ///         This test <b>fails on the current implementation</b> and is the green-bar target
    ///         for the fix (use <c>ConcurrentDictionary.GetOrAdd</c> with a factory + Lazy, or
    ///         guard the open with a lock keyed by identifier).
    ///     </para>
    /// </summary>
    [Fact(DisplayName = "BUG (contract): GetOrOpenWorkbook must call provider exactly once per key")]
    public async Task GetOrOpenWorkbook_ConcurrentCallsForSameIdentifier_ShouldOpenWorkbookExactlyOnce()
    {
        var bookPath = Path.Combine(Path.GetTempPath(), $"race_{Guid.NewGuid():N}.xlsx");
        await File.WriteAllBytesAsync(bookPath, [0x50, 0x4B, 0x03, 0x04], TestContext.Current.CancellationToken);
        try
        {
            var (_, data) = StepHelper.CreateContextPair();
            var identifier = new WorkbookIdentifier(bookPath);

            var openCount = 0;
            var provider = Substitute.For<IWorkbookProvider>();
            provider.OpenWorkbookReadOnly(Arg.Any<WorkbookIdentifier>()).Returns(_ =>
            {
                Interlocked.Increment(ref openCount);
                Thread.SpinWait(2_000);
                return Substitute.For<IReadOnlyWorkbook>();
            });

            using var startGate = new ManualResetEventSlim();
            var tasks = Enumerable.Range(0, 64)
                .Select(_ => Task.Run(() =>
                {
                    startGate.Wait();
                    return data.GetOrOpenWorkbook(provider, identifier);
                }))
                .ToArray();
            startGate.Set();
            var results = await Task.WhenAll(tasks);

            openCount.Should().Be(1, "the cache must be populated atomically");
            results.Should().OnlyContain(r => ReferenceEquals(r, results[0]),
                "every caller must receive the same cached workbook handle");
        }
        finally
        {
            try
            {
                File.Delete(bookPath);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    #endregion

    #region File-not-found contract

    /// <summary>
    ///     Verifies that <see cref="Utilities.GetOrOpenWorkbook" /> throws
    ///     <see cref="FileNotFoundException" /> when the workbook path on disk is missing and
    ///     the handle is not already cached — the only documented exception this method must
    ///     surface. Locks the existing contract to prevent regressions when the race fix lands.
    /// </summary>
    [Fact]
    public void GetOrOpenWorkbook_FileMissingAndNotCached_ThrowsFileNotFoundException()
    {
        var (_, data) = StepHelper.CreateContextPair();
        var provider = Substitute.For<IWorkbookProvider>();
        var identifier = new WorkbookIdentifier(Path.Combine(Path.GetTempPath(),
            $"definitely-missing-{Guid.NewGuid():N}.xlsx"));

        var act = () => data.GetOrOpenWorkbook(provider, identifier);

        act.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    ///     Verifies that an already-cached handle is returned without consulting the file
    ///     system. This is the resume-after-persist path; <see cref="File.Exists" /> must not
    ///     be reached, otherwise resumed workflows whose source files were moved would fail.
    /// </summary>
    [Fact]
    public void GetOrOpenWorkbook_HandleAlreadyCached_ReturnsCachedAndSkipsFileExistsCheck()
    {
        var (_, data) = StepHelper.CreateContextPair();
        var provider = Substitute.For<IWorkbookProvider>();
        var identifier = new WorkbookIdentifier(Path.Combine(Path.GetTempPath(),
            $"definitely-missing-{Guid.NewGuid():N}.xlsx"));
        var cached = Substitute.For<IReadOnlyWorkbook>();
        data.WorkbookHandles.TryAdd(identifier, cached);

        var result = data.GetOrOpenWorkbook(provider, identifier);

        result.Should().BeSameAs(cached);
        provider.DidNotReceive().OpenWorkbookReadOnly(Arg.Any<WorkbookIdentifier>());
    }

    #endregion
}