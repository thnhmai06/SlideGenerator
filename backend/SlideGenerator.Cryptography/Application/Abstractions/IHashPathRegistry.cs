/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: IHashPathRegistry.cs
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
namespace SlideGenerator.Cryptography.Application.Abstractions;

/// <summary>
///     Defines the contract for a thread-safe registry that caches short file path hashes.
///     Enables consistent, reproducible path generation across workflow steps.
/// </summary>
public interface IHashPathRegistry
{
    /// <summary>
    ///     Gets the 7-character short hash for the specified path, computing it if not already cached.
    /// </summary>
    /// <param name="path">The path to hash.</param>
    /// <returns>A 7-character hexadecimal string.</returns>
    string GetShortHash(string path);

    /// <summary>
    ///     Adds or updates a hash for the specified path manually.
    /// </summary>
    void SetHash(string path, string hash);

    /// <summary>
    ///     Tries to retrieve the cached hash for the specified path without computing it.
    /// </summary>
    bool TryGetHash(string path, out string? hash);

    /// <summary>
    ///     Clears all stored hashes.
    /// </summary>
    void Clear();
}
