/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: ModelIdentifierTests.cs
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

public sealed class ModelIdentifierTests
{
    [Fact]
    public void BookIdentifier_ShouldNormalizePath()
    {
        var id = new BookIdentifier("data/./test.xlsx");
        // Path.GetFullPath will normalize it. We check it doesn't contain relative segments.
        id.BookPath.Should().NotContain("/./").And.NotContain("\\.\\");
    }

    [Fact]
    public void SheetIdentifier_ShouldInheritBookPath()
    {
        var id = new SheetIdentifier("test.xlsx", "Sheet1");
        id.BookPath.Should().EndWith("test.xlsx");
        id.SheetName.Should().Be("Sheet1");
    }

    [Fact]
    public void PresentationIdentifier_ShouldNormalizePath()
    {
        var id = new PresentationIdentifier("out/../template.pptx");
        id.PresentationPath.Should().NotContain("/../").And.NotContain("\\..\\");
    }

    [Fact]
    public void ShapeIdentifier_ShouldStorePropertiesCorrectly()
    {
        var id = new ShapeIdentifier("temp.pptx", 1, "Shape1", "password");
        id.PresentationPath.Should().EndWith("temp.pptx");
        id.SlideIndex.Should().Be(1);
        id.ShapeName.Should().Be("Shape1");
        id.PresentationPassword.Should().Be("password");
    }

    [Fact]
    public void BookType_ShouldResolveFromExtension()
    {
        BookTypeExtensions.FromExtension(".xlsx").Should().Be(BookType.Xlsx);
        BookTypeExtensions.FromExtension(".csv").Should().Be(BookType.Csv);

        Action act = () => BookTypeExtensions.FromExtension(".unknown");
        act.Should().Throw<ArgumentException>();
    }
}