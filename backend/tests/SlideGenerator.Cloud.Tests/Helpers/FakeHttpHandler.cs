/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: FakeHttpHandler.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Cloud.Tests.Helpers;

/// <summary>
///     A test double for <see cref="HttpMessageHandler" /> that delegates each request to a
///     caller-supplied factory function.  <c>response.RequestMessage</c> is automatically set
///     to the outgoing request when the factory does not populate it, so that
///     <c>response.RequestMessage.RequestUri</c> always reflects the request URI.
/// </summary>
internal sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    /// <summary>
    ///     Invokes the factory and ensures the returned response carries a valid
    ///     <see cref="HttpResponseMessage.RequestMessage" />.
    ///     Synchronous exceptions thrown by the factory are wrapped in a faulted
    ///     <see cref="Task{T}" /> so that <see cref="HttpClient" /> handles them correctly.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = respond(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            return Task.FromException<HttpResponseMessage>(ex);
        }
    }
}