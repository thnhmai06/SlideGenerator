/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: JobOutputTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SlideGenerator.Generator.Jobs;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="JobOutput" />: the working (partial) path a job writes to and the cleanup
///     used when the job does not finish.
/// </summary>
public sealed class JobOutputTests
{
    private static readonly ILogger Logger = Substitute.For<ILogger>();

    /// <summary>Verifies the working path keeps the presentation extension and sits next to the final output.</summary>
    /// <param name="outputPath">The final output path.</param>
    /// <param name="expected">The expected working path.</param>
    [Theory]
    [InlineData(@"C:\o\a.pptx", @"C:\o\a.partial.pptx")]
    [InlineData(@"C:\o\a.b.pptx", @"C:\o\a.b.partial.pptx")]
    [InlineData(@"C:\o\a.potx", @"C:\o\a.partial.potx")]
    [InlineData(@"C:\o\a.ppsx", @"C:\o\a.partial.ppsx")]
    [InlineData("a.pptx", "a.partial.pptx")]
    [InlineData("name", "name.partial")]
    public void GetPartialPath_GivenOutputPath_ReturnsWorkingPathBesideIt(string outputPath, string expected)
    {
        JobOutput.GetPartialPath(outputPath).Should().Be(Path.GetFullPath(expected));
    }

    /// <summary>Verifies <see cref="JobOutput.DiscardPartial" /> deletes an existing working file.</summary>
    [Fact]
    public void DiscardPartial_PartialFileExists_DeletesIt()
    {
        var dir = CreateTempDir();
        var outputPath = Path.Combine(dir, "output.pptx");
        var partialPath = JobOutput.GetPartialPath(outputPath);
        File.WriteAllText(partialPath, "half-written");

        try
        {
            JobOutput.DiscardPartial(outputPath, Logger);

            File.Exists(partialPath).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies <see cref="JobOutput.DiscardPartial" /> does not throw when no working file exists.</summary>
    [Fact]
    public void DiscardPartial_PartialFileMissing_DoesNotThrow()
    {
        var dir = CreateTempDir();
        var outputPath = Path.Combine(dir, "output.pptx");

        try
        {
            var act = () => JobOutput.DiscardPartial(outputPath, Logger);

            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies <see cref="JobOutput.DiscardPartial" /> leaves the finished result next to it untouched.</summary>
    [Fact]
    public void DiscardPartial_FinalOutputExists_IsNotDeleted()
    {
        var dir = CreateTempDir();
        var outputPath = Path.Combine(dir, "output.pptx");
        File.WriteAllText(outputPath, "previous result");

        try
        {
            JobOutput.DiscardPartial(outputPath, Logger);

            File.Exists(outputPath).Should().BeTrue();
            File.ReadAllText(outputPath).Should().Be("previous result");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
