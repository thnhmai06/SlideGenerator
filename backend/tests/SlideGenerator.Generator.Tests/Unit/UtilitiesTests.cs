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
using SlideGenerator.Cloud.Models;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Utilities.ExecuteWithBackoffAsync{T}" /> and
///     <see cref="Utilities.ExceedsMaxDownloadBytes" />.
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

    #region ExceedsMaxDownloadBytes

    /// <summary>Verifies a null <see cref="ContentInfo" /> never exceeds the limit.</summary>
    [Fact]
    public void ExceedsMaxDownloadBytes_NullContentInfo_ReturnsFalse()
    {
        Utilities.ExceedsMaxDownloadBytes(null, 1000).Should().BeFalse();
    }

    /// <summary>Verifies an unknown (null) content length never exceeds the limit.</summary>
    [Fact]
    public void ExceedsMaxDownloadBytes_NullLength_ReturnsFalse()
    {
        var info = new ContentInfo(new Uri("https://example.com/a.png"), "image/png", null, ".png");

        Utilities.ExceedsMaxDownloadBytes(info, 1000).Should().BeFalse();
    }

    /// <summary>Verifies a content length over the limit returns true.</summary>
    [Fact]
    public void ExceedsMaxDownloadBytes_LengthOverLimit_ReturnsTrue()
    {
        var info = new ContentInfo(new Uri("https://example.com/a.png"), "image/png", 2000, ".png");

        Utilities.ExceedsMaxDownloadBytes(info, 1000).Should().BeTrue();
    }

    /// <summary>Verifies a content length within the limit returns false.</summary>
    [Fact]
    public void ExceedsMaxDownloadBytes_LengthWithinLimit_ReturnsFalse()
    {
        var info = new ContentInfo(new Uri("https://example.com/a.png"), "image/png", 500, ".png");

        Utilities.ExceedsMaxDownloadBytes(info, 1000).Should().BeFalse();
    }

    /// <summary>Verifies a <c>maxDownloadBytes</c> of 0 (unlimited) never exceeds, regardless of length.</summary>
    [Fact]
    public void ExceedsMaxDownloadBytes_UnlimitedZero_ReturnsFalse()
    {
        var info = new ContentInfo(new Uri("https://example.com/a.png"), "image/png", uint.MaxValue, ".png");

        Utilities.ExceedsMaxDownloadBytes(info, 0).Should().BeFalse();
    }

    #endregion
}
