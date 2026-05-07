/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: SharePointResolverTests.cs
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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SlideGenerator.Cloud.Resolvers;
using Xunit;

namespace SlideGenerator.Tests.Cloud;

public sealed class SharePointResolverTests
{
    private readonly SharePointResolver _resolver;

    public SharePointResolverTests()
    {
        var loggerMock = new Mock<ILogger>();
        _resolver = new SharePointResolver(loggerMock.Object);
    }

    [Fact]
    public void IsUriSupported_ShouldReturnTrue_ForSharePointHost()
    {
        _resolver.IsUriSupported(new Uri("https://tenant.sharepoint.com/site")).Should().BeTrue();
    }

    [Fact]
    public async Task ResolveUriAsync_ShouldExtractIdAndAppendDownloadParam()
    {
        // Arrange
        var input = "https://tenant.sharepoint.com/:i:/s/site/Abc?id=%2Fsites%2Ftest%2Ffile.png";
        var uri = new Uri(input);

        // Act
        var result = await _resolver.ResolveUriAsync(uri, new HttpClient());

        // Assert
        result.AbsoluteUri.Should().Be("https://tenant.sharepoint.com/sites/test/file.png?download=1");
    }

    [Fact]
    public async Task ResolveUriAsync_ShouldReturnOriginal_WhenIdMissing()
    {
        var uri = new Uri("https://tenant.sharepoint.com/no-id");
        var result = await _resolver.ResolveUriAsync(uri, new HttpClient());
        result.Should().Be(uri);
    }
}