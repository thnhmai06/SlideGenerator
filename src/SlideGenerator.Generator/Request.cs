/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Request.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Presentations.Identifiers;

namespace SlideGenerator.Generator;

/// <summary>
///     Represents the user-provided request to start a slide generation process.
/// </summary>
/// <param name="RecipeId">The database id of the recipe to use.</param>
/// <param name="Name">The display name of the generation job.</param>
/// <param name="OutputType">The desired file extension for the output presentations.</param>
/// <param name="SaveFolder">The root directory where generated presentations will be saved.</param>
/// <param name="AllowLocalPaths">
///     Gets whether local file paths are accepted.
///     When <see langword="true" />, a cell value that matches an existing local file bypasses download
///     and is hard-linked (or copied) to the destination instead.
/// </param>
public sealed record Request(
    int RecipeId,
    string Name,
    PresentationType OutputType,
    string SaveFolder,
    bool AllowLocalPaths = false)
{
    /// <summary>
    ///     Gets the validated save folder path.
    /// </summary>
    public string SaveFolder { get; init; } = !string.IsNullOrWhiteSpace(SaveFolder)
        ? Path.GetFullPath(SaveFolder)
        : throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder));
}

/// <summary>
///     Persisted row for one generation request — the original <see cref="Request" /> plus the identity
///     and bookkeeping fields needed to reconstruct <see cref="Summary" />/resume jobs after a restart.
/// </summary>
/// <param name="RequestId">Groups every job spawned for this request.</param>
/// <param name="Request">The original submitted request.</param>
/// <param name="LogPath">Path to the single log file shared by every job of this request.</param>
/// <param name="CreatedAt">UTC timestamp when the request was created.</param>
public sealed record RequestRecord(
    string RequestId,
    Request Request,
    string LogPath,
    DateTimeOffset CreatedAt);

/// <summary>
///     Discriminates why a <see cref="PlannedJob" />'s output path is unusable, if at all. Returned by
///     <see cref="IService.PreviewAsync" /> so a caller can show every conflict before submitting, using the
///     exact same conflict definition <see cref="IService.CreateAsync" /> enforces at submit time.
/// </summary>
public enum ConflictKind
{
    /// <summary>No conflict — this output path is free to write to.</summary>
    None,

    /// <summary>Another job computed from the same request would write to this same output path.</summary>
    DuplicateWithinRequest,

    /// <summary>A job of a different, currently active (running, pending, or paused) request already claims this output path.</summary>
    ConflictsWithActiveRequest
}

/// <summary>
///     One job <see cref="IService.CreateAsync" /> would spawn for a <see cref="Request" />, as computed by
///     <see cref="IService.PreviewAsync" /> — lets a run confirmation dialog show the full fan-out (one job
///     per worksheet source × mapping, see <c>Service.BuildJobs</c>) and every output-path conflict before
///     the request is actually submitted.
/// </summary>
/// <param name="OutputPath">Absolute path this job would write its output presentation to.</param>
/// <param name="WorkbookPath">Absolute path to the source workbook this job reads from.</param>
/// <param name="WorksheetName">Name of the source worksheet within <see cref="WorkbookPath" />.</param>
/// <param name="ConflictKind">Why <see cref="OutputPath" /> is unusable, or <see cref="Generator.ConflictKind.None" /> if it is not.</param>
public sealed record PlannedJob(
    string OutputPath,
    string WorkbookPath,
    string WorksheetName,
    ConflictKind ConflictKind);