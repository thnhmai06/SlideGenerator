/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingContext.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Summarization.Domain.Models.Recipes;

namespace SlideGenerator.Generator.Domain.Models.Contexts;

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
    ///     The resolved recipe summary. Not persisted — reloaded by <c>LoadRecipeSummary</c> on each run.
    /// </summary>
    [JsonIgnore]
    public RecipeSummary? RecipeSummary { get; set; }

    /// <summary>
    ///     Persisted snapshot of validation items built by <c>LoadRecipeSummary</c>.
    ///     Used by Phase A ForEach to avoid re-evaluating from the transient RecipeSummary on resume.
    /// </summary>
    public List<ValidationItem> ValidationItems { get; set; } = [];

    /// <summary>
    ///     Persisted path to the workflow log file, used to reattach the logger on resume.
    /// </summary>
    public string WorkflowLogPath { get; init; } = null!;

    /// <summary>
    ///     Persisted scope name used when creating the workflow logger.
    /// </summary>
    public string WorkflowScope { get; init; } = null!;

    /// <summary>
    ///     Gets or sets the workflow-scoped logger factory, backed by a dedicated file sink.
    ///     Not serialized — recreated by <c>GeneratingMiddleware</c> before each step.
    ///     Steps call <c>LoggerFactory.CreateLogger(nameof(Step))</c> to obtain a named <see cref="ILogger" />.
    /// </summary>
    [JsonIgnore]
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    ///     Gets or sets the per-workflow asset deduplication coordinator.
    ///     Not serialized — recreated empty by <c>GeneratingMiddleware</c> on resume (idempotency handles the rest).
    /// </summary>
    [JsonIgnore]
    public ICoordinator? AssetCoordinator { get; set; } = null!;

    /// <summary>
    ///     The collection of validated worksheets and their target output configurations.
    ///     Populated during Phase A.
    /// </summary>
    public ConcurrentDictionary<SheetIdentifier, SheetContext> ValidWorksheets { get; set; } = new();

    /// <summary>
    ///     The collection of slide generation contexts containing data replacements.
    ///     List preserves insertion order required by WorkflowCore ForEach pointer tracking.
    /// </summary>
    public List<SlideContext> SlideContexts { get; set; } = [];

    /// <summary>
    ///     The collection of image processing contexts combining download and edit requirements.
    ///     List preserves insertion order required by WorkflowCore ForEach pointer tracking.
    /// </summary>
    public List<ImageContext> ImageContexts { get; set; } = [];

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
    ///     Per-context lazy factories ensuring at-most-once provider invocation under concurrent
    ///     access. Backed by <see cref="LazyThreadSafetyMode.ExecutionAndPublication" /> so that
    ///     racing threads share the same handle and the loser does not leak an open file.
    /// </summary>
    [JsonIgnore]
    public ConcurrentDictionary<BookIdentifier, Lazy<IReadOnlyWorkbook>> WorkbookFactories { get; } = new();

    /// <summary>Lazy factories for template presentations; see <see cref="WorkbookFactories" />.</summary>
    [JsonIgnore]
    public ConcurrentDictionary<PresentationIdentifier, Lazy<IReadOnlyPresentation>> TemplateFactories { get; } = new();

    /// <summary>Lazy factories for output presentations; see <see cref="WorkbookFactories" />.</summary>
    [JsonIgnore]
    public ConcurrentDictionary<PresentationIdentifier, Lazy<IPresentation>> OutputFactories { get; } = new();

    /// <summary>
    ///     Disposes of all open workbook and presentation handles and the workflow logger factory.
    /// </summary>
    public void Dispose()
    {
        LoggerFactory?.Dispose();
        DisposeAndClear(WorkbookHandles);
        DisposeAndClear(TemplateHandles);
        DisposeAndClear(OutputHandles);
    }

    private static void DisposeAndClear<TKey, TValue>(ConcurrentDictionary<TKey, TValue> handles)
        where TKey : notnull
        where TValue : IDisposable
    {
        foreach (var handle in handles.Values)
            try
            {
                handle.Dispose();
            }
            catch
            {
                /* ignore */
            }

        handles.Clear();
    }
}