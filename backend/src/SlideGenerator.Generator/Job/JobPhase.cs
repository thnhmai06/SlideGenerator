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

namespace SlideGenerator.Generator.Job;

/// <summary>
///     Identifies which of the 4 sequential phases a job is currently in. A job always progresses
///     forward (never regresses) through these phases; <see cref="JobRecord.CurrentIndex" />
///     tracks how far it got within the current phase.
/// </summary>
public enum JobPhase
{
    /// <summary>Opening (resume) or creating (fresh) the output presentation file.</summary>
    CreatingOutput,

    /// <summary>Appending one blank slide (cloned from the template) per data row.</summary>
    CreatingSlides,

    /// <summary>Composing text placeholders into every appended slide.</summary>
    FillingText,

    /// <summary>Downloading, cropping, and inserting images into every appended slide.</summary>
    FillingImages,

    /// <summary>Every phase has completed for this job.</summary>
    Done
}
