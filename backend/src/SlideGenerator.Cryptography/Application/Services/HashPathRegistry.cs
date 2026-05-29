/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: HashPathRegistry.cs
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

using System.Collections.Concurrent;
using SlideGenerator.Cryptography.Application.Abstractions;

namespace SlideGenerator.Cryptography.Application.Services;

/// <summary>
///     A thread-safe registry for storing and retrieving file path hashes.
///     Computes the hash automatically if it's not already cached.
/// </summary>
internal sealed class HashPathRegistry(IHasher hasher) : IHashPathRegistry
{
    private const int HashLength = 7;
    private readonly ConcurrentDictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the 7-character short hash for the specified path, computing it if necessary.
    /// </summary>
    /// <param name="path">The path to hash.</param>
    /// <returns>A 7-character hexadecimal string.</returns>
    public string GetShortHash(string path)
    {
        return _hashes.GetOrAdd(path, static (p, h) => h.ComputeHash(p, HashLength), hasher);
    }

    /// <summary>
    ///     Adds or updates a hash for the specified path manually.
    /// </summary>
    public void SetHash(string path, string hash)
    {
        _hashes[path] = hash;
    }

    /// <summary>
    ///     Tries to retrieve the hash for the specified path without computing it.
    /// </summary>
    public bool TryGetHash(string path, out string? hash)
    {
        return _hashes.TryGetValue(path, out hash);
    }

    /// <summary>
    ///     Clears all stored hashes.
    /// </summary>
    public void Clear()
    {
        _hashes.Clear();
    }
}