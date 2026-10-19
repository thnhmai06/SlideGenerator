/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: RequestStatusLocalizationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Xml.Linq;
using FluentAssertions;
using SlideGenerator.Generator;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Verifies the request-level status keys (and the Runs "Failed" chip label) exist with non-empty values in
///     both <c>.resx</c> files — read as plain XML rather than through <c>LocalizationService.SetLanguage</c>,
///     which mutates process-wide culture state and would race the other tests in this assembly.
/// </summary>
public sealed class RequestStatusLocalizationTests
{
    private static readonly string LocalizationDirectory = FindLocalizationDirectory();

    private static string FindLocalizationDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "SlideGenerator.Desktop", "Services",
                "Localization");
            if (Directory.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Desktop localization directory above '{AppContext.BaseDirectory}'.");
    }

    private static IReadOnlyDictionary<string, string> ReadResources(string fileName)
    {
        var document = XDocument.Load(Path.Combine(LocalizationDirectory, fileName));
        return document.Root!
            .Elements("data")
            .ToDictionary(
                data => data.Attribute("name")!.Value,
                data => data.Element("value")?.Value ?? "");
    }

    /// <summary>Every <see cref="RequestStatus" /> member resolves to a non-empty value that is not the key itself, in both cultures.</summary>
    [Theory]
    [InlineData("Resources.resx")]
    [InlineData("Resources.vi.resx")]
    public void Resources_EveryRequestStatus_HasLocalizedText(string fileName)
    {
        var resources = ReadResources(fileName);

        foreach (var status in Enum.GetValues<RequestStatus>())
        {
            var camel = char.ToLowerInvariant(status.ToString()[0]) + status.ToString()[1..];
            var key = $"enums.requestStatus.{camel}";

            resources.Should().ContainKey(key);
            resources[key].Should().NotBeNullOrWhiteSpace();
            resources[key].Should().NotBe(key);
        }
    }

    /// <summary>The Runs filter chip label for failed requests is present in both cultures.</summary>
    [Theory]
    [InlineData("Resources.resx")]
    [InlineData("Resources.vi.resx")]
    public void Resources_FailedFilterLabel_HasLocalizedText(string fileName)
    {
        var resources = ReadResources(fileName);

        resources.Should().ContainKey("runs.filter.failed");
        resources["runs.filter.failed"].Should().NotBeNullOrWhiteSpace();
    }
}
