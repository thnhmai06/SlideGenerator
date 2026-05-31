/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: MustacheEngineTests.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Document.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Document.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="MustacheEngine" />, verifying placeholder scanning and
///     template rendering behavior including smart data-type parsing.
/// </summary>
public sealed class MustacheEngineTests
{
    private readonly MustacheEngine _engine = new(NullLogger<MustacheEngine>.Instance);

    #region ScanPlaceholders

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns an empty set
    ///     for null, empty, or whitespace-only input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void ScanPlaceholders_NullOrWhiteSpace_ReturnsEmptySet(string text)
    {
        var result = _engine.ScanPlaceholders(text);

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> extracts the key from
    ///     a simple double-brace variable tag.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_SimpleVariableTag_ReturnsKey()
    {
        var result = _engine.ScanPlaceholders("Hello {{name}}!");

        result.Should().ContainSingle().Which.Should().Be("name");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> extracts the key from
    ///     a triple-brace (unescaped) tag.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_TripleBraceTag_ReturnsKey()
    {
        var result = _engine.ScanPlaceholders("{{{rawHtml}}}");

        result.Should().ContainSingle().Which.Should().Be("rawHtml");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns the root key from
    ///     a section tag, stripping the leading <c>#</c> or <c>^</c> prefix, and includes keys
    ///     found within the section.
    /// </summary>
    [Theory]
    [InlineData("{{#items}}x{{/items}}", new[] { "items" })]
    [InlineData("{{^empty}}y{{/empty}}", new[] { "empty" })]
    [InlineData("{{#object}}{{{shouldIncluded}}} + {{andThisToo}}{{/object}}",
        new[] { "object", "shouldIncluded", "andThisToo" })]
    [InlineData("{{^list}}- Hello {{.}}!{{/list}}", new[] { "list" })]
    public void ScanPlaceholders_SectionTag_ReturnsAllContainedKeys(string text, string[] expectedKeys)
    {
        var result = _engine.ScanPlaceholders(text);

        result.Should().BeEquivalentTo(expectedKeys);
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> ignores comment tags
    ///     (<c>{{!…}}</c>) and closing section tags (<c>{{/…}}</c>).
    /// </summary>
    [Fact]
    public void ScanPlaceholders_CommentAndClosingTags_AreIgnored()
    {
        var result = _engine.ScanPlaceholders("{{! this is a comment }} {{/section}}");

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns only the root key
    ///     when dotted property access (<c>{{obj.prop}}</c>) is present.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_DottedProperty_ReturnsRootKeyOnly()
    {
        var result = _engine.ScanPlaceholders("{{person.name}}");

        result.Should().ContainSingle().Which.Should().Be("person");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns all distinct keys
    ///     when multiple different placeholders appear.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_MultipleUniqueKeys_ReturnsAll()
    {
        var result = _engine.ScanPlaceholders("{{first}} and {{second}} and {{third}}");

        result.Should().BeEquivalentTo("first", "second", "third");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns multiple distinct
    ///     root keys when several different section tags are present.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_MultipleSectionTags_ReturnsAllRootKeys()
    {
        const string text =
            "{{#first}}a{{/first}} {{#second}}{{shouldHave}}{{/second}} {{^third}}{{{! hmmm}}} {{.}}{{/third}}";

        var result = _engine.ScanPlaceholders(text);

        result.Should().BeEquivalentTo("first", "second", "shouldHave", "third");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns a single entry
    ///     when the same key appears multiple times, regardless of case.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_DuplicateKeysIgnoringCase_ReturnedOnce()
    {
        var result = _engine.ScanPlaceholders("{{Name}} {{name}} {{NAME}}");

        result.Should().ContainSingle();
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> correctly extracts the key
    ///     from a tag that contains surrounding whitespace (<c>{{ name }}</c>), trimming it to the
    ///     bare key name.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_WhitespaceInsideTag_KeyExtractedWithoutWhitespace()
    {
        var result = _engine.ScanPlaceholders("{{ name }}");

        result.Should().ContainSingle().Which.Should().Be("name");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.ScanPlaceholders" /> returns both keys when two
    ///     placeholders appear immediately adjacent with no separator between them.
    /// </summary>
    [Fact]
    public void ScanPlaceholders_AdjacentPlaceholders_BothKeysReturned()
    {
        var result = _engine.ScanPlaceholders("{{a}}{{b}}");

        result.Should().BeEquivalentTo("a", "b");
    }

    #endregion

    #region Render

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> returns the template unchanged when
    ///     the resolved-value dictionary is empty.
    /// </summary>
    [Fact]
    public void Render_EmptyResolvedValues_ReturnsTemplateUnchanged()
    {
        const string template = "Hello {{name}}!";

        var result = _engine.Render(template, new Dictionary<string, string>());

        result.Should().Be(template);
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> returns the template unchanged when
    ///     the template text is empty or whitespace.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_EmptyOrWhitespaceTemplate_ReturnsTemplateUnchanged(string template)
    {
        var result = _engine.Render(template, new Dictionary<string, string> { ["key"] = "value" });

        result.Should().Be(template);
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> replaces a single double-brace
    ///     placeholder with its corresponding value.
    /// </summary>
    [Fact]
    public void Render_SingleVariablePlaceholder_ReplacedWithValue()
    {
        var result = _engine.Render("Hello {{name}}!", new Dictionary<string, string> { ["name"] = "World" });

        result.Should().Be("Hello World!");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> replaces a triple-brace placeholder
    ///     with its raw value (no HTML encoding applied).
    /// </summary>
    [Fact]
    public void Render_TripleBracePlaceholder_ReplacedWithRawValue()
    {
        var result = _engine.Render("{{{raw}}}", new Dictionary<string, string> { ["raw"] = "<b>bold</b>" });

        result.Should().Be("<b>bold</b>");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> replaces multiple distinct placeholders
    ///     in a single pass.
    /// </summary>
    [Fact]
    public void Render_MultiplePlaceholders_AllReplaced()
    {
        var values = new Dictionary<string, string> { ["first"] = "A", ["second"] = "B" };

        var result = _engine.Render("{{first}}-{{second}}", values);

        result.Should().Be("A-B");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> renders a placeholder absent from the
    ///     resolved-value dictionary as an empty string (Stubble's default behavior).
    /// </summary>
    [Fact]
    public void Render_PlaceholderAbsentFromValues_RenderedAsEmpty()
    {
        var result = _engine.Render("Hello {{missing}}!", new Dictionary<string, string> { ["other"] = "x" });

        result.Should().Be("Hello !");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> expands a Mustache section when the
    ///     value is a comma-separated list used as a simple array (complex-key context).
    /// </summary>
    [Fact]
    public void Render_SectionWithCommaList_ExpandsItems()
    {
        const string template = "{{#items}}[{{.}}] {{/items}}";
        var values = new Dictionary<string, string> { ["items"] = "a,b,c" };

        var result = _engine.Render(template, values);

        result.Should().Be("[a] [b] [c] ");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> outputs an empty string for a placeholder
    ///     whose resolved value is an empty string.
    /// </summary>
    [Fact]
    public void Render_EmptyValue_PlaceholderReplacedWithEmptyString()
    {
        var result = _engine.Render("Hello {{name}}!", new Dictionary<string, string> { ["name"] = "" });

        result.Should().Be("Hello !");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> does not double-interpret a resolved value
    ///     that contains Mustache syntax — the braces are emitted literally in the output.
    /// </summary>
    [Fact]
    public void Render_ValueContainsMustacheCharacters_OutputIsLiteralNotDoubleRendered()
    {
        var result = _engine.Render("{{key}}", new Dictionary<string, string> { ["key"] = "{{nested}}" });

        result.Should().Be("{{nested}}");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> correctly resolves a tag that contains
    ///     surrounding whitespace (<c>{{ name }}</c>), treating it identically to <c>{{name}}</c>.
    /// </summary>
    [Fact]
    public void Render_WhitespaceInsideTag_ReplacedWithValue()
    {
        var result = _engine.Render("{{ name }}", new Dictionary<string, string> { ["name"] = "Alice" });

        result.Should().Be("Alice");
    }

    /// <summary>
    ///     Verifies that <see cref="MustacheEngine.Render" /> replaces two adjacent placeholders
    ///     that appear with no separator between them, each independently resolved.
    /// </summary>
    [Fact]
    public void Render_AdjacentPlaceholders_BothReplaced()
    {
        var result = _engine.Render("{{a}}{{b}}", new Dictionary<string, string> { ["a"] = "Hello", ["b"] = "World" });

        result.Should().Be("HelloWorld");
    }

    /// <summary>
    ///     Verifies that an inner placeholder inside a truthy section is resolved from the root
    ///     resolved-value dictionary when its key is present. The section value acts as a truthy
    ///     guard; the key independently looks up the inner placeholder.
    /// </summary>
    [Fact]
    public void Render_SectionWithInnerPlaceholderInValues_PlaceholderResolved()
    {
        // "show" is truthy (non-empty string) → section renders once
        // {{name}} falls back to root dict and resolves to "Alice"
        const string template = "{{#show}}Hello {{name}}!{{/show}}";
        var values = new Dictionary<string, string> { ["show"] = "true", ["name"] = "Alice" };

        var result = _engine.Render(template, values);

        result.Should().Be("Hello Alice!");
    }

    /// <summary>
    ///     Verifies that an inner placeholder inside a truthy section renders as an empty string
    ///     when its key is absent from the resolved-value dictionary — consistent with top-level
    ///     absent-placeholder behavior.
    /// </summary>
    [Fact]
    public void Render_SectionWithInnerPlaceholderAbsentFromValues_InnerRenderedAsEmpty()
    {
        const string template = "{{#show}}Hello {{missing}}!{{/show}}";
        var values = new Dictionary<string, string> { ["show"] = "true" };

        var result = _engine.Render(template, values);

        result.Should().Be("Hello !");
    }

    /// <summary>
    ///     Verifies that a section value parsed from a JSON object string grants access to the
    ///     object's properties as inner placeholders within the section body.
    ///     SmartParse converts <c>{"name":"Alice","age":"30"}</c> to a <c>Dictionary&lt;string, object&gt;</c>
    ///     the section renders once with that dictionary as the inner context.
    /// </summary>
    [Fact]
    public void Render_SectionFromJsonObject_InnerPropertiesResolved()
    {
        const string template = "{{#person}}{{name}} is {{age}}{{/person}}";
        var values = new Dictionary<string, string>
        {
            ["person"] = """{"name":"Alice","age":"30"}"""
        };

        var result = _engine.Render(template, values);

        result.Should().Be("Alice is 30");
    }

    /// <summary>
    ///     Verifies that a section value parsed from a JSON array of objects iterates over each
    ///     element, resolving inner placeholders from each element's properties independently.
    ///     SmartParse converts the JSON array string to a <c>List&lt;object&gt;</c> of dictionaries.
    /// </summary>
    [Fact]
    public void Render_SectionFromJsonArrayOfObjects_IteratesAndResolvesProperties()
    {
        const string template = "{{#people}}{{name}}, {{/people}}";
        var values = new Dictionary<string, string>
        {
            ["people"] = """[{"name":"Alice"},{"name":"Bob"}]"""
        };

        var result = _engine.Render(template, values);

        result.Should().Be("Alice, Bob, ");
    }

    /// <summary>
    ///     Verifies that a section value in Excel array format (<c>{v1, v2}</c>) is parsed into a
    ///     list and iterated, with each element accessible via <c>{{.}}</c> inside the section.
    /// </summary>
    [Fact]
    public void Render_SectionFromExcelArray_IteratesItems()
    {
        const string template = "{{#items}}[{{.}}]{{/items}}";
        var values = new Dictionary<string, string> { ["items"] = "{Alice, Bob}" };

        var result = _engine.Render(template, values);

        result.Should().Be("[Alice][Bob]");
    }

    /// <summary>
    ///     Verifies that an inverted section (<c>{{^key}}</c>) renders its body when the key's
    ///     resolved value is an empty string (Mustache falsy).
    /// </summary>
    [Fact]
    public void Render_InvertedSection_RendersWhenValueFalsy()
    {
        const string template = "{{^empty}}shown{{/empty}}";
        var values = new Dictionary<string, string> { ["empty"] = "" };

        var result = _engine.Render(template, values);

        result.Should().Be("shown");
    }

    /// <summary>
    ///     Verifies that an inverted section (<c>{{^key}}</c>) suppresses its body when the key's
    ///     resolved value is a non-empty truthy string.
    /// </summary>
    [Fact]
    public void Render_InvertedSection_SuppressedWhenValueTruthy()
    {
        const string template = "{{^present}}hidden{{/present}}";
        var values = new Dictionary<string, string> { ["present"] = "x" };

        var result = _engine.Render(template, values);

        result.Should().BeEmpty();
    }

    #endregion
}