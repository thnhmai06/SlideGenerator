/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: TextComposerIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Document.Services;
using SlideGenerator.Document.Adapters.Slide;
using SlideGenerator.Document.Services;
using SlideGenerator.Document.Tests.Integration.Models;
using Syncfusion.Licensing;
using Syncfusion.Presentation;
using Xunit;
using SfIPresentation = Syncfusion.Presentation.IPresentation;
using SfIShape = Syncfusion.Presentation.IShape;
using SfPresentation = Syncfusion.Presentation.Presentation;

namespace SlideGenerator.Document.Tests.Integration;

/// <summary>
///     Data-driven integration tests for <see cref="TextComposer" /> that iterate every row in
///     <c>Data.csv</c>, compose all Mustache text shapes on both slides of <c>Template.pptx</c>,
///     assert the composed content, and save one <c>.pptx</c> output file per row for visual inspection.
/// </summary>
/// <remarks>
///     Requires <c>SYNCFUSION_LICENSE_KEY</c> or a populated <c>.env</c> file.
///     Filter out with: <code>dotnet test --filter "Category!=Integration"</code>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class TextComposerIntegrationTests : IDisposable
{
    private static readonly string TemplatePath = Path.Combine("fixtures", "data", "Template.pptx");
    private static readonly string DataCsvPath  = Path.Combine("fixtures", "data", "Data.csv");
    private static readonly string OutputSubDir = Path.Combine("fixtures", "output");

    private readonly TextComposer _composer = new(new MustacheEngine(NullLogger<MustacheEngine>.Instance));
    private readonly SfIPresentation _presentation;

    /// <summary>Registers Syncfusion license and opens a fresh <c>Template.pptx</c> per test case.</summary>
    public TextComposerIntegrationTests()
    {
        SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicense.Key);
        _presentation = SfPresentation.Open(TemplatePath);
    }

    /// <inheritdoc />
    public void Dispose() => _presentation.Dispose();

    #region Data loading

    /// <summary>Loads all rows from <c>Data.csv</c> as <see cref="DataRow" /> instances.</summary>
    public static TheoryData<DataRow> LoadCases()
    {
        var path   = Path.Combine(AppContext.BaseDirectory, DataCsvPath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions     = TrimOptions.Trim,
        };

        using var reader = new StreamReader(path);
        using var csv    = new CsvReader(reader, config);

        var data = new TheoryData<DataRow>();
        foreach (var row in csv.GetRecords<DataRow>())
            data.Add(row);
        return data;
    }

    #endregion

    #region Helpers

    private static IEnumerable<SfIShape> AllShapes(System.Collections.IEnumerable shapes)
    {
        foreach (SfIShape shape in shapes)
        {
            yield return shape;
            if (shape is not IGroupShape group) continue;
            foreach (var child in AllShapes(group.Shapes))
                yield return child;
        }
    }

    private static List<string> ParagraphTexts(SfIShape shape) =>
        shape.TextBody.Paragraphs
            .Select(p => p.Text)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

    private static IEnumerable<string> ParseDataList(string dataList)
    {
        var inner = dataList.Trim().TrimStart('{').TrimEnd('}');
        return inner.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0);
    }

    private void SaveOutput(string fileName)
    {
        var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, OutputSubDir));
        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, $"{fileName}.pptx");
        _presentation.Save(filePath);
        TestContext.Current.SendDiagnosticMessage($"Output saved: {filePath}");
    }

    #endregion

    #region Assertions

    /// <summary>
    ///     Verifies that a single slide contains correctly composed content for the given <paramref name="row" />.
    /// </summary>
    private static void AssertSlideComposition(ISlide slide, DataRow row)
    {
        var allShapes = AllShapes(slide.Shapes).ToList();

        // No unreplaced Mustache tokens
        foreach (var shape in allShapes)
            shape.TextBody?.Text.Should().NotContain("{{",
                because: "all Mustache tokens must be resolved");

        // Locate the two composed shapes by their resulting text content
        var infoShape = allShapes.FirstOrDefault(s => s.TextBody?.Text.Contains("Provider: ") == true);
        var textShape = allShapes.FirstOrDefault(s => s.TextBody?.Text.Contains("Source: ") == true);

        infoShape.Should().NotBeNull("Test Info data shape must be present on the slide");
        textShape.Should().NotBeNull("Text data shape must be present on the slide");

        // Test Info paragraphs
        var info = ParagraphTexts(infoShape);
        info.Should().HaveCountGreaterThanOrEqualTo(4);
        info[0].Should().Be($"Provider: {row.Provider}");
        info[1].Should().Be($"Type: {row.Type}");
        info[2].Should().Be($"ShouldDownload: {row.ShouldDownload}");
        info[3].Should().Be($"URL: {row.Url}");

        // Text shape paragraphs
        var text = ParagraphTexts(textShape);
        text.Should().HaveCountGreaterThanOrEqualTo(4);
        text[0].Should().Be($"Source: {row.Provider} – {row.Type}");
        text[1].Should().Be(row.Provider);

        if (row.ShouldDownload.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            text[2].Trim().Should().Be($"URL: {row.Url}");
        else
            text[2].Should().Be("N/A");

        var expectedTags = "Tags: " + string.Concat(ParseDataList(row.DataList).Select(item => $"[{item}]"));
        text[3].Should().Be(expectedTags);
    }

    #endregion

    #region Data-driven composition

    /// <summary>
    ///     For each row in <c>Data.csv</c>, composes all Mustache text shapes on both slides of
    ///     <c>Template.pptx</c>, asserts the composed content matches expected values, then saves
    ///     the result to <c>fixtures/output/{Provider}_{Type}.pptx</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(LoadCases))]
    public void Compose_AllTextShapes_BothSlides_SavesOutput(DataRow row)
    {
        var values = row.ToResolvedValues();

        foreach (var slide in _presentation.Slides)
        {
            foreach (var sfShape in AllShapes(slide.Shapes))
            {
                if (sfShape.TextBody?.Text?.Contains("{{") != true) continue;
                _composer.Compose(new SfShape(sfShape), values);
            }
        }

        foreach (var slide in _presentation.Slides)
            AssertSlideComposition(slide, row);

        SaveOutput(row.SafeFileName);
    }

    #endregion
}
