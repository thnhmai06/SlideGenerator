/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: ReplaceSlideDataTests.cs
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

using Microsoft.Extensions.Logging;
using Moq;
using SlideGenerator.Document.Slide.Services;
using Xunit;

namespace SlideGenerator.Tests.Document;

public sealed class ReplaceSlideDataTests
{
    private readonly TextComposer _textComposer;

    public ReplaceSlideDataTests()
    {
        var loggerMock = new Mock<ILogger<TextComposer>>();
        _textComposer = new TextComposer(loggerMock.Object);
    }

    [Fact(Skip = "INTEGRATION: requires Syncfusion license and complex mocking of IShape")]
    public void Replace_ShouldCorrectlyRenderMustache_WhenDataProvided()
    {
        // Scenario 1: "Hello, {{Name}}!" + {Name: "An"} -> "Hello, An!"
        // Scenario 2: "{{Missing}}" -> ""
        // Scenario 3: No tags -> unchanged
    }
}