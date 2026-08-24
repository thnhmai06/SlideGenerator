/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: SummaryCacheTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Summarizer;
using SlideGenerator.Summarizer.Workbooks;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="SummaryCache" />'s (path, last-write-time) memoization — the editor
///     re-summarizes the same file on nearly every render and must not re-open it via Syncfusion each time.
/// </summary>
public sealed class SummaryCacheTests
{
    /// <summary>Repeat calls against an unchanged file must hit the inner service exactly once.</summary>
    [Fact]
    public async Task GetWorkbookAsync_SameFileUnchanged_CallsInnerServiceOnce()
    {
        var path = Path.GetTempFileName();
        try
        {
            var inner = Substitute.For<ISummarizationService>();
            inner.SummarizeWorkbookAsync(Arg.Any<WorkbookIdentifier>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new WorkbookSummary(path, "wb", []));
            var cache = new SummaryCache(inner);
            var identifier = new WorkbookIdentifier(path);

            await cache.GetWorkbookAsync(identifier);
            await cache.GetWorkbookAsync(identifier);

            await inner.Received(1).SummarizeWorkbookAsync(Arg.Any<WorkbookIdentifier>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A changed last-write-time must invalidate the cached entry and re-call the inner service.</summary>
    [Fact]
    public async Task GetWorkbookAsync_FileModified_CallsInnerServiceAgain()
    {
        var path = Path.GetTempFileName();
        try
        {
            var inner = Substitute.For<ISummarizationService>();
            inner.SummarizeWorkbookAsync(Arg.Any<WorkbookIdentifier>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new WorkbookSummary(path, "wb", []));
            var cache = new SummaryCache(inner);
            var identifier = new WorkbookIdentifier(path);

            await cache.GetWorkbookAsync(identifier);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
            await cache.GetWorkbookAsync(identifier);

            await inner.Received(2).SummarizeWorkbookAsync(Arg.Any<WorkbookIdentifier>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    ///     A failed call (e.g. the file was locked) must not poison the cache — the same unchanged file must
    ///     be retried on the next call rather than replaying the same exception forever.
    /// </summary>
    [Fact]
    public async Task GetWorkbookAsync_InnerServiceThrows_RetriesOnNextCall()
    {
        var path = Path.GetTempFileName();
        try
        {
            var inner = Substitute.For<ISummarizationService>();
            inner.SummarizeWorkbookAsync(Arg.Any<WorkbookIdentifier>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(
                    Task.FromException<WorkbookSummary>(new IOException("locked")),
                    Task.FromResult(new WorkbookSummary(path, "wb", [])));
            var cache = new SummaryCache(inner);
            var identifier = new WorkbookIdentifier(path);

            var act = () => cache.GetWorkbookAsync(identifier);
            await act.Should().ThrowAsync<IOException>();

            var result = await cache.GetWorkbookAsync(identifier);

            result.Name.Should().Be("wb");
            await inner.Received(2).SummarizeWorkbookAsync(Arg.Any<WorkbookIdentifier>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
