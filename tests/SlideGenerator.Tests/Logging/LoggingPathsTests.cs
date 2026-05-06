/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: LoggingPathsTests.cs
 */

using System.IO;
using FluentAssertions;
using SlideGenerator.Logging;
using Xunit;

namespace SlideGenerator.Tests.Logging;

public sealed class LoggingPathsTests
{
    [Fact]
    public void LogFolderPath_ShouldBeInBaseDirectory()
    {
        var path = LoggingPaths.LogFolderPath;
        path.Should().EndWith("Logs");
        Directory.Exists(Path.GetDirectoryName(path)).Should().BeTrue();
    }
}
