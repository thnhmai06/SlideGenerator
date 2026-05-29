/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: TextComposerTests.cs
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

using System.Drawing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Document.Application.Services;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Document.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="TextComposer" />, verifying the six-step compose algorithm:
///     flatten → scan → build marked template → render → parse chunks → reconstruct shape.
///     Tests use hand-coded <see cref="FakeShape" />, <see cref="FakeParagraph" />, and
///     <see cref="FakeTextPart" /> stubs to provide mutable state without mocking overhead.
/// </summary>
public sealed class TextComposerTests
{
    private readonly TextComposer _composer =
        new(new MustacheEngine(NullLogger<MustacheEngine>.Instance));

    #region Helpers

    private static FakeShape ShapeWithParagraphs(params string[][] paragraphTexts)
    {
        var shape = new FakeShape();
        foreach (var parts in paragraphTexts)
            shape.AddParagraphWith(parts);
        return shape;
    }

    private static Dictionary<string, string> Values(params (string key, string value)[] pairs)
    {
        return pairs.ToDictionary(p => p.key, p => p.value);
    }

    #endregion

    #region No-op cases

    /// <summary>
    ///     Verifies that <see cref="TextComposer.Compose" /> does nothing when the shape has no paragraphs.
    /// </summary>
    [Fact]
    public void Compose_ShapeHasNoParagraphs_ShapeUnchanged()
    {
        var shape = new FakeShape();

        _composer.Compose(shape, Values(("name", "World")));

        shape.ParagraphsCount.Should().Be(0);
    }

    /// <summary>
    ///     Verifies that <see cref="TextComposer.Compose" /> does nothing when the resolved-value
    ///     dictionary is empty.
    /// </summary>
    [Fact]
    public void Compose_EmptyResolvedValues_ShapeUnchanged()
    {
        var shape = ShapeWithParagraphs(["Hello {{name}}"]);

        _composer.Compose(shape, new Dictionary<string, string>());

        shape.ParagraphsCount.Should().Be(1);
        shape.AllText.Should().Be("Hello {{name}}");
    }

    #endregion

    #region Single placeholder in single TextPart

    /// <summary>
    ///     Verifies that a single placeholder in a single <see cref="ITextPart" /> is replaced
    ///     with the provided value.
    /// </summary>
    [Fact]
    public void Compose_SinglePlaceholderInSinglePart_IsReplaced()
    {
        var shape = ShapeWithParagraphs(["Hello {{name}}!"]);

        _composer.Compose(shape, Values(("name", "World")));

        shape.AllText.Should().Be("Hello World!");
    }

    /// <summary>
    ///     Verifies that static text with no placeholder remains unchanged after composition.
    /// </summary>
    [Fact]
    public void Compose_StaticTextOnlyParagraph_TextPreserved()
    {
        var shape = ShapeWithParagraphs(["No placeholders here"]);

        _composer.Compose(shape, Values(("key", "value")));

        shape.AllText.Should().Be("No placeholders here");
    }

    #endregion

    #region Multi-part paragraph

    /// <summary>
    ///     Verifies that when a placeholder is split across two <see cref="ITextPart" /> instances
    ///     (2 chars in part 0, 6 chars in part 1, total tag length 8), the rendered value is
    ///     distributed proportionally — 25 % to the first output part, 75 % to the second —
    ///     so that each output part inherits the formatting source of the original part it overlaps.
    /// </summary>
    [Fact]
    public void Compose_PlaceholderSpanningMultipleParts_RatioPreservedInOutputParts()
    {
        // "{{" (2 chars) + "name}}" (6 chars) → tag coverage ratio 2:6 = 0.25: 0.75
        // "Alice" (5 chars) → round(0.25×5)=1 char in part 0, remaining 4 chars in part 1
        var shape = ShapeWithParagraphs(["{{", "name}}"]);

        _composer.Compose(shape, Values(("name", "Alice")));

        var parts = shape.Paragraph.Cast<FakeParagraph>().Single().TextParts.Cast<FakeTextPart>().ToList();
        parts.Should().HaveCount(2);
        parts[0].Text.Should().Be("A");
        parts[1].Text.Should().Be("lice");
    }

