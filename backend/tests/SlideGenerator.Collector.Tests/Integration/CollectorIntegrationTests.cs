/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector.Tests
 * File: CollectorIntegrationTests.cs
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
using System.Globalization;
using CsvHelper;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Collector.Infrastructure.Services;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Collector.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="FileCollector.IsImageSourceAsync" />, driven by
///     <c>Integration/Testcases.csv</c>. For each row whose URL is recognized by the
///     <see cref="CloudResolver" />, verifies that the collector's download decision
///     matches the expected <c>ShouldDownload</c> value. Rows that the resolver does not
///     support are skipped — they are covered separately by <see cref="ResolverIntegrationTests" />.
/// </summary>
public sealed class CollectorIntegrationTests
{
    private static readonly CloudResolver Resolver = new(Substitute.For<ISystemLogger>());

    private static readonly FileCollector Collector = new(
        Substitute.For<IImageFactory>(),
        new RealHttpClientFactory(),
        Substitute.For<ISystemLogger>());

    private static string CsvPath => Path.Combine(
        Path.GetDirectoryName(typeof(CollectorIntegrationTests).Assembly.Location)!,
        "Integration", "Testcases.csv");

    /// <summary>
    ///     Loads all rows from the test CSV file as theory data.
    /// </summary>
    /// <returns>A <see cref="TheoryData{T1,T2,T3,T4}" /> instance with one entry per CSV row.</returns>
    public static TheoryData<string, string, bool, string?> GetTestCases()
    {
        var data = new TheoryData<string, string, bool, string?>();
        using var reader = new StreamReader(CsvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        foreach (var row in csv.GetRecords<TestCaseRow>())
            data.Add(row.Provider, row.Type, row.ShouldDownload, row.Url);
        return data;
    }

    /// <summary>
    ///     For each URL recognized by <see cref="CloudResolver" />, verifies that
    ///     <see cref="FileCollector.IsImageSourceAsync" /> returns a download decision that
    ///     matches the <c>ShouldDownload</c> column. Rows whose URL the resolver does not
    ///     support are skipped (their resolver behavior is tested in
    ///     <see cref="ResolverIntegrationTests" />).
    /// </summary>
    [Theory(Timeout = 30_000)]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetTestCases))]
    public async Task IsImageSource_CsvTestCase_MatchesExpected(
        string provider, string type, bool shouldDownload, string? url)
    {
        var uri = TryNormalizeUri(url);
        if (uri is null) return;

        if (!Resolver.IsUriSupported(uri, out _)) return;

        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        var resolvedUri = await Resolver.ResolveUriAsync(uri, httpClient, TestContext.Current.CancellationToken);

        var (isValid, _) = await Collector
            .IsImageSourceAsync(resolvedUri.ToString(), TestContext.Current.CancellationToken);

        isValid.Should().Be(shouldDownload, $"{provider}: {type}");
    }

    /// <summary>
    ///     Normalizes a raw URL string to an absolute <see cref="Uri" />, adding the
    ///     <c>https://</c> scheme if absent. Returns <see langword="null" /> for
    ///     empty, whitespace, or completely non-parseable strings.
    /// </summary>
    private static Uri? TryNormalizeUri(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        if (!trimmed.Contains("://")) trimmed = "https://" + trimmed;
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ? uri : null;
    }

    private sealed class RealHttpClientFactory : IHttpClientFactory
    {
        /// <inheritdoc />
        public HttpClient CreateClient(string name) =>
            new(new HttpClientHandler { AllowAutoRedirect = true });
    }
}
