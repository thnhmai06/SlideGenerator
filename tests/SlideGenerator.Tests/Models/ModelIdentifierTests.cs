/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: ModelIdentifierTests.cs
 */

using System;
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
