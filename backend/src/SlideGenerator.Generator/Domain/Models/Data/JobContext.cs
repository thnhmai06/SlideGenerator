/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobContext.cs
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
using SlideGenerator.Cloud.Models;
using SlideGenerator.Document.Domain.Abstractions.Slide;

namespace SlideGenerator.Generator.Domain.Models.Data;

/// <summary>
///     WorkflowCore data class for a single per-job generation workflow instance.
///     <see cref="Persist" /> is serialized to SQLite between steps.
///     <see cref="Transient" /> is excluded from serialization and recreated on resume.
/// </summary>
public sealed class JobContext : IDisposable
{
    /// <summary>Persisted workflow state — stored in the database.</summary>
    public JobPersistContext Persist { get; init; } = new();

    /// <summary>In-memory runtime state — not serialized, recreated on resume.</summary>
    [JsonIgnore]
    public TransientContext Transient { get; } = new();

    /// <inheritdoc />
    public void Dispose() => Transient.Dispose();
}

/// <summary>
///     Workflow state that is persisted to SQLite by WorkflowCore between steps.
///     All fields here must be serializable by Newtonsoft.Json.
/// </summary>
public sealed class JobPersistContext
{
    /// <summary>
    ///     ID grouping every <c>JobWorkflow</c> instance spawned for the same generation request.
    ///     Assigned once by <c>Service.StartAsync</c> when spawning; used to derive an aggregate
    ///     <see cref="Summary" /> across all jobs of the same request.
    /// </summary>
    public string RequestId { get; init; } = null!;

    /// <summary>The initial generation request. Duplicated across every job of the same request.</summary>
    public Request Request { get; init; } = null!;

    /// <summary>
    ///     The recipe snapshot frozen at the time <c>Service.StartAsync</c> read it.
    ///     Persisted so steps can resolve nodes by id without re-loading from the database on resume.
    ///     Newtonsoft handles <see cref="Recipe.Domain.Models.CanvasNode" /> polymorphism
    ///     via the <c>[JsonConverter]</c> attribute on that type.
    /// </summary>
    public Recipe.Domain.Models.Recipe Recipe { get; init; } = null!;

    /// <summary>Path to the workflow log file.</summary>
    public string LogPath { get; init; } = null!;

    /// <summary>The single generation job this workflow instance processes.</summary>
    public JobSpecification Specification { get; init; } = null!;

    /// <summary>
    ///     Every image cell source inspected so far for this job, keyed by the raw source string.
    ///     Populated in real time by <c>InspectUrlsStep</c> (in addition to the shared, app-wide
    ///     <c>ICache.InspectedUrls</c>) and consulted directly by <c>GenerateJobStep</c> when downloading —
    ///     it never re-queries the shared cache itself. A source missing from this dictionary, or mapped
    ///     to <see langword="null" />, means it did not resolve to an image; download is skipped for it.
    /// </summary>
    public ConcurrentDictionary<string, ContentInfo?> InspectedUrls { get; set; } = new();
}

/// <summary>
///     In-memory runtime state for a per-job generation workflow instance.
///     Not persisted — recreated fresh when a workflow resumes from SQLite.
/// </summary>
public sealed class TransientContext : IDisposable
{
    /// <summary>
    ///     Workflow-scoped logger factory backed by a dedicated file sink.
    ///     Steps call <c>LoggerFactory.CreateLogger(nameof(Step))</c> to get a named logger.
    ///     Initialized by <c>Middleware</c> before the first step; reused on resume.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>Cloned template slide keyed by the output path; avoids reopening the source template for each row.</summary>
    public ConcurrentDictionary<string, ISlide> TemplateSlides { get; } = new();

    /// <inheritdoc />
    public void Dispose()
    {
        LoggerFactory?.Dispose();
    }
}
