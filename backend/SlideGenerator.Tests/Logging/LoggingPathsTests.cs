/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: LoggingPathsTests.cs
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