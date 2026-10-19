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
///     caller-supplied factory function, optionally chosen per HTTP method. Every received method is
///     recorded in <see cref="Methods" /> so tests can assert which HTTP methods were sent.
///     <c>response.RequestMessage</c> is automatically set to the outgoing request when the factory
///     does not populate it, so that <c>response.RequestMessage.RequestUri</c> always reflects the
///     request URI.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<HttpMethod, Func<HttpRequestMessage, HttpResponseMessage>> _byMethod;
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
    private readonly List<HttpMethod> _methods = [];

    /// <summary>
    ///     Initializes a handler whose single factory answers every request, regardless of method.
    /// </summary>
    /// <param name="respond">Factory invoked for each request.</param>
    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : this(respond, new Dictionary<HttpMethod, Func<HttpRequestMessage, HttpResponseMessage>>())
    {
    }

    /// <summary>
    ///     Initializes a handler with a per-method factory dictionary. A request whose method is not
    ///     present falls back to <paramref name="respond" />.
    /// </summary>
    /// <param name="respond">Fallback factory invoked when no method-specific factory is registered.</param>
    /// <param name="byMethod">Method-specific factories.</param>
    public FakeHttpHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        IReadOnlyDictionary<HttpMethod, Func<HttpRequestMessage, HttpResponseMessage>> byMethod)
    {
        _respond = respond;
        _byMethod = new Dictionary<HttpMethod, Func<HttpRequestMessage, HttpResponseMessage>>(byMethod);
    }

    /// <summary>
    ///     HTTP methods received by this handler, in the order they were sent.
    /// </summary>
    public IReadOnlyList<HttpMethod> Methods => _methods;

    /// <summary>
    ///     Records the method, invokes the matching factory, and ensures the returned response
    ///     carries a valid <see cref="HttpResponseMessage.RequestMessage" />.
    ///     Synchronous exceptions thrown by the factory are wrapped in a faulted
    ///     <see cref="Task{T}" /> so that <see cref="HttpClient" /> handles them correctly.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _methods.Add(request.Method);

        try
        {
            var factory = _byMethod.TryGetValue(request.Method, out var specific) ? specific : _respond;
            var response = factory(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            return Task.FromException<HttpResponseMessage>(ex);
        }
    }
}
