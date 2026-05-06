/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: SharePointResolverTests.cs
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

public sealed class SharePointResolverTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly SharePointResolver _resolver;

    public SharePointResolverTests()
    {
        _loggerMock = new Mock<ILogger>();
        _resolver = new SharePointResolver(_loggerMock.Object);
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
