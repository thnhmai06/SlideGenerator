/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: GoogleDriveResolverTests.cs
 */

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SlideGenerator.Cloud.Resolvers;
using Xunit;

namespace SlideGenerator.Tests.Cloud;

public sealed class GoogleDriveResolverTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly GoogleDriveResolver _resolver;

    public GoogleDriveResolverTests()
    {
        _loggerMock = new Mock<ILogger>();
        _resolver = new GoogleDriveResolver(_loggerMock.Object);
    }

    [Theory]
    [InlineData("https://drive.google.com/file/d/1abc123/view", "1abc123")]
    [InlineData("https://drive.google.com/file/d/xyz-789/view?usp=sharing", "xyz-789")]
    [InlineData("https://drive.google.com/open?id=foo_bar", "foo_bar")]
    public async Task ResolveUriAsync_ShouldExtractFileId_FromSupportedUrls(string inputUrl, string expectedId)
    {
        // Arrange
        var uri = new Uri(inputUrl);
        var httpClient = new HttpClient();

        // Act
        var result = await _resolver.ResolveUriAsync(uri, httpClient);

        // Assert
        result.AbsoluteUri.Should().Contain($"id={expectedId}");
        result.AbsoluteUri.Should().StartWith("https://drive.google.com/uc?export=download");
    }

    [Fact]
    public void IsUriSupported_ShouldReturnTrue_ForGoogleDriveHost()
    {
        _resolver.IsUriSupported(new Uri("https://drive.google.com/test")).Should().BeTrue();
        _resolver.IsUriSupported(new Uri("https://other.com/test")).Should().BeFalse();
    }

    [Fact]
    public async Task ResolveUriAsync_ShouldFetchHtmlAndExtractId_ForFolderUrls()
    {
        // Arrange
        var uri = new Uri("https://drive.google.com/folders/some-folder-id");
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html>... /file/d/extracted-id-from-html/view ...</html>")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var result = await _resolver.ResolveUriAsync(uri, httpClient);

        // Assert
        result.AbsoluteUri.Should().Contain("id=extracted-id-from-html");
    }
}
