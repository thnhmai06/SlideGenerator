/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: TypeExtensionsTests.cs
 */

using System;
using FluentAssertions;
using SlideGenerator.Document.Sheet.Models;
using SlideGenerator.Document.Slide.Models;
using Xunit;

namespace SlideGenerator.Tests.Models;

public sealed class TypeExtensionsTests
{
    [Theory]
    [InlineData(".xlsx", BookType.Xlsx)]
    [InlineData(".csv", BookType.Csv)]
    [InlineData(".XLS", BookType.Xls)]
    public void BookType_FromExtension_ShouldMatch(string ext, BookType expected)
    {
        BookTypeExtensions.FromExtension(ext).Should().Be(expected);
    }

    [Fact]
    public void BookType_FromExtension_ShouldThrow_OnUnsupported()
    {
        Action act = () => BookTypeExtensions.FromExtension(".pdf");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(BookType.Xlsx, ".xlsx")]
    [InlineData(BookType.Csv, ".csv")]
    public void BookType_GetExtension_ShouldMatch(BookType type, string expected)
    {
        type.GetExtension().Should().Be(expected);
    }

    [Theory]
    [InlineData(".pptx", PresentationType.Pptx)]
    [InlineData(".potx", PresentationType.Potx)]
    [InlineData(".PPSX", PresentationType.Ppsx)]
    public void PresentationType_FromExtension_ShouldMatch(string ext, PresentationType expected)
    {
        PresentationTypeExtensions.FromExtension(ext).Should().Be(expected);
    }

    [Fact]
    public void PresentationType_FromExtension_ShouldThrow_OnUnsupported()
    {
        Action act = () => PresentationTypeExtensions.FromExtension(".doc");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(PresentationType.Pptx, ".pptx")]
    [InlineData(PresentationType.Potx, ".potx")]
    public void PresentationType_ToExtension_ShouldMatch(PresentationType type, string expected)
    {
        type.ToExtension().Should().Be(expected);
    }
}
