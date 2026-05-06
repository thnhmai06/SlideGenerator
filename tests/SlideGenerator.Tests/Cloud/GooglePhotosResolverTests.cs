/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: GooglePhotosResolverTests.cs
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

public sealed class GooglePhotosResolverTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly GooglePhotosResolver _resolver;

    public GooglePhotosResolverTests()
    {
        _loggerMock = new Mock<ILogger>();
        _resolver = new GooglePhotosResolver(_loggerMock.Object);
    }

    [Fact]
    public void IsUriSupported_ShouldReturnTrue_ForGooglePhotosHosts()
    {
        _resolver.IsUriSupported(new Uri("https://photos.app.goo.gl/abc")).Should().BeTrue();
        _resolver.IsUriSupported(new Uri("https://photos.google.com/album")).Should().BeTrue();
        _resolver.IsUriSupported(new Uri("https://lh3.googleusercontent.com/pw/xyz")).Should().BeTrue();
    }

    [Fact]
    public async Task ResolveUriAsync_ShouldExtractDirectLink_FromHtml()
    {
        // Arrange
        var uri = new Uri("https://photos.app.goo.gl/test");
        var directLink = "https://lh3.googleusercontent.com/pw/AM-JKLX";
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"<html><body><img src=\"{directLink}\"></body></html>")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var result = await _resolver.ResolveUriAsync(uri, httpClient);

        // Assert
        result.AbsoluteUri.Should().Be(directLink + "=d");
    }

    [Fact]
    public async Task ResolveUriAsync_ShouldReturnOriginal_WhenNoMatchFound()
    {
        // Arrange
        var uri = new Uri("https://photos.app.goo.gl/test");
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html>no link here</html>")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var result = await _resolver.ResolveUriAsync(uri, httpClient);

        // Assert
        result.Should().Be(uri);
    }
}
