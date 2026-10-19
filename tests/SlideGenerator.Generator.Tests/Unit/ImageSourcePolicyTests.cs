/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: ImageSourcePolicyTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Jobs;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="ImageSourcePolicy" />, verifying that only network URLs and — when the
///     request allows them — existing local files are accepted as image sources, while everything else is
///     ignored before any network or file-system work happens.
/// </summary>
public sealed class ImageSourcePolicyTests : IDisposable
{
    private readonly string _tempFile;

    /// <summary>Creates a real temp file that the local-path facts can point at.</summary>
    public ImageSourcePolicyTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"slidegen-policy-{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(_tempFile, [0xFF, 0xD8, 0xFF, 0xD9]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    /// <summary>Verifies that each cell value maps to the expected kind, with a URL only for accepted URLs.</summary>
    [Theory]
    [InlineData("https://example.com/a.jpg", "Url")]
    [InlineData("http://example.com/a.jpg", "Url")]
    [InlineData("example.com/a.jpg", "Url")]
    [InlineData(" https://example.com/a.jpg ", "Url")]
    [InlineData("192.168.0.1/a.jpg", "Url")]
    [InlineData("ftp://example.com/a.jpg", "Ignored")]
    [InlineData("file:///C:/a.jpg", "Ignored")]
    [InlineData("\\\\server\\share\\a.jpg", "Ignored")]
    [InlineData("John Smith", "Ignored")]
    [InlineData("abc", "Ignored")]
    [InlineData("localhost/a.jpg", "Ignored")]
    [InlineData("", "Ignored")]
    [InlineData("   ", "Ignored")]
    [InlineData(null, "Ignored")]
    public void Classify_VariousValues_ReturnsExpectedKind(string? source, string expectedKind)
    {
        var expected = Enum.Parse<ImageSourceKind>(expectedKind);

        var kind = ImageSourcePolicy.Classify(source, allowLocalPaths: false, out var url);

        kind.Should().Be(expected);
        if (expected == ImageSourceKind.Url) url.Should().NotBeNull();
        else url.Should().BeNull();
    }

    /// <summary>Verifies that an existing local file is a local-file source when the request allows local paths.</summary>
    [Fact]
    public void Classify_ExistingLocalFileWithLocalPathsAllowed_ReturnsLocalFileWithoutUrl()
    {
        var kind = ImageSourcePolicy.Classify(_tempFile, allowLocalPaths: true, out var url);

        kind.Should().Be(ImageSourceKind.LocalFile);
        url.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that an existing local file is ignored when the request forbids local paths — and that it is
    ///     never reinterpreted as a URL.
    /// </summary>
    [Fact]
    public void Classify_ExistingLocalFileWithLocalPathsForbidden_ReturnsIgnored()
    {
        var kind = ImageSourcePolicy.Classify(_tempFile, allowLocalPaths: false, out var url);

        kind.Should().Be(ImageSourceKind.Ignored);
        url.Should().BeNull();
    }

    /// <summary>Verifies that a path that does not exist is ignored even when local paths are allowed.</summary>
    [Fact]
    public void Classify_MissingLocalFileWithLocalPathsAllowed_ReturnsIgnored()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"slidegen-missing-{Guid.NewGuid():N}.jpg");

        var kind = ImageSourcePolicy.Classify(missing, allowLocalPaths: true, out var url);

        kind.Should().Be(ImageSourceKind.Ignored);
        url.Should().BeNull();
    }
}