    /// <summary>
    ///     Verifies that a paragraph with multiple text parts containing separate placeholders
    ///     has all placeholders replaced independently.
    /// </summary>
    [Fact]
    public void Compose_MultiplePlaceholdersInOneParagraph_AllReplaced()
    {
        var shape = ShapeWithParagraphs(["{{first}} and {{second}}"]);

        _composer.Compose(shape, Values(("first", "A"), ("second", "B")));

        shape.AllText.Should().Be("A and B");
    }

    #endregion

    #region Multi-paragraph shape

    /// <summary>
    ///     Verifies that a shape with multiple paragraphs produces the same number of output paragraphs
    ///     after composition when no section expansion occurs.
    /// </summary>
    [Fact]
    public void Compose_MultiParagraphShape_ParagraphCountPreserved()
    {
        var shape = ShapeWithParagraphs(
            ["Line {{a}}"],
            ["Line {{b}}"]);

        _composer.Compose(shape, Values(("a", "1"), ("b", "2")));

        shape.ParagraphsCount.Should().Be(2);
    }

    /// <summary>
    ///     Verifies that the text in each paragraph of a multi-paragraph shape is independently
    ///     replaced with the correct value.
    /// </summary>
    [Fact]
    public void Compose_MultiParagraphShape_EachParagraphReplaced()
    {
        var shape = ShapeWithParagraphs(
            ["Hello {{name}}"],
            ["Score: {{score}}"]);

        _composer.Compose(shape, Values(("name", "Bob"), ("score", "100")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        paragraphs[0].AllText().Should().Be("Hello Bob");
        paragraphs[1].AllText().Should().Be("Score: 100");
    }

    #endregion

    #region Cross-paragraph section and placeholder

    /// <summary>
    ///     Verifies that a Mustache section whose open tag, body, and close tag each occupy a separate
    ///     paragraph is expanded into one output paragraph per list item. The section paragraph template
    ///     (body paragraph) is replicated once per item; the open and close tag paragraphs produce no
    ///     output since their content is structural and renders empty.
    /// </summary>
    [Fact]
    public void Compose_SectionSpanningMultipleParagraphs_ExpandsIntoSeparateParagraphs()
    {
        // Para 0: open tag; Para 1: body; Para 2: close tag
        var shape = ShapeWithParagraphs(
            ["{{#items}}"],
            ["- {{.}}"],
            ["{{/items}}"]);

        _composer.Compose(shape, Values(("items", "a,b")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        shape.ParagraphsCount.Should().Be(2);
        paragraphs[0].AllText().Should().Be("- a");
        paragraphs[1].AllText().Should().Be("- b");
    }

    /// <summary>
    ///     Verifies that a Mustache section whose open tag is split across multiple <see cref="ITextPart" />
    ///     instances in paragraph 0, and whose close tag is split across multiple <see cref="ITextPart" />
    ///     instances in paragraph 2, is still correctly expanded — producing one output paragraph per list
    ///     item from the body paragraph (paragraph 1).
    /// </summary>
    [Fact]
    public void Compose_SectionTagSplitAcrossPartsAndSpansMultipleParagraphs_ExpandsCorrectly()
    {
        // Para 0: open tag split across two TextParts ("{{#ite" + "ms}}")
        // Para 1: body with variable
        // Para 2: close tag split across two TextParts ("{{/" + "items}}")
        var shape = ShapeWithParagraphs(
            ["{{#ite", "ms}}"],
            ["item: {{.}} "],
            ["{{/", "items}}"]);

        _composer.Compose(shape, Values(("items", "x,y")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        shape.ParagraphsCount.Should().Be(2);
        paragraphs[0].AllText().Should().Be("item: x ");
        paragraphs[1].AllText().Should().Be("item: y ");
    }

    /// <summary>
    ///     Verifies that when a cross-section body placeholder (<c>{{.}}</c>) is split across three
    ///     <see cref="ITextPart" /> instances ("{{" 2 chars, "." 1 char, "}}" 2 chars, total tag 5 chars),
    ///     the rendered value for each list item is distributed proportionally across the same three
    ///     output parts — coverage ratio 2:1:2 = 0.4: 0.2: 0.4. The section itself spans three
    ///     paragraphs (open tag / body / close tag).
    /// </summary>
    [Fact]
    public void Compose_SectionBodyWithSplitPlaceholderInValues_RatioPreservedAcrossItems()
    {
        // Para 0: section open; Para 1: body with {{.}} split 2+1+2; Para 2: section close
        // Tag coverage ratio: part0=2/5=0.4, part1=1/5=0.2, part2=2/5=0.4
        // "Alice"(5): round(0.4×5)=2→"Al", round(0.2×5)=1→"i", 5-3=2→"ce"
        // "Bob"(3):   round(0.4×3)=1→"B",  round(0.2×3)=1→"o",  3-2=1→"b"
        var shape = ShapeWithParagraphs(
            ["{{#items}}"],
            ["{{", ".", "}}"],
            ["{{/items}}"]);

        _composer.Compose(shape, Values(("items", "Alice,Bob")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        shape.ParagraphsCount.Should().Be(2);

        var parts0 = paragraphs[0].TextParts.Cast<FakeTextPart>().ToList();
        parts0.Should().HaveCount(3);
        parts0[0].Text.Should().Be("Al");
        parts0[1].Text.Should().Be("i");
        parts0[2].Text.Should().Be("ce");

        var parts1 = paragraphs[1].TextParts.Cast<FakeTextPart>().ToList();
        parts1.Should().HaveCount(3);
        parts1[0].Text.Should().Be("B");
        parts1[1].Text.Should().Be("o");
        parts1[2].Text.Should().Be("b");
    }

    /// <summary>
    ///     Verifies that when the body paragraph of a cross-paragraph section contains a split
    ///     placeholder whose key is absent from the resolved-value dictionary, the placeholder
    ///     renders as empty and the static prefix text is preserved in each expanded paragraph.
    ///     Only the static-text part produces output; no TextPart is added for the empty placeholder.
    /// </summary>
    [Fact]
    public void Compose_SectionBodyWithSplitPlaceholderAbsentFromValues_StaticPrefixPreserved()
    {
        // Para 0: section open; Para 1: "Static: " + split {{missing}}; Para 2: section close
        // "missing" is not in values → Mustache renders it as "" → only static part survives
        var shape = ShapeWithParagraphs(
            ["{{#items}}"],
            ["Static: {{", "missing}}"],
            ["{{/items}}"]);

        _composer.Compose(shape, Values(("items", "x,y")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        shape.ParagraphsCount.Should().Be(2);

        var parts0 = paragraphs[0].TextParts.Cast<FakeTextPart>().ToList();
        parts0.Should().HaveCount(1);
        parts0[0].Text.Should().Be("Static: ");

        var parts1 = paragraphs[1].TextParts.Cast<FakeTextPart>().ToList();
        parts1.Should().HaveCount(1);
        parts1[0].Text.Should().Be("Static: ");
    }

    /// <summary>
    ///     Verifies that a placeholder spanning a paragraph boundary (opening braces in paragraph 0,
    ///     closing braces in paragraph 1) distributes the rendered value proportionally across the
    ///     original source TextParts. The paragraph boundary is preserved: paragraph 0 receives
    ///     "Hello " plus the portion of "World" overlapping the boundary (ratio 0.25→"W" and 0.75→"orld"),
    ///     while paragraph 1 receives only the trailing "!" from the original second part.
    /// </summary>
    [Fact]
    public void Compose_PlaceholderSplitAcrossParagraphBoundary_RatioPreservedInOutputParts()
    {
        // Para 0: "Hello {{" (8 chars), Para 1: "name}}!" (7 chars)
        // {{name}} tag spans positions 6-14: part0 overlaps [6,8)→2 chars (ratio 0.25),
        //                                    part1 overlaps [8,14)→6 chars (ratio 0.75)
        // "World"(5): round(0.25×5)=1→"W", 5-1=4→"orld"
        // <pb_0/> injected between pos 14 and pos 14 (before "!" segment) → "!" goes to para 1
        // Para 0: 3 TextParts ["Hello ", "W", "orld"]; Para 1: 1 TextPart ["!"]
        var shape = ShapeWithParagraphs(
            ["Hello {{"],
            ["name}}!"]);

        _composer.Compose(shape, Values(("name", "World")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        shape.ParagraphsCount.Should().Be(2);

        var parts0 = paragraphs[0].TextParts.Cast<FakeTextPart>().ToList();
        parts0.Should().HaveCount(3);
        parts0[0].Text.Should().Be("Hello ");
        parts0[1].Text.Should().Be("W");
        parts0[2].Text.Should().Be("orld");

        var parts1 = paragraphs[1].TextParts.Cast<FakeTextPart>().ToList();
        parts1.Should().HaveCount(1);
        parts1[0].Text.Should().Be("!");
    }

    #endregion

    #region Section expansion — edge cases

    /// <summary>
    ///     Verifies that <see cref="TextComposer.Compose" /> produces no output paragraphs when
    ///     the resolved value for a section key is an empty string (Mustache falsy), causing the
    ///     entire section block to be suppressed regardless of how many paragraphs it spans.
    /// </summary>
    [Fact]
    public void Compose_SectionWithEmptyStringValue_ProducesNoParagraphs()
    {
        var shape = ShapeWithParagraphs(
            ["{{#items}}"],
            ["item: {{.}}"],
            ["{{/items}}"]);

        _composer.Compose(shape, Values(("items", "")));

        shape.ParagraphsCount.Should().Be(0);
    }

    /// <summary>
    ///     Verifies that <see cref="TextComposer.Compose" /> produces exactly one output paragraph
    ///     when the section value is a JSON array containing a single element, iterating once.
    /// </summary>
    [Fact]
    public void Compose_SectionWithSingleItemJsonArray_ProducesOneParagraph()
    {
        var shape = ShapeWithParagraphs(
            ["{{#items}}"],
            ["{{.}}"],
            ["{{/items}}"]);

        // JSON array with one element — SmartParse parses to List<object>{"Alice"}
        _composer.Compose(shape, Values(("items", "[\"Alice\"]")));

        shape.ParagraphsCount.Should().Be(1);
        shape.AllText.Should().Be("Alice");
    }

    /// <summary>
    ///     Verifies that a section whose open tag, body, and close tag are all within the same
    ///     paragraph produces a single output paragraph containing the concatenation of all expanded
    ///     items — no paragraph boundary is created between items when the section is inline.
    /// </summary>
    [Fact]
    public void Compose_InlineSectionSameParagraph_AllItemsConcatenatedInOneParagraph()
    {
        var shape = ShapeWithParagraphs(["{{#items}}{{.}}{{/items}}"]);

        _composer.Compose(shape, Values(("items", "a,b,c")));

        shape.ParagraphsCount.Should().Be(1);
        shape.AllText.Should().Be("abc");
    }

    /// <summary>
    ///     Verifies that nested sections work correctly when the outer section spans multiple paragraphs
    ///     and the inner section is inline within the body paragraph. Each outer iteration produces one
    ///     output paragraph whose text is the concatenation of all inner items.
    /// </summary>
    [Fact]
    public void Compose_NestedSectionsOuterSpanningParagraphs_EachOuterItemYieldsOneParagraph()
    {
        // Para 0: outer open; Para 1: inline inner section; Para 2: outer close
        // outer=["x","y"] → 2 outer iterations; inner=["a","b"] → 2 inner items each
        //  body para renders inner items concatenated → "ab"; 2 outer → 2 paragraphs "ab"
        var shape = ShapeWithParagraphs(
            ["{{#outer}}"],
            ["{{#inner}}{{.}}{{/inner}}"],
            ["{{/outer}}"]);

        _composer.Compose(shape, Values(("outer", "x,y"), ("inner", "a,b")));

        var paragraphs = shape.Paragraph.Cast<FakeParagraph>().ToList();
        shape.ParagraphsCount.Should().Be(2);
        paragraphs[0].AllText().Should().Be("ab");
        paragraphs[1].AllText().Should().Be("ab");
    }

    /// <summary>
    ///     Verifies that <see cref="TextComposer.Compose" /> does not affect non-paragraph properties
    ///     of the shape. The <see cref="FakeShape.ImageData" /> byte array set before composition must
    ///     be identical after composition, confirming that <c>ClearParagraph</c> only clears text
    ///     content and leaves other shape state intact.
    /// </summary>
    [Fact]
    public void Compose_ShapeWithImageData_ImageDataPreservedAfterCompose()
    {
        var shape = ShapeWithParagraphs(["{{name}}"]);
        shape.ImageData = [1, 2, 3];

        _composer.Compose(shape, Values(("name", "World")));

        shape.ImageData.Should().Equal(1, 2, 3);
    }

    #endregion
}

#region Fake stubs

/// <summary>
///     A hand-coded test stub for <see cref="ITextPart" /> that stores mutable text state.
/// </summary>
internal sealed class FakeTextPart(string text) : ITextPart
{
    public string Text { get; set; } = text;
}

/// <summary>
///     A hand-coded test stub for <see cref="IParagraph" /> backed by a mutable list of <see cref="FakeTextPart" />.
/// </summary>
internal sealed class FakeParagraph : IParagraph
{
    private readonly List<FakeTextPart> _parts = [];

    /// <inheritdoc />
    public IEnumerable<ITextPart> TextParts => _parts;

    /// <inheritdoc />
    public int TextPartsCount => _parts.Count;

    /// <inheritdoc />
    public ITextPart AddTextPart(ITextPart textPart)
    {
        var newPart = new FakeTextPart(textPart.Text);
        _parts.Add(newPart);
        return newPart;
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        _parts.RemoveAt(index);
    }

    /// <summary>Returns the concatenated text of all text parts.</summary>
    public string AllText()
    {
        return string.Concat(_parts.Select(p => p.Text));
    }

    /// <summary>Appends a text part with the given text directly.</summary>
    public void AddText(string text)
    {
        _parts.Add(new FakeTextPart(text));
    }
}

/// <summary>
///     A hand-coded test stub for <see cref="IShape" /> backed by a mutable list of <see cref="FakeParagraph" />.
/// </summary>
internal sealed class FakeShape : IShape
{
    private readonly List<FakeParagraph> _paragraphs = [];

    /// <summary>Returns the concatenated text of all paragraphs and text parts.</summary>
    public string AllText => string.Concat(_paragraphs.Select(p => p.AllText()));

    /// <inheritdoc />
    public string Name => "FakeShape";

    /// <inheritdoc />
    public string DisplayText => string.Concat(_paragraphs.SelectMany(p => p.TextParts).Select(t => t.Text));

    /// <inheritdoc />
    public RectangleF Bounds => RectangleF.Empty;

    public byte[]? ImageData { get; set; }

    /// <inheritdoc />
    public IEnumerable<IParagraph> Paragraph => _paragraphs;

    /// <inheritdoc />
    public int ParagraphsCount => _paragraphs.Count;

    /// <inheritdoc />
    public IParagraph AddParagraph()
    {
        var para = new FakeParagraph();
        _paragraphs.Add(para);
        return para;
    }

    /// <inheritdoc />
    public void ClearParagraph()
    {
        _paragraphs.Clear();
    }

    /// <summary>Appends a new paragraph with the given text parts.</summary>
    public void AddParagraphWith(params string[] texts)
    {
        var para = new FakeParagraph();
        foreach (var text in texts)
            para.AddText(text);
        _paragraphs.Add(para);
    }
}

#endregion