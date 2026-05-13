/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: GeneratingContext.cs
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
using Newtonsoft.Json;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Generating.Domain.Models.Contexts;

/// <summary>
///     Represents the state and data managed by the slide generation workflow.
///     Serialized by WorkflowCore (Newtonsoft.Json); file handles and the logger are excluded.
/// </summary>
public sealed class GeneratingContext : IDisposable
{
    /// <summary>
    ///     The initial generation request.
    /// </summary>
    public GeneratingRequest Request { get; init; } = null!;

    /// <summary>
    ///     Persisted path to the workflow log file, used to reattach the logger on resume.
    /// </summary>
    public string WorkflowLogPath { get; init; } = null!;

    /// <summary>
    ///     Persisted scope name used when creating the workflow logger.
    /// </summary>
    public string WorkflowScope { get; init; } = null!;

    /// <summary>
    ///     Gets or sets the workflow-scoped logger.
    ///     Not serialized — recreated by <c>GeneratingLoggerMiddleware</c> before each step.
    /// </summary>
    [JsonIgnore]
    public IAppLogger? Logger { get; set; }

    /// <summary>
    ///     The collection of validated worksheets and their target output configurations.
    ///     Populated during Phase A.
    /// </summary>
    public ConcurrentDictionary<SheetIdentifier, SheetContext> ValidWorksheets { get; } = new();

    /// <summary>
    ///     The collection of slide generation contexts containing data replacements.
    /// </summary>
    public ConcurrentBag<SlideContext> SlideContexts { get; } = [];

    /// <summary>
    ///     The collection of image processing contexts combining download and edit requirements.
    /// </summary>
    public ConcurrentBag<ImageContext> ImageContexts { get; } = [];

    // Long-lived handles — not serialized; reopened lazily on resume via Utilities extension methods.

    /// <summary>
    ///     Gets the collection of workbook handles used for reading data.
    /// </summary>
    [JsonIgnore]
    public ConcurrentDictionary<BookIdentifier, IReadOnlyWorkbook> WorkbookHandles { get; } = new();

    /// <summary>
    ///     Gets the collection of presentation handles used as templates.
    /// </summary>
    [JsonIgnore]
    public ConcurrentDictionary<PresentationIdentifier, IReadOnlyPresentation> TemplateHandles { get; } = new();

    /// <summary>
    ///     Gets the collection of presentation handles used for output generation.
    /// </summary>
    [JsonIgnore]
    public ConcurrentDictionary<PresentationIdentifier, IPresentation> OutputHandles { get; } = new();

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
}