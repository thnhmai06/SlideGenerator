/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GenerateJobStep.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Cloud;
using SlideGenerator.Document.Slide;
using SlideGenerator.Document.Template;
using SlideGenerator.Document.Workbook;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Generator.Models.Enum;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Image.Loading;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Config;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Steps;

/// <summary>
///     Processes a single <see cref="JobSpecification" />: resolves the mapped worksheet rows, downloads
///     and edits images, and writes each data row as an appended slide in the output presentation.
///     Idempotent: resume is handled by counting existing slides in the output to determine the start row.
/// </summary>
public sealed partial class GenerateJobStep(
    IWorkbookProvider workbookProvider,
    IPresentationProvider presentationProvider,
    ITextComposer textComposer,
    ISmartCropper smartCropper,
    IImageLoader imageLoader,
    ICloudClient cloudClient,
    IHttpClientFactory httpClientFactory,
    ISettingProvider settingProvider,
    IEventBus eventBus) : StepBodyAsync
{
    /// <summary>The job to process.</summary>
    private JobSpecification Specification { get; set; } = null!;

    private ILogger _logger = null!;
    private IReadOnlyDictionary<string, ContentInfo?> _inspectedUrls = null!;
    private StepProgress _progress = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (JobContext)context.Workflow.Data;
        var ct = context.CancellationToken;
        using var requestScope = LogContext.PushProperty("RequestId", data.Persist.RequestId);
        using var jobScope = LogContext.PushProperty("JobId", context.Workflow.Id);
        Specification = data.Persist.Specification;
        _logger = data.Transient.LoggerFactory!.CreateLogger(data.Persist.Request.Name);
        _inspectedUrls = data.Persist.InspectedUrls;
        _progress = StepProgress.From(eventBus, data.Persist.RequestId, context.Workflow.Id);
        var recipe = data.Persist.Recipe;

        // Resolve nodes from recipe graph
        var workbookNode = (WorkbookNode)recipe.Nodes[Specification.Source.WorkbookNodeId];
        var worksheetNode = (WorksheetNode)recipe.Nodes[Specification.Source.WorksheetNodeId];
        var mapNode = (MapNode)recipe.Nodes[Specification.MapNodeId];
        var presentationNode = (PresentationNode)recipe.Nodes[Specification.Template.PresentationNodeId];
        var slideNode = (SlideNode)recipe.Nodes[Specification.Template.SlideNodeId];

        var workbookPath = workbookNode.Workbook.BookPath;
        var sheetName = worksheetNode.Worksheet.SheetName;
        var presentationIdentifier = presentationNode.Presentation;
        var slideIdentifier = slideNode.Slide;

        _logger.LogInformation("{Status}", $"Processing job: {Specification.OutputPath}");

        if (!File.Exists(workbookPath) || !File.Exists(presentationIdentifier.PresentationPath))
        {
            _logger.LogError("Source file(s) missing — workbook: '{Book}', template: '{Ppt}'",
                workbookPath, presentationIdentifier.PresentationPath);
            return ExecutionResult.Next();
        }

        var output = File.Exists(Specification.OutputPath)
            ? await OpenOutputAsync(
                    new PresentationIdentifier(Specification.OutputPath, presentationIdentifier.PresentationPassword), ct)
                .ConfigureAwait(false)
            : await CreateOutputAsync(presentationIdentifier, ct).ConfigureAwait(false);

        var (workbook, worksheet) = await OpenWorksheetAsync(workbookNode.Workbook, sheetName, ct)
            .ConfigureAwait(false);
        if (worksheet == null)
        {
            _logger.LogError("Sheet '{Sheet}' not found in '{Book}'", sheetName, workbookPath);
            output.Save();
            output.Dispose();
            workbook.Dispose();
            return ExecutionResult.Next();
        }

        var headerToIndex = Utilities.BuildHeaderToIndexMap(worksheet);

        var templateSlide = await LoadTemplateSlideAsync(
                presentationIdentifier, slideIdentifier, data.Transient.TemplateSlides, ct)
            .ConfigureAwait(false);
        if (templateSlide == null)
        {
            _logger.LogError("Template slide not found in '{Ppt}' at index {Index}",
                presentationIdentifier.PresentationPath, slideNode.Slide.SlideIndex);
            output.Save();
            output.Dispose();
            workbook.Dispose();
            return ExecutionResult.Next();
        }

        // Indexes template shapes by identifier once per job — every row/instruction below looks up
        // against this same (unchanging) template slide, so an O(1) dictionary lookup replaces an O(N)
        // linear scan per shape reference.
        var templateShapesByIdentifier = templateSlide.Shapes.ToDictionary(s => s.Identifier);

        var doneCount = output.SlidesCount;
        var dataCount = worksheet.RowCount - 1;
        var dataRows = (worksheetNode.RowFilter?.GetIndices(dataCount) ?? Enumerable.Range(1, dataCount))
            .Skip(doneCount)
            .ToList();

        // Pre-seed every remaining row as Waiting so the frontend sees the full row set immediately
        // instead of it growing one row at a time.
        _progress.SeedRows(dataRows.Select(r => r + 1));

        // InspectUrlsStep already ran for this job earlier in the same JobWorkflow instance and recorded
        // results into data.Persist.InspectedUrls — a source missing from that dictionary is skipped
        // entirely below.
        foreach (var dataRow in dataRows)
        {
            ct.ThrowIfCancellationRequested();
            _progress.ReportRow(dataRow + 1, RowStatus.Processing);
            try
            {
                await GenerateRowSlideAsync(worksheet, headerToIndex, dataRow, mapNode, templateShapesByIdentifier,
                        templateSlide, output, data.Persist.Request.AllowLocalPaths, ct)
                    .ConfigureAwait(false);
                _progress.ReportRow(dataRow + 1, RowStatus.Done);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Row {RowIndex} failed", dataRow + 1);
                _progress.ReportRow(dataRow + 1, RowStatus.Error, note: ex.Message);
                throw;
            }
        }

        data.Transient.TemplateSlides.TryRemove(Specification.OutputPath, out _);
        workbook.Dispose();
        output.Dispose();
        _logger.LogInformation("Job complete: {OutputPath}", Specification.OutputPath);

        return ExecutionResult.Next();
    }
}