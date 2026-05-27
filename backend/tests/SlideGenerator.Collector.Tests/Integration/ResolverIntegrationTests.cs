/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector.Tests
 * File: ResolverIntegrationTests.cs
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
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Collector.Infrastructure.Services;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Collector.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="CloudResolver.IsUriSupported" />, driven by
///     <c>Integration/Testcases.csv</c>. Verifies that each URL is recognized by the
///     correct cloud provider resolver (or rejected for unrecognized sources).
/// </summary>
public sealed class ResolverIntegrationTests
{
    private static readonly CloudResolver Resolver = new(Substitute.For<ISystemLogger>());

    private static string CsvPath => Path.Combine(
        Path.GetDirectoryName(typeof(ResolverIntegrationTests).Assembly.Location)!,
        "Integration", "Testcases.csv");

    /// <summary>
    ///     Loads all rows from the test CSV file as theory data.
    /// </summary>
    /// <returns>A <see cref="TheoryData{T1,T2,T3}" /> instance with one entry per CSV row.</returns>
    public static TheoryData<string, string, string?> GetTestCases()
    {
        var data = new TheoryData<string, string, string?>();
        using var reader = new StreamReader(CsvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        foreach (var row in csv.GetRecords<TestCaseRow>())
            data.Add(row.Provider, row.Type, row.Url);
        return data;
    }

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.IsUriSupported" /> returns the correct result and
    ///     provider key for each URL in the test CSV. "Raw" URLs must be rejected; cloud provider
    ///     URLs must be recognized with the matching <see cref="CloudResolverKey" />.
    ///     Unparseable URLs (empty or non-URI strings) are skipped — they cannot be passed to
    ///     <see cref="CloudResolver.IsUriSupported" /> which requires a valid <see cref="Uri" />.
    /// </summary>
    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetTestCases))]
    public void IsUriSupported_CsvTestCase_MatchesProvider(
        string provider, string type, string? url)
    {
        var uri = TryNormalizeUri(url);
        if (uri is null)
            return; // Unparseable URL: cannot construct a Uri, so IsUriSupported is not callable

        var result = Resolver.IsUriSupported(uri, out var key);

        switch (provider)
        {
            case "Google Drive":
                result.Should().BeTrue($"{provider}: {type}");
                key.Should().Be(CloudResolverKey.GoogleDrive, $"{provider}: {type}");
                break;

            case "Google Photos":
                result.Should().BeTrue($"{provider}: {type}");
                key.Should().Be(CloudResolverKey.GooglePhotos, $"{provider}: {type}");
                break;

            case "OneDrive":
                result.Should().BeTrue($"{provider}: {type}");
                key.Should().Be(CloudResolverKey.OneDrive, $"{provider}: {type}");
                break;

            default: // "Raw" and any unknown providers
                result.Should().BeFalse($"{provider}: {type}");
                break;
        }
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
}
