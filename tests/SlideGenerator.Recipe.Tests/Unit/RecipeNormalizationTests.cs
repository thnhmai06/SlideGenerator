/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipeNormalizationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Models;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Tests for <see cref="Recipe.Normalized" />, verifying that every collection a deserialized or
///     hand-crafted recipe may have left null is replaced by an empty one, recursively.
/// </summary>
public sealed class RecipeNormalizationTests
{
    private static readonly string WorkbookPath = Path.GetFullPath("data.xlsx");
    private static readonly string PresentationPath = Path.GetFullPath("template.pptx");

    /// <summary>Returns a mapping whose collections and nested collections are all populated.</summary>
    private static Mapping PopulatedMapping(string? fallbackImagePath = null)
    {
        return new Mapping(
            [
                new WorksheetSource(
                    new WorkbookIdentifier(WorkbookPath),
                    new WorksheetIdentifier("Sheet1"),
                    new HashSet<ColumnIdentifier> { new("Name") },
                    new AllRowFilter())
            ],
            new PresentationSource(new PresentationIdentifier(PresentationPath), new SlideIdentifier(1)),
            [new TextInstruction(new HashSet<string> { "{{Name}}" }, [new ColumnIdentifier("Name")])],
            [
                new ImageInstruction(
                    new HashSet<ShapeIdentifier> { new("Picture 1") },
                    [new ColumnIdentifier("Photo")],
                    new ImageEditInstruction([new AnchorOption()]),
                    fallbackImagePath)
            ]);
    }

    /// <summary>
    ///     A recipe whose collections are null (including nested instruction collections and a missing
    ///     image edit instruction) normalizes to a tree with empty collections, preserving the template
    ///     and leaving the null used-columns/row-filter meaning intact.
    /// </summary>
    [Fact]
    public void Normalized_AllCollectionsNull_ReplacesThemWithEmptyCollections()
    {
        var template = new PresentationSource(new PresentationIdentifier(PresentationPath), new SlideIdentifier(1));
        var source = new WorksheetSource(new WorkbookIdentifier(WorkbookPath), new WorksheetIdentifier("Sheet1"));
        var recipe = new Models.Recipe([
            new Mapping(null!, template, null!, null!),
            new Mapping(
                [source],
                template,
                [new TextInstruction(null!, null!)],
                [new ImageInstruction(null!, null!, null!)])
        ]);

        var normalized = recipe.Normalized();

        normalized.Mappings.Should().HaveCount(2);

        var nullLists = normalized.Mappings[0];
        nullLists.Sources.Should().NotBeNull().And.BeEmpty();
        nullLists.TextInstructions.Should().NotBeNull().And.BeEmpty();
        nullLists.ImageInstructions.Should().NotBeNull().And.BeEmpty();
        nullLists.Template.Should().Be(template);

        var nullMembers = normalized.Mappings[1];
        nullMembers.Sources.Should().ContainSingle().Which.UsedColumns.Should().BeNull();
        nullMembers.Sources.Single().RowFilter.Should().BeNull();
        nullMembers.TextInstructions.Single().Placeholders.Should().NotBeNull().And.BeEmpty();
        nullMembers.TextInstructions.Single().Columns.Should().NotBeNull().And.BeEmpty();
        nullMembers.ImageInstructions.Single().Shapes.Should().NotBeNull().And.BeEmpty();
        nullMembers.ImageInstructions.Single().Columns.Should().NotBeNull().And.BeEmpty();
        nullMembers.ImageInstructions.Single().ImageEditInstruction.Should().NotBeNull();
        nullMembers.ImageInstructions.Single().ImageEditInstruction.RoiOptions.Should().NotBeNull().And.BeEmpty();
        nullMembers.Template.Should().Be(template);
    }

    /// <summary>A null top-level mapping list normalizes to an empty mapping list.</summary>
    [Fact]
    public void Normalized_NullMappingList_ReturnsEmptyMappingList()
    {
        var normalized = new Models.Recipe(null!).Normalized();

        normalized.Mappings.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>
    ///     Already-populated members are kept as they are: the same template instance and equal
    ///     source/instruction values survive normalization.
    /// </summary>
    [Fact]
    public void Normalized_PopulatedRecipe_KeepsMemberValues()
    {
        var original = PopulatedMapping();
        var recipe = new Models.Recipe([original]);

        var normalized = recipe.Normalized().Mappings.Single();

        normalized.Template.Should().Be(original.Template);
        normalized.Sources.Should().Equal(original.Sources);
        normalized.TextInstructions.Should().Equal(original.TextInstructions);
        normalized.ImageInstructions.Should().Equal(original.ImageInstructions);
    }

    /// <summary>An image instruction's fallback path survives normalization unchanged.</summary>
    [Fact]
    public void Normalized_ImageFallbackPath_IsPreserved()
    {
        var fallbackPath = Path.GetFullPath("fallback.png");
        var recipe = new Models.Recipe([PopulatedMapping(fallbackPath)]);

        var normalized = recipe.Normalized();

        normalized.Mappings.Single().ImageInstructions.Single().FallbackImagePath.Should().Be(fallbackPath);
    }

    /// <summary>
    ///     The JSON route: null or missing collections in a payload deserialize to null and are then
    ///     normalized away, so consumers can enumerate them and <see cref="Recipe.GetReferencedFiles" />
    ///     does not throw.
    /// </summary>
    [Fact]
    public void Normalized_DeserializedPayloadWithNullCollections_ProducesEnumerablesafeTree()
    {
        var options = RecipePackageFormat.Data.Recipe.Format;
        var node = JsonSerializer.SerializeToNode(new Models.Recipe([PopulatedMapping()]), options)!;
        var mappingNode = (JsonObject)node["mappings"]![0]!;
        mappingNode["sources"] = null;
        mappingNode.Remove("textInstructions");
        var imageNode = (JsonObject)mappingNode["imageInstructions"]![0]!;
        imageNode["shapes"] = null;
        imageNode["columns"] = null;
        imageNode["imageEditInstruction"] = null;

        var deserialized = JsonSerializer.Deserialize<Models.Recipe>(node.ToJsonString(), options)!;
        var normalized = deserialized.Normalized();
        var mapping = normalized.Mappings.Single();

        mapping.Sources.Should().NotBeNull().And.BeEmpty();
        mapping.TextInstructions.Should().NotBeNull().And.BeEmpty();
        mapping.ImageInstructions.Should().NotBeNull().And.HaveCount(1);
        mapping.ImageInstructions.Single().Shapes.Should().NotBeNull().And.BeEmpty();
        mapping.ImageInstructions.Single().Columns.Should().NotBeNull().And.BeEmpty();
        mapping.ImageInstructions.Single().ImageEditInstruction.Should().NotBeNull();
        mapping.ImageInstructions.Single().ImageEditInstruction.RoiOptions.Should().NotBeNull().And.BeEmpty();

        var act = () => normalized.GetReferencedFiles();

        act.Should().NotThrow();
        act().Should().Contain(PresentationPath);
    }
}
