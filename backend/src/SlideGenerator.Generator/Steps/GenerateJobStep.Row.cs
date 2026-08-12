/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GenerateJobStep.Row.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Document.Abstractions;
using SlideGenerator.Document.Abstractions.Sheet;
using SlideGenerator.Document.Abstractions.Slide;
using SlideGenerator.Document.Models.Sheet;
using SlideGenerator.Document.Models.Slide;
using SlideGenerator.Generator.Models.Enum;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Models.Components;

namespace SlideGenerator.Generator.Steps;

public sealed partial class GenerateJobStep
{
    /// <summary>
    ///     Generates one data slide for <paramref name="dataRow" />: builds text/image values from the row,
    ///     appends a fresh slide cloned from <paramref name="templateSlide" />, replaces its placeholders and
    ///     target shapes, then saves. Orchestrates the row-level sub-steps below; each reports its own status.
    /// </summary>
    private async Task GenerateRowSlideAsync(
        IReadOnlyWorksheet worksheet,
        IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex,
        int dataRow,
        MapNode mapNode,
        IReadOnlyDictionary<ShapeIdentifier, IShape> templateShapesByIdentifier,
        ISlide templateSlide,
        IPresentation output,
        bool allowLocalPaths,
        CancellationToken ct)
    {
        using var rowScope = LogContext.PushProperty("RowIndex", dataRow + 1);
        _logger.LogInformation("{Status}", $"Processing row {dataRow + 1}");

        var row = worksheet.GetRow(dataRow + 1); // row 1 = header => data row N = absolute row N+1
        var rowValues = headerToIndex.ToDictionary(kv => kv.Key, kv => row[kv.Value]);

        var textValues = BuildRowTextValues(rowValues, mapNode.TextInstructions);
        var imageRequests = BuildRowImageRequests(rowValues, mapNode.ImageInstructions, templateShapesByIdentifier);
        var imageResults = await CollectRowImagesAsync(imageRequests, allowLocalPaths, dataRow + 1, ct).ConfigureAwait(false);

        var newSlide = AppendDataSlide(output, templateSlide);
        var newShapesByIdentifier = ReplaceShapeTexts(newSlide, textValues);
        ReplaceShapeImages(newShapesByIdentifier, imageResults);

        _progress.ReportRow(dataRow + 1, RowStatus.Processing, RowStage.SavingOutput);
        await SaveOutputAsync(output, ct).ConfigureAwait(false);
    }

    /// <summary>Appends a fresh clone of <paramref name="templateSlide" /> to <paramref name="output" /> and returns it.</summary>
    private static ISlide AppendDataSlide(IPresentation output, ISlide templateSlide)
    {
        output.AddSlide(templateSlide.Clone());
        return output.Slides.ElementAt(output.SlidesCount - 1);
    }

    /// <summary>
    ///     Composes <paramref name="textValues" /> into every shape of <paramref name="slide" /> via
    ///     <see cref="ITextComposer" />, returning the slide's shapes indexed by identifier for the
    ///     subsequent image-replacement step.
    /// </summary>
    private Dictionary<ShapeIdentifier, IShape> ReplaceShapeTexts(
        ISlide slide, IReadOnlyDictionary<string, string> textValues)
    {
        var shapesByIdentifier = new Dictionary<ShapeIdentifier, IShape>();
        foreach (var shape in slide.Shapes)
        {
            textComposer.Compose(shape, textValues);
            shapesByIdentifier[shape.Identifier] = shape;
        }

        return shapesByIdentifier;
    }

    /// <summary>Assigns each resolved image's bytes to its target shape, skipping shapes with no image data.</summary>
    private static void ReplaceShapeImages(
        IReadOnlyDictionary<ShapeIdentifier, IShape> shapesByIdentifier,
        IReadOnlyDictionary<(ImageInstruction, ShapeIdentifier), byte[]?> imageResults)
    {
        foreach (var ((_, shapeIdentifier), imageData) in imageResults)
        {
            if (imageData == null) continue;
            if (shapesByIdentifier.TryGetValue(shapeIdentifier, out var shape))
                shape.ImageData = imageData;
        }
    }
}