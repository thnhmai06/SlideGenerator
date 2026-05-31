/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: IHasher.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Cryptography.Application.Abstractions;

/// <summary>
///     Defines the contract for cryptographic hashing of files and strings.
/// </summary>
public interface IHasher
{
    /// <summary>The maximum number of hexadecimal characters a hash can produce.</summary>
    int MaxLength { get; }

    /// <summary>
    ///     Computes the SHA-256 hash of the file at the specified path and returns the hash as a hexadecimal string.
    /// </summary>
    /// <param name="filePath">The path to the file to hash.</param>
    /// <param name="length">Number of characters to return. Defaults to <see cref="MaxLength" />.</param>
    /// <returns>Hexadecimal hash string of the requested length.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    string ComputeHashFile(string filePath, int? length = null);

    /// <summary>
    ///     Computes the SHA-256 hash of the given text and returns the hash as a hexadecimal string.
    /// </summary>
    /// <param name="text">The input text to hash.</param>
    /// <param name="length">Number of characters to return. Defaults to <see cref="MaxLength" />.</param>
    /// <returns>Hexadecimal hash string of the requested length, or <see cref="string.Empty" /> for null/empty input.</returns>
    string ComputeHash(string text, int? length = null);
}