/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: CloudResolverIntegrationTests.cs
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
using SlideGenerator.Cloud.Infrastructure.Services;
using SlideGenerator.Cloud.Tests.Helpers;
using SlideGenerator.Cloud.Tests.Integration.Models;
using Xunit;

namespace SlideGenerator.Cloud.Tests.Integration;

/// <summary>
///     Integration tests that drive the full resolve-then-inspect pipeline against live URLs
///     defined in <c>Testcases.csv</c>.  Each test row represents one real-world scenario and
///     asserts whether the pipeline correctly identifies the resource as a downloadable image.
/// </summary>
/// <remarks>
///     These tests require an active internet connection and may be slow or flaky under
///     network constraints.  Filter them out for fast feedback loops with:
///     <code>dotnet test --filter "Category!=Integration"</code>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class CloudResolverIntegrationTests
{
    private static readonly HttpClient SharedHttpClient = new(new HttpClientHandler { AllowAutoRedirect = true });
    private static readonly CloudClient Client = new();
    private static readonly CloudResolver Resolver = new(Client);

    /// <summary>Loads all test cases from <c>Testcases.csv</c>.</summary>
    public static TheoryData<TestCase> LoadCases()
    {
        var data = new TheoryData<TestCase>();
        foreach (var tc in TestCsvLoader.Load(@"Integration\Testcases.csv"))
            data.Add(tc);
        return data;
    }

    /// <summary>
    ///     Verifies that the resolve-then-inspect pipeline produces a result that matches
    ///     the <c>ShouldDownload</c> expectation from the CSV for each test case.
    ///     The pipeline: <c>ResolveAsync</c> → <c>InspectAsync</c> → <c>IsImage()</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(LoadCases))]
    public async Task ResolveAndInspect_MatchesShouldDownload(TestCase tc)
    {
        var resolvedUri = await Resolver.ResolveAsync(tc.Url ?? string.Empty, SharedHttpClient,
            TestContext.Current.CancellationToken);

        var info = resolvedUri is not null
            ? await Client.InspectAsync(resolvedUri, SharedHttpClient, TestContext.Current.CancellationToken)
            : null;

        var actual = info?.IsImage() ?? false;
        actual.Should().Be(tc.ShouldDownload, tc.ToString());
    }
}