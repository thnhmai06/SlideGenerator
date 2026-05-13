/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: FileHasher.cs
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
using System.Security.Cryptography;
using System.Text;

namespace SlideGenerator.Cryptography.Application.Services;

/// <summary>
///     Provides cryptographic hashing services for files and strings.
/// </summary>
public static class FileHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of the file at the specified path and returns the hash as a hexadecimal string.
    /// </summary>
    /// <param name="filePath">The path to the file for which the SHA-256 hash is computed.</param>
    /// <returns>The hexadecimal string representation of the SHA-256 hash.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static string ComputeSha256(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return ToHexString(hashBytes);
    }

    /// <summary>
    /// Computes a shortened hash of the given text and returns the first seven characters
    /// of the hexadecimal string representation of the hash.
    /// </summary>
    /// <param name="text">The input text for which the shortened hash is computed.</param>
    /// <param name="length">The number of characters to return from the hash. Default is 7.</param>
    /// <returns>A string containing the first seven characters of the hash.
    /// If the input text is null or empty, a default value of "default" is returned.</returns>
    public static string ComputeHash(string text, int length = 7)
    {
        if (string.IsNullOrEmpty(text)) return "default";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return ToHexString(hashBytes)[..length];
    }

    private static string ToHexString(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}





