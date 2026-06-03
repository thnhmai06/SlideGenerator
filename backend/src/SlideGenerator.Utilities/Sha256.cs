/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: Sha256.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Security.Cryptography;
using System.Text;

namespace SlideGenerator.Utilities;

/// <summary>
///     SHA-256 hashing utilities for files and strings.
/// </summary>
public static class Sha256
{
    /// <summary>The maximum number of hexadecimal characters a SHA-256 hash can produce.</summary>
    public const int MaxLength = 64;

    /// <summary>
    ///     Computes the SHA-256 hash of the file at the specified path.
    /// </summary>
    /// <param name="filePath">The path to the file to hash.</param>
    /// <param name="length">Number of characters to return. Defaults to <see cref="MaxLength" />.</param>
    /// <returns>Hexadecimal hash string of the requested length.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static string HashFile(string filePath, int? length = null)
    {
        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        var take = Math.Min(length ?? MaxLength, MaxLength);
        return ToHexString(hashBytes)[..take];
    }

    /// <summary>
    ///     Computes the SHA-256 hash of the given text.
    /// </summary>
    /// <param name="text">The input text to hash.</param>
    /// <param name="length">Number of characters to return. Defaults to <see cref="MaxLength" />.</param>
    /// <returns>Hexadecimal hash string of the requested length, or <see cref="string.Empty" /> for null/empty input.</returns>
    public static string HashText(this string text, int? length = null)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var take = Math.Min(length ?? MaxLength, MaxLength);
        return ToHexString(hashBytes)[..take];
    }

    private static string ToHexString(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}