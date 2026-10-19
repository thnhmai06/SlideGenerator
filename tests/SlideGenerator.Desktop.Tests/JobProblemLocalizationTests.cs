/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: JobProblemLocalizationTests.cs
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
///     Verifies the run dialog's job-problem keys (and the non-blocking overwrite conflict label) exist with
///     non-empty values in both <c>.resx</c> files — read as plain XML rather than through
///     <c>LocalizationService.SetLanguage</c>, which mutates process-wide culture state and would race the other
///     tests in this assembly.
/// </summary>
public sealed class JobProblemLocalizationTests
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

    /// <summary>
    ///     Every <see cref="JobProblem" /> member except <see cref="JobProblem.None" /> resolves to a non-empty value
    ///     that is not the key itself, in both cultures.
    /// </summary>
    [Theory]
    [InlineData("Resources.resx")]
    [InlineData("Resources.vi.resx")]
    public void Resources_EveryJobProblem_HasLocalizedText(string fileName)
    {
        var resources = ReadResources(fileName);

        foreach (var problem in Enum.GetValues<JobProblem>().Where(p => p != JobProblem.None))
        {
            var camel = char.ToLowerInvariant(problem.ToString()[0]) + problem.ToString()[1..];
            var key = $"enums.jobProblem.{camel}";

            resources.Should().ContainKey(key);
            resources[key].Should().NotBeNullOrWhiteSpace();
            resources[key].Should().NotBe(key);
        }
    }

    /// <summary>The overwrite warning's conflict-kind label is present in both cultures.</summary>
    [Theory]
    [InlineData("Resources.resx")]
    [InlineData("Resources.vi.resx")]
    public void Resources_OverwritesExistingFile_HasLocalizedText(string fileName)
    {
        var resources = ReadResources(fileName);

        resources.Should().ContainKey("enums.conflictKind.overwritesExistingFile");
        resources["enums.conflictKind.overwritesExistingFile"].Should().NotBeNullOrWhiteSpace();
    }
}
