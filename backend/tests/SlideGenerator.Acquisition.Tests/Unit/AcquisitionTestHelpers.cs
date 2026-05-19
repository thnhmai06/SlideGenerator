/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition.Tests
 * File: AcquisitionTestHelpers.cs
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

using System.Net;

namespace SlideGenerator.Acquisition.Tests.Unit;

/// <summary>
///     Provides shared test infrastructure for acquisition unit tests, including a fake
///     <see cref="HttpMessageHandler" /> that returns controlled HTTP responses.
/// </summary>
internal static class AcquisitionTestHelpers
{
    /// <summary>
    ///     Creates an <see cref="HttpClient" /> backed by a fake handler that always returns
    ///     <paramref name="responseBody" /> with the given <paramref name="statusCode" />.
    /// </summary>
    /// <param name="responseBody">The response body content the fake handler returns.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <returns>A configured <see cref="HttpClient" /> suitable for unit testing.</returns>
    internal static HttpClient CreateFakeHttpClient(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new FakeMessageHandler(responseBody, statusCode));
    }

    private sealed class FakeMessageHandler(
        string responseBody,
        HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            };
            return Task.FromResult(response);
        }
    }
}