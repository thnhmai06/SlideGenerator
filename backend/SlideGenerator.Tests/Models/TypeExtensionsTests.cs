/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: TypeExtensionsTests.cs
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