/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: OneDriveResolverTests.cs
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

public sealed class OneDriveResolverTests
{
    private readonly OneDriveResolver _resolver;

    public OneDriveResolverTests()
    {
        var loggerMock = new Mock<ILogger>();
        _resolver = new OneDriveResolver(loggerMock.Object);
    }

    [Theory]
    [InlineData("https://1drv.ms/u/s!Abc123xyz")]
    [InlineData("https://onedrive.live.com/redir?resid=123")]
    public void IsUriSupported_ShouldReturnTrue_ForOneDriveHosts(string url)
    {
        _resolver.IsUriSupported(new Uri(url)).Should().BeTrue();
    }

    [Fact]
    public async Task ResolveUriAsync_ShouldReturnApiLink_WithBase64EncodedUrl()
    {
        // Arrange
        var inputUrl = "https://1drv.ms/u/s!TestUrl";
        var uri = new Uri(inputUrl);

        // Act
        var result = await _resolver.ResolveUriAsync(uri, new HttpClient());

        // Assert
        result.AbsoluteUri.Should().StartWith("https://api.onedrive.com/v1.0/shares/u!");
        result.AbsoluteUri.Should().EndWith("/root/content");
    }
}