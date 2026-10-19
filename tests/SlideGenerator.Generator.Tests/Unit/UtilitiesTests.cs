/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: UtilitiesTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Document.Workbooks.Components;
using SlideGenerator.Document.Workbooks.Identifiers;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Utilities.ExecuteWithBackoffAsync{T}" />,
///     <see cref="Utilities.FilterColumns" /> and <see cref="Utilities.ReadRows" />.
/// </summary>
public sealed class UtilitiesTests
{
    private static readonly Func<int, CancellationToken, Task> NoDelay = (_, _) => Task.CompletedTask;

    #region ExecuteWithBackoffAsync

    /// <summary>Verifies a first-try success returns immediately without ever invoking the delay callback.</summary>
    [Fact]
    public async Task ExecuteWithBackoffAsync_SucceedsFirstTry_ReturnsResultWithoutDelay()
    {
        var delayCalls = 0;

        Task Delay(int _, CancellationToken _2)
        {
            delayCalls++;
            return Task.CompletedTask;
        }

        var result = await Utilities.ExecuteWithBackoffAsync(
            3, TimeSpan.FromSeconds(16), () => Task.FromResult<string?>("ok"), CancellationToken.None, Delay);

        result.Should().Be("ok");
        delayCalls.Should().Be(0);
    }

    /// <summary>Verifies the action is retried (with delay between attempts) until it eventually succeeds.</summary>
    [Fact]
    public async Task ExecuteWithBackoffAsync_FailsThenSucceeds_RetriesUntilResult()
    {
        var calls = 0;

        Task<string?> Action()
        {
            calls++;
            return Task.FromResult(calls < 3 ? null : "ok");
        }

        var result = await Utilities.ExecuteWithBackoffAsync(
            5, TimeSpan.FromSeconds(16), Action, CancellationToken.None, NoDelay);

        result.Should().Be("ok");
        calls.Should().Be(3);
    }

    /// <summary>Verifies a factory that always returns null exhausts all retries and returns null.</summary>
    [Fact]
    public async Task ExecuteWithBackoffAsync_ExhaustsRetries_ReturnsNull()
    {
        var calls = 0;

        Task<string?> Action()
        {
            calls++;
            return Task.FromResult<string?>(null);
        }

        var result = await Utilities.ExecuteWithBackoffAsync(
            3, TimeSpan.FromSeconds(16), Action, CancellationToken.None, NoDelay);

        result.Should().BeNull();
        calls.Should().Be(4); // first try + 3 retries
    }

    /// <summary>Verifies an exception on the final attempt is not swallowed — it propagates to the caller.</summary>
    [Fact]
    public async Task ExecuteWithBackoffAsync_FinalAttemptThrows_ExceptionPropagates()
    {
        var calls = 0;

        Task<string?> Action()
        {
            calls++;
            throw new InvalidOperationException("boom");
        }

        var act = () => Utilities.ExecuteWithBackoffAsync(
            2, TimeSpan.FromSeconds(16), Action, CancellationToken.None, NoDelay);

        await act.Should().ThrowAsync<InvalidOperationException>();
        calls.Should().Be(3); // first try + 2 retries, all thrown
    }

    /// <summary>Verifies a genuine cancellation (via the caller's token) is never retried — it throws immediately.</summary>
    [Fact]
    public async Task ExecuteWithBackoffAsync_RealCancellation_ThrowsImmediatelyWithoutRetry()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var calls = 0;

        Task<string?> Action()
        {
            calls++;
            throw new OperationCanceledException(cts.Token);
        }

        var act = () => Utilities.ExecuteWithBackoffAsync(
            5, TimeSpan.FromSeconds(16), Action, cts.Token, NoDelay);

