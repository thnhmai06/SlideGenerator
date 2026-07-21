/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: IDownloadedFileCacheTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Utilities;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for the pure, I/O-free <see cref="IDownloadedFileCache.GetPath" /> naming rule.
/// </summary>
public sealed class IDownloadedFileCacheTests
{
    /// <summary>Verifies the path is the resolved URI's hash plus extension under <see cref="NameAndPaths.TempFolder" />.</summary>
    [Fact]
    public void GetPath_UriAndExtension_ReturnsFileUnderTempFolderRoot()
    {
        var uri = new Uri("https://example.com/a.png");

        var result = IDownloadedFileCache.GetPath(uri, ".png");

        result.Should().Be(Path.Combine(NameAndPaths.TempFolder.RootPath, $"{uri.AbsoluteUri.HashText()}.png"));
    }

    /// <summary>Verifies a <see langword="null" /> extension yields a path with no file extension.</summary>
    [Fact]
    public void GetPath_NullExtension_ReturnsFileWithoutExtension()
    {
        var uri = new Uri("https://example.com/a.png");

        var result = IDownloadedFileCache.GetPath(uri, null);

        result.Should().Be(Path.Combine(NameAndPaths.TempFolder.RootPath, uri.AbsoluteUri.HashText()));
    }

    /// <summary>Verifies two different URIs hash to different paths.</summary>
    [Fact]
    public void GetPath_DifferentUris_ReturnDifferentPaths()
    {
        var pathA = IDownloadedFileCache.GetPath(new Uri("https://example.com/a.png"), ".png");
        var pathB = IDownloadedFileCache.GetPath(new Uri("https://example.com/b.png"), ".png");

        pathA.Should().NotBe(pathB);
    }
}
