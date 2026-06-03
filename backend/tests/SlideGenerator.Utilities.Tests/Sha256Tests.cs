/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities.Tests
 * File: Sha256Tests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text;
using FluentAssertions;
using Xunit;

namespace SlideGenerator.Utilities.Tests;

/// <summary>
///     Unit tests for the <see cref="Sha256" /> utility class, covering file hashing and text hashing.
/// </summary>
public sealed class Sha256Tests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    /// <summary>Cleans up all temporary files created during the test run.</summary>
    public void Dispose()
    {
        foreach (var f in _tempFiles.Where(File.Exists)) File.Delete(f);
    }

    private string NewTempFile(string content)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static string RandomString() => Guid.NewGuid().ToString("N");

    #region HashFile

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashFile" /> returns a 64-character lowercase hex string
    ///     that differs from the original content.
    /// </summary>
    [Fact]
    public void HashFile_ExistingFile_Returns64CharHexStringNotEqualToContent()
    {
        var content = RandomString();
        var path = NewTempFile(content);

        var result = Sha256.HashFile(path);

        result.Should().HaveLength(Sha256.MaxLength);
        result.Should().MatchRegex("^[0-9a-f]+$");
        result.Should().NotBe(content);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashFile" /> produces identical hashes for two files
    ///     that contain the same content.
    /// </summary>
    [Fact]
    public void HashFile_SameContentTwoFiles_ReturnsSameHash()
    {
        var content = RandomString();
        var path1 = NewTempFile(content);
        var path2 = NewTempFile(content);

        var hash1 = Sha256.HashFile(path1);
        var hash2 = Sha256.HashFile(path2);

        hash1.Should().Be(hash2);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashFile" /> produces different hashes for files with different content.
    /// </summary>
    [Fact]
    public void HashFile_DifferentContent_ReturnsDifferentHash()
    {
        var path1 = NewTempFile(RandomString());
        var path2 = NewTempFile(RandomString());

        var hash1 = Sha256.HashFile(path1);
        var hash2 = Sha256.HashFile(path2);

        hash1.Should().NotBe(hash2);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashFile" /> throws <see cref="FileNotFoundException" />
    ///     when the specified file does not exist.
    /// </summary>
    [Fact]
    public void HashFile_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        var act = () => Sha256.HashFile(nonExistent);

        act.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashFile" /> returns a hash truncated to the requested length
    ///     when <paramref name="length" /> is less than <see cref="Sha256.MaxLength" />.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(63)]
    public void HashFile_WithShorterLength_ReturnsTruncatedHash(int length)
    {
        var path = NewTempFile(RandomString());

        var result = Sha256.HashFile(path, length);

        result.Should().HaveLength(length);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashFile" /> clamps the hash length to
    ///     <see cref="Sha256.MaxLength" /> when the requested length exceeds the maximum.
    /// </summary>
    [Fact]
    public void HashFile_LengthExceedsMaxLength_ClampsToMaxLength()
    {
        var path = NewTempFile(RandomString());

        var result = Sha256.HashFile(path, Sha256.MaxLength + 100);

        result.Should().HaveLength(Sha256.MaxLength);
    }

    #endregion

    #region HashText

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> returns <see cref="string.Empty" />
    ///     for an empty string input.
    /// </summary>
    [Fact]
    public void HashText_EmptyString_ReturnsEmpty()
    {
        var result = string.Empty.HashText();

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> returns <see cref="string.Empty" />
    ///     when called on a <see langword="null" /> string.
    /// </summary>
    [Fact]
    public void HashText_NullString_ReturnsEmpty()
    {
        var result = ((string)null!).HashText();

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> returns a 64-character lowercase hex string
    ///     that differs from the original input.
    /// </summary>
    [Fact]
    public void HashText_ValidText_Returns64CharHexStringNotEqualToInput()
    {
        var text = RandomString();

        var result = text.HashText();

        result.Should().HaveLength(Sha256.MaxLength);
        result.Should().MatchRegex("^[0-9a-f]+$");
        result.Should().NotBe(text);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> produces identical hashes for identical inputs.
    /// </summary>
    [Fact]
    public void HashText_SameInput_ReturnsSameHash()
    {
        var text = RandomString();

        var hash1 = text.HashText();
        var hash2 = text.HashText();

        hash1.Should().Be(hash2);
        hash1.Should().NotBe(text);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> produces different hashes for different inputs.
    /// </summary>
    [Fact]
    public void HashText_DifferentInputs_ReturnsDifferentHashes()
    {
        var text1 = RandomString();
        var text2 = RandomString();

        var hash1 = text1.HashText();
        var hash2 = text2.HashText();

        hash1.Should().NotBe(hash2);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> returns a hash truncated to the requested length
    ///     when <paramref name="length" /> is less than <see cref="Sha256.MaxLength" />.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(63)]
    public void HashText_WithShorterLength_ReturnsTruncatedHash(int length)
    {
        var result = RandomString().HashText(length);

        result.Should().HaveLength(length);
    }

    /// <summary>
    ///     Verifies that <see cref="Sha256.HashText" /> clamps the hash length to
    ///     <see cref="Sha256.MaxLength" /> when the requested length exceeds the maximum.
    /// </summary>
    [Fact]
    public void HashText_LengthExceedsMaxLength_ClampsToMaxLength()
    {
        var result = RandomString().HashText(Sha256.MaxLength + 100);

        result.Should().HaveLength(Sha256.MaxLength);
    }

    #endregion
}