        await act.Should().ThrowAsync<OperationCanceledException>();
        calls.Should().Be(1);
    }

    #endregion

    #region FilterColumns

    /// <summary>Verifies a null <c>usedColumns</c> leaves the header map untouched (same instance).</summary>
    [Fact]
    public void FilterColumns_NullUsedColumns_ReturnsTheSameMap()
    {
        var map = new Dictionary<ColumnIdentifier, int> { [new ColumnIdentifier("Name")] = 0 };

        var result = Utilities.FilterColumns(map, null);

        result.Should().BeSameAs(map);
    }

    /// <summary>
    ///     Verifies only the columns named in <c>usedColumns</c> survive, matched case-insensitively and keeping
    ///     their original indexes.
    /// </summary>
    [Fact]
    public void FilterColumns_UsedColumnsSubset_KeepsOnlyThoseColumnsCaseInsensitively()
    {
        var map = new Dictionary<ColumnIdentifier, int>
        {
            [new ColumnIdentifier("Name")] = 0,
            [new ColumnIdentifier("City")] = 1,
            [new ColumnIdentifier("Photo")] = 2
        };
        IReadOnlySet<ColumnIdentifier> used = new HashSet<ColumnIdentifier>
        {
            new("name"),
            new("PHOTO")
        };

        var result = Utilities.FilterColumns(map, used);

        result.Should().HaveCount(2);
        result[new ColumnIdentifier("name")].Should().Be(0);
        result[new ColumnIdentifier("photo")].Should().Be(2);
        result.Should().BeEquivalentTo(new Dictionary<ColumnIdentifier, int>
        {
            [new ColumnIdentifier("Name")] = 0,
            [new ColumnIdentifier("Photo")] = 2
        });
    }

    /// <summary>Verifies an empty <c>usedColumns</c> set yields an empty map.</summary>
    [Fact]
    public void FilterColumns_UsedColumnsEmpty_ReturnsEmptyMap()
    {
        var map = new Dictionary<ColumnIdentifier, int> { [new ColumnIdentifier("Name")] = 0 };

        var result = Utilities.FilterColumns(map, new HashSet<ColumnIdentifier>());

        result.Should().BeEmpty();
    }

    #endregion

    #region ReadRows

    /// <summary>
    ///     Verifies only the requested worksheet rows are read (each exactly once) and that every result element
    ///     maps the header columns to that row's values, in <c>dataRows</c> order.
    /// </summary>
    [Fact]
    public void ReadRows_SelectedRows_ReadsEachWorksheetRowOnceAndMapsValues()
    {
        var worksheet = Substitute.For<IReadOnlyWorksheet>();
        worksheet.GetRow(2).Returns(new[] { "Alice", "Hanoi" });
        worksheet.GetRow(4).Returns(new[] { "Bob", "Saigon" });
        worksheet.GetRow(6).Returns(new[] { "Cara", "Danang" });
        var headerToIndex = new Dictionary<ColumnIdentifier, int>
        {
            [new ColumnIdentifier("Name")] = 0,
            [new ColumnIdentifier("City")] = 1
        };

        var result = Utilities.ReadRows(worksheet, headerToIndex, [1, 3, 5]);

        result.Should().HaveCount(3);
        result[0][new ColumnIdentifier("Name")].Should().Be("Alice");
        result[0][new ColumnIdentifier("City")].Should().Be("Hanoi");
        result[1][new ColumnIdentifier("Name")].Should().Be("Bob");
        result[1][new ColumnIdentifier("City")].Should().Be("Saigon");
        result[2][new ColumnIdentifier("Name")].Should().Be("Cara");
        result[2][new ColumnIdentifier("City")].Should().Be("Danang");
        result.Should().OnlyContain(row => row.Count == headerToIndex.Count);
        worksheet.Received(1).GetRow(2);
        worksheet.Received(1).GetRow(4);
        worksheet.Received(1).GetRow(6);
        worksheet.DidNotReceive().GetRow(3);
        worksheet.DidNotReceive().GetRow(5);
    }

    /// <summary>Verifies a row shorter than the header map reads missing cells as empty strings.</summary>
    [Fact]
    public void ReadRows_ShortRow_FillsMissingCellsWithEmptyString()
    {
        var worksheet = Substitute.For<IReadOnlyWorksheet>();
        worksheet.GetRow(2).Returns(new[] { "Alice" });
        var headerToIndex = new Dictionary<ColumnIdentifier, int>
        {
            [new ColumnIdentifier("Name")] = 0,
            [new ColumnIdentifier("City")] = 1
        };

        var result = Utilities.ReadRows(worksheet, headerToIndex, [1]);

        result.Should().ContainSingle();
        result[0][new ColumnIdentifier("Name")].Should().Be("Alice");
        result[0][new ColumnIdentifier("City")].Should().BeEmpty();
    }

    /// <summary>Verifies no requested data rows means an empty result and no worksheet access at all.</summary>
    [Fact]
    public void ReadRows_NoDataRows_ReturnsEmptyListWithoutReadingTheWorksheet()
    {
        var worksheet = Substitute.For<IReadOnlyWorksheet>();
        var headerToIndex = new Dictionary<ColumnIdentifier, int> { [new ColumnIdentifier("Name")] = 0 };

        var result = Utilities.ReadRows(worksheet, headerToIndex, []);

        result.Should().BeEmpty();
        worksheet.DidNotReceive().GetRow(Arg.Any<int>());
    }

    #endregion
}