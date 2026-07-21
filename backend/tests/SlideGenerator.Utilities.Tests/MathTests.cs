/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities.Tests
 * File: MathTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Xunit;

namespace SlideGenerator.Utilities.Tests;

/// <summary>
///     Unit tests for the <see cref="Math" /> utility class, covering the truncated exponential
///     backoff delay computation.
/// </summary>
public sealed class MathTests
{
    /// <summary>Verifies attempt 0's delay falls within [1s, 1s + 1000ms] when the cap is not reached.</summary>
    [Fact]
    public void ComputeBackoffDelay_AttemptZero_WithinExpectedRange()
    {
        var delay = Math.ComputeBackoffDelay(0, TimeSpan.FromSeconds(100));

        delay.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
        delay.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(1000));
    }

    /// <summary>Verifies a large attempt count is always clamped to the supplied max delay.</summary>
    [Fact]
    public void ComputeBackoffDelay_LargeAttempt_ClampedToMaxDelay()
    {
        var maxDelay = TimeSpan.FromSeconds(16);

        var delay = Math.ComputeBackoffDelay(20, maxDelay);

        delay.Should().Be(maxDelay);
    }

    /// <summary>Verifies the computed delay is never negative, across a range of attempt counts.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    public void ComputeBackoffDelay_NeverNegative(int attempt)
    {
        var delay = Math.ComputeBackoffDelay(attempt, TimeSpan.FromSeconds(16));

        delay.Should().BePositive();
    }
}
