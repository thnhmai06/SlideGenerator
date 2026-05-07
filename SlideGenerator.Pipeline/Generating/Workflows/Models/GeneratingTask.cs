/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
 * File: GeneratingTask.cs
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

using System.Collections.Concurrent;
using Serilog;
using SlideGenerator.Document.Sheet.Entities;
using SlideGenerator.Document.Sheet.Models;
using SlideGenerator.Document.Slide.Entities;
using SlideGenerator.Document.Slide.Models;
using SlideGenerator.Pipeline.Generating.Models;

namespace SlideGenerator.Pipeline.Generating.Workflows.Models;

/// <summary>
///     Represents the state and data managed by the slide generation workflow.
/// </summary>
public sealed class GeneratingTask : IDisposable
{
    private ILogger? _logger;

    /// <summary>
    ///     The initial generation request.
    /// </summary>
    public GeneratingRequest Request { get; init; } = null!;

    /// <summary>
    ///     Gets the workflow-scoped logger enriched with <c>TaskId</c>.
    ///     Must be initialized via <see cref="TryInitLogger" /> before first use.
    /// </summary>
    public ILogger Logger =>
        _logger ?? throw new InvalidOperationException("Workflow logger has not been initialized.");

    /// <summary>
    ///     The collection of validated worksheets and their target output configurations.
    ///     Populated during Phase A.
    /// </summary>
    public ConcurrentDictionary<SheetIdentifier, SheetTask> ValidWorksheets { get; } = new();

    /// <summary>
    ///     The collection of slide generation tasks containing data replacements.
    /// </summary>
    public ConcurrentBag<SlideTask> SlideTasks { get; } = [];

    /// <summary>
    ///     The collection of image processing tasks combining download and edit requirements.
    /// </summary>
    public ConcurrentBag<ImageTask> ImageTasks { get; } = [];

    // Long-lived handles

    /// <summary>
    ///     Gets the collection of workbook handles used for reading data.
    ///     Key is the book identifier.
    /// </summary>
    public ConcurrentDictionary<BookIdentifier, SfWorkbook> WorkbookHandles { get; } = new();

    /// <summary>
    ///     Gets the collection of presentation handles used as templates.
    ///     Key is the presentation identifier.
    /// </summary>
    public ConcurrentDictionary<PresentationIdentifier, SfPresentation> TemplateHandles { get; } = new();

    /// <summary>
    ///     Gets the collection of presentation handles used for output generation.
    ///     Key is the presentation identifier.
    /// </summary>
    public ConcurrentDictionary<PresentationIdentifier, SfPresentation> OutputHandles { get; } = new();

    /// <summary>
    ///     Disposes of all open workbook and presentation handles.
    /// </summary>
    public void Dispose()
    {
        foreach (var handle in WorkbookHandles.Values)
            try
            {
                handle.Dispose();
            }
            catch
            {
                /* ignore */
            }

        WorkbookHandles.Clear();

        foreach (var handle in TemplateHandles.Values)
            try
            {
                handle.Dispose();
            }
            catch
            {
                /* ignore */
            }

        TemplateHandles.Clear();

        foreach (var handle in OutputHandles.Values)
            try
            {
                handle.Dispose();
            }
            catch
            {
                /* ignore */
            }

        OutputHandles.Clear();
    }

    /// <summary>
    ///     Initializes the workflow-scoped logger. Idempotent — only the first call has effect.
    /// </summary>
    public void TryInitLogger(ILogger baseLogger, string workflowId)
    {
        Interlocked.CompareExchange(ref _logger, 
            baseLogger
                .ForContext("TaskId", workflowId)
                .ForContext("RecipeName", Request.Recipe.Name), 
            null);
    }
}