/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: CacheEvictionRulesTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Rules;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for the pure, I/O-free <see cref="CacheEvictionRules" /> policy.
/// </summary>
public sealed class CacheEvictionRulesTests
{
    #region Constants

    /// <summary>Verifies the TTL constant is 14 days.</summary>
    [Fact]
    public void Ttl_IsFourteenDays()
    {
        CacheEvictionRules.Ttl.Should().Be(TimeSpan.FromDays(14));
    }

    /// <summary>Verifies the max-size constant is 50MB.</summary>
    [Fact]
    public void MaxSizeBytes_IsFiftyMegabytes()
    {
        CacheEvictionRules.MaxSizeBytes.Should().Be(50L * 1024 * 1024);
    }

    /// <summary>Verifies the eviction ratio constant is 20%.</summary>
    [Fact]
    public void EvictionRatio_IsTwentyPercent()
    {
        CacheEvictionRules.EvictionRatio.Should().Be(0.20);
    }

    #endregion

    #region IsExpired

    /// <summary>Verifies a timestamp just under the TTL boundary is not expired.</summary>
    [Fact]
    public void IsExpired_JustUnderTtl_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now - CacheEvictionRules.Ttl + TimeSpan.FromSeconds(1);

        CacheEvictionRules.IsExpired(timestamp, now).Should().BeFalse();
    }

    /// <summary>Verifies a timestamp exactly at the TTL boundary is expired.</summary>
    [Fact]
    public void IsExpired_ExactlyAtTtl_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now - CacheEvictionRules.Ttl;

        CacheEvictionRules.IsExpired(timestamp, now).Should().BeTrue();
    }

    /// <summary>Verifies a timestamp older than the TTL is expired.</summary>
    [Fact]
    public void IsExpired_OlderThanTtl_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now - CacheEvictionRules.Ttl - TimeSpan.FromDays(1);

        CacheEvictionRules.IsExpired(timestamp, now).Should().BeTrue();
    }

    #endregion

    #region ExpiryCutoff

    /// <summary>Verifies the cutoff is exactly now minus the TTL.</summary>
    [Fact]
    public void ExpiryCutoff_ReturnsNowMinusTtl()
    {
        var now = DateTimeOffset.UtcNow;

        CacheEvictionRules.ExpiryCutoff(now).Should().Be(now - CacheEvictionRules.Ttl);
    }

    #endregion

    #region IsOverBudget

    /// <summary>Verifies a size below the max is not over budget.</summary>
    [Fact]
    public void IsOverBudget_BelowMax_ReturnsFalse()
    {
        CacheEvictionRules.IsOverBudget(CacheEvictionRules.MaxSizeBytes - 1).Should().BeFalse();
    }

    /// <summary>Verifies a size exactly at the max is over budget.</summary>
    [Fact]
    public void IsOverBudget_ExactlyAtMax_ReturnsTrue()
    {
        CacheEvictionRules.IsOverBudget(CacheEvictionRules.MaxSizeBytes).Should().BeTrue();
    }

    #endregion

    #region RowsToEvict

    /// <summary>Verifies zero rows evicts zero rows.</summary>
    [Fact]
    public void RowsToEvict_ZeroRows_ReturnsZero()
    {
        CacheEvictionRules.RowsToEvict(0).Should().Be(0);
    }

    /// <summary>Verifies a single row never gets evicted (rounds down, never evicts 100%).</summary>
    [Fact]
    public void RowsToEvict_OneRow_ReturnsZero()
    {
        CacheEvictionRules.RowsToEvict(1).Should().Be(0);
    }

    /// <summary>Verifies 10 rows evicts 2 (20%).</summary>
    [Fact]
    public void RowsToEvict_TenRows_ReturnsTwo()
    {
        CacheEvictionRules.RowsToEvict(10).Should().Be(2);
    }

    /// <summary>Verifies 100 rows evicts 20 (20%).</summary>
    [Fact]
    public void RowsToEvict_HundredRows_ReturnsTwenty()
    {
        CacheEvictionRules.RowsToEvict(100).Should().Be(20);
    }

    #endregion
}
