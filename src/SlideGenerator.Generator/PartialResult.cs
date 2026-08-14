/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: PartialResult.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator;

/// <summary>
///     Result of a best-effort fan-out operation (cancel/pause/resume) applied to every job of a
///     generation request. Jobs already in a terminal or non-runnable state are counted as skipped,
///     not as failures.
/// </summary>
public sealed record PartialResult(int Succeeded, int Skipped);