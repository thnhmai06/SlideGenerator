/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: Sha256Hasher.cs
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
using SlideGenerator.Cryptography.Application.Abstractions;

namespace SlideGenerator.Cryptography.Infrastructure;

/// <summary>
///     SHA-256 implementation of <see cref="IHasher" />.
/// </summary>
public sealed class Sha256Hasher : IHasher
{
    /// <inheritdoc />
    public int MaxLength => 64;

    /// <inheritdoc />
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public string ComputeHashFile(string filePath, int? length = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);

        var take = Math.Min(length ?? MaxLength, MaxLength);
        return ToHexString(hashBytes)[..take];
    }

    /// <inheritdoc />
    public string ComputeHash(string text, int? length = null)
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