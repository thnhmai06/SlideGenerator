/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: OneDriveResolverTests.cs
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SlideGenerator.Cloud.Resolvers;
using Xunit;

namespace SlideGenerator.Tests.Cloud;

public sealed class OneDriveResolverTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly OneDriveResolver _resolver;

    public OneDriveResolverTests()
    {
        _loggerMock = new Mock<ILogger>();
        _resolver = new OneDriveResolver(_loggerMock.Object);
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
