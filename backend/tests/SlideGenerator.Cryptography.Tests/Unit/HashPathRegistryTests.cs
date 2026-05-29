/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography.Tests
 * File: HashPathRegistryTests.cs
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

using FluentAssertions;
using SlideGenerator.Cryptography.Application.Services;
using SlideGenerator.Cryptography.Infrastructure;
using Xunit;

namespace SlideGenerator.Cryptography.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="HashPathRegistry" />, covering caching semantics, manual hash registration,
///     retrieval, clearing, and thread-safety guarantees.
/// </summary>
public sealed class HashPathRegistryTests
{
    private readonly HashPathRegistry _registry = new(new Sha256Hasher());

    #region SetHash

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.SetHash" /> overwrites any previously stored hash
    ///     for the same path with the new value.
    /// </summary>
    [Fact]
    public void SetHash_ExistingPath_OverwritesPreviousValue()
    {
        _registry.SetHash("file.txt", "first00");
        _registry.SetHash("file.txt", "second0");

        _registry.TryGetHash("file.txt", out var hash);
        hash.Should().Be("second0");
    }

    #endregion

    #region Clear

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.Clear" /> removes all previously stored entries,
    ///     making <see cref="HashPathRegistry.TryGetHash" /> return <see langword="false" /> for all paths.
    /// </summary>
    [Fact]
    public void Clear_WithExistingEntries_RemovesAllEntries()
    {
        _registry.SetHash("file1.txt", "hash001");
        _registry.SetHash("file2.txt", "hash002");

        _registry.Clear();

        _registry.TryGetHash("file1.txt", out _).Should().BeFalse();
        _registry.TryGetHash("file2.txt", out _).Should().BeFalse();
    }

    #endregion

    #region Thread Safety

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry" /> does not throw or corrupt state when
    ///     <see cref="HashPathRegistry.GetShortHash" /> is called concurrently from multiple threads
    ///     with overlapping path keys.
    /// </summary>
    [Fact]
    public async Task GetShortHash_ConcurrentAccess_DoesNotThrow()
    {
        var tasks = Enumerable.Range(0, 200)
            .Select(i => Task.Run(() => _registry.GetShortHash($"path-{i % 10}.txt")));

        Func<Task> act = async () => await Task.WhenAll(tasks);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetShortHash

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.GetShortHash" /> computes a 7-character hexadecimal hash
    ///     on the first call for a previously unseen path.
    /// </summary>
    [Fact]
    public void GetShortHash_NewPath_ComputesAndReturns7CharHash()
    {
        var result = _registry.GetShortHash("some/path/file.txt");

        result.Should().HaveLength(7)
            .And.MatchRegex("^[0-9a-f]+$");
    }

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.GetShortHash" /> returns the same value on further
    ///     calls for the same path, confirming the result is cached after the first computation.
    /// </summary>
    [Fact]
    public void GetShortHash_SamePathCalledTwice_ReturnsCachedValue()
    {
        var first = _registry.GetShortHash("cached/path.txt");
        var second = _registry.GetShortHash("cached/path.txt");

        first.Should().Be(second);
    }

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.GetShortHash" /> treats path lookup as case-insensitive,
    ///     returning the same cached hash for paths that differ only in casing.
    /// </summary>
    [Fact]
    public void GetShortHash_DifferentCasingForSamePath_ReturnsSameHash()
    {
        var upper = _registry.GetShortHash("PATH/FILE.TXT");
        var lower = _registry.GetShortHash("path/file.txt");

        upper.Should().Be(lower);
    }

    #endregion

    #region TryGetHash

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.TryGetHash" /> returns <see langword="false" />
    ///     and an out value of <see langword="null" /> for a path that has not been registered.
    /// </summary>
    [Fact]
    public void TryGetHash_UnknownPath_ReturnsFalseAndNull()
    {
        var found = _registry.TryGetHash("unknown/path.txt", out var hash);

        found.Should().BeFalse();
        hash.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.TryGetHash" /> returns <see langword="true" /> and
    ///     the correct hash value after the path has been registered via <see cref="HashPathRegistry.SetHash" />.
    /// </summary>
    [Fact]
    public void TryGetHash_AfterSetHash_ReturnsTrueWithStoredValue()
    {
        _registry.SetHash("my/path.txt", "abc1234");

        var found = _registry.TryGetHash("my/path.txt", out var hash);

        found.Should().BeTrue();
        hash.Should().Be("abc1234");
    }

    /// <summary>
    ///     Verifies that <see cref="HashPathRegistry.TryGetHash" /> returns <see langword="true" /> after
    ///     a path's hash has been implicitly computed by <see cref="HashPathRegistry.GetShortHash" />.
    /// </summary>
    [Fact]
    public void TryGetHash_AfterGetShortHash_ReturnsTrueWithComputedValue()
    {
        var computed = _registry.GetShortHash("computed/path.txt");

        var found = _registry.TryGetHash("computed/path.txt", out var hash);

        found.Should().BeTrue();
        hash.Should().Be(computed);
    }

    #endregion
}