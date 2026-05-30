/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography.Tests
 * File: Sha256HasherTests.cs
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

using System.Text;
using FluentAssertions;
using SlideGenerator.Cryptography.Infrastructure;
using Xunit;

namespace SlideGenerator.Cryptography.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Sha256Hasher" />, covering both
///     file-based SHA-256 computation and text-based shortened hash generation.
/// </summary>
public sealed class Sha256HasherTests
{
    private readonly Sha256Hasher _hasher = new();

    #region Helpers

    private static readonly string UnicodeChars = MakeUnicodeChars();

    private static readonly string AsciiChars = MakeAsciiChars();

    private static string MakeAsciiChars()
    {
        var builder = new StringBuilder(256);
        for (var i = 0; i <= 255; i++) builder.Append((char)i);

        return builder.ToString();
    }

    private static string MakeUnicodeChars()
    {
        var unicodeBuilder = new StringBuilder(65536);
        for (var i = 0; i <= 65535; i++)
        {
            if (i is >= 0xD800 and <= 0xDFFF) continue;
            unicodeBuilder.Append((char)i);
        }

        return unicodeBuilder.ToString();
    }

    private static string GetRandomString(string chars, int length)
    {
        var result = Random.Shared.GetItems(chars.ToCharArray(), length);
        return new string(result);
    }

    public static TheoryData<int> GetRandomHashLengths()
    {
        var max = new Sha256Hasher().MaxLength;
        return
        [
            Random.Shared.Next(1, max),
            max,
            max + Random.Shared.Next(1, max)
        ];
    }

    #endregion

    #region ComputeHash

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHash" /> returns the sentinel value <c>"default"</c>
    ///     when the input text is <see langword="null" /> or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ComputeHash_NullOrEmpty_ReturnsDefault(string? text)
    {
        var result = _hasher.ComputeHash(text!);

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHash" /> returns a lowercase hexadecimal string
    ///     of exactly the requested length for ASCII inputs.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRandomHashLengths))]
    public void ComputeHash_ValidAsciiText_ReturnsCorrectLengthCharHexString(int length)
    {
        var input = GetRandomString(AsciiChars, AsciiChars.Length);
        var result = _hasher.ComputeHash(input, length);

        var expectedLength = Math.Min(length, _hasher.MaxLength);
        result.Should().HaveLength(expectedLength).And.MatchRegex("^[0-9a-f]+$");
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHash" /> returns a lowercase hexadecimal string
    ///     of exactly the requested length for Unicode inputs.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRandomHashLengths))]
    public void ComputeHash_ValidUnicodeText_ReturnsCorrectLengthCharHexString(int length)
    {
        var input = GetRandomString(UnicodeChars, UnicodeChars.Length);
        var result = _hasher.ComputeHash(input, length);

        var expectedLength = Math.Min(length, _hasher.MaxLength);
        result.Should().HaveLength(expectedLength).And.MatchRegex("^[0-9a-f]+$");
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHash" /> is deterministic: identical inputs
    ///     always produce the same hash output for both ASCII and Unicode.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComputeHash_SameInput_ReturnsSameHash(bool useUnicode)
    {
        var charset = useUnicode ? UnicodeChars : AsciiChars;
        var input = GetRandomString(charset, charset.Length);
        var first = _hasher.ComputeHash(input);
        var second = _hasher.ComputeHash(input);

        first.Should().Be(second);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHash" /> produces distinct outputs for distinct inputs.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComputeHash_DifferentInputs_ReturnDifferentHashes(bool useUnicode)
    {
        var charset = useUnicode ? UnicodeChars : AsciiChars;
        var sample = GetRandomString(charset, charset.Length);
        var first = _hasher.ComputeHash(sample + "123");
        var second = _hasher.ComputeHash(sample + "213");

        first.Should().NotBe(second);
    }

    #endregion

    #region ComputeHashFile

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHashFile" /> throws a <see cref="FileNotFoundException" />
    ///     when the specified file does not exist on disk.
    /// </summary>
    [Fact]
    public void ComputeFileHash_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file-xyz-123.dat");

        var act = () => _hasher.ComputeHashFile(nonExistentPath);

        act.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHashFile" /> returns a lowercase hexadecimal string
    ///     of exactly the requested length for a valid existing file.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRandomHashLengths))]
    public void ComputeFileHash_ValidFile_ReturnsCorrectLengthCharHexString(int length)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "sample content for hashing");

            var result = _hasher.ComputeHashFile(tempFile, length);

            var expectedLength = Math.Min(length, _hasher.MaxLength);
            result.Should().HaveLength(expectedLength).And.MatchRegex("^[0-9a-f]+$");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHashFile" /> is deterministic: two calls on the same
    ///     file with identical content produce the same hash.
    /// </summary>
    [Fact]
    public void ComputeFileHash_SameFile_ReturnsSameHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, [0x01, 0x02, 0x03]);

            var first = _hasher.ComputeHashFile(tempFile);
            var second = _hasher.ComputeHashFile(tempFile);

            first.Should().Be(second);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256Hasher.ComputeHashFile" /> produces different hashes for files
    ///     with different content, confirming the underlying SHA-256 algorithm is used correctly.
    /// </summary>
    [Fact]
    public void ComputeFileHash_DifferentFileContent_ReturnsDifferentHashes()
    {
        var fileA = Path.GetTempFileName();
        var fileB = Path.GetTempFileName();
        try
        {
            File.WriteAllText(fileA, "content-a");
            File.WriteAllText(fileB, "content-b");

            var hashA = _hasher.ComputeHashFile(fileA);
            var hashB = _hasher.ComputeHashFile(fileB);

            hashA.Should().NotBe(hashB);
        }
        finally
        {
            File.Delete(fileA);
            File.Delete(fileB);
        }
    }

    #endregion
}