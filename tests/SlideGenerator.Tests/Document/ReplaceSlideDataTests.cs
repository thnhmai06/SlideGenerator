/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: ReplaceSlideDataTests.cs
 */

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SlideGenerator.Document.Slide.Services;
using Syncfusion.Presentation;
using Xunit;

namespace SlideGenerator.Tests.Document;

public sealed class ReplaceSlideDataTests
{
    private readonly TextComposer _textComposer;
    private readonly Mock<ILogger<TextComposer>> _loggerMock;

    public ReplaceSlideDataTests()
    {
        _loggerMock = new Mock<ILogger<TextComposer>>();
        _textComposer = new TextComposer(_loggerMock.Object);
    }

    [Fact(Skip = "INTEGRATION: requires Syncfusion license and complex mocking of IShape")]
    public void Replace_ShouldCorrectlyRenderMustache_WhenDataProvided()
    {
        // Scenario 1: "Hello, {{Name}}!" + {Name: "An"} -> "Hello, An!"
        // Scenario 2: "{{Missing}}" -> ""
        // Scenario 3: No tags -> unchanged
    }
}
