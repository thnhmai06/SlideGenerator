/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobPhase.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Jobs.Models;

/// <summary>
///     Identifies which phase of the <c>CreatingOutput -> Generating -> Done</c> pipeline a job is currently in.
///     A job always progresses forward (never regresses) through these phases;
///     <see cref="JobSnapshot.CurrentIndex" /> tracks how far it got within the current phase.
/// </summary>
public enum JobPhase : byte
{
    /// <summary>Just get started, didn't do anything.</summary>
    Queued,

    /// <summary>Opening (resume) or creating (fresh) the output presentation file.</summary>
    CreatingOutput,

    /// <summary>
    ///     Per data row: append its slide, fill its text, fill its images.
    ///     <see cref="JobSnapshot.CurrentIndex" /> is the number of rows whose slide is already saved in the
    ///     working file.
    /// </summary>
    Generating,

    /// <summary>Legacy — no longer emitted; kept so job rows written by earlier builds still parse.</summary>
    CreatingSlides,

    /// <summary>Legacy — no longer emitted; kept so job rows written by earlier builds still parse.</summary>
    FillingText,

    /// <summary>Legacy — no longer emitted; kept so job rows written by earlier builds still parse.</summary>
    FillingImages,

    /// <summary>Every phase has completed for this job.</summary>
    Done
}