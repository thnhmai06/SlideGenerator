/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: ImageEdits.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Image.Application.Models;

namespace SlideGenerator.Recipe.Domain.Models.Summary;

/// <summary>
///     Defines the processing rules for image transformations.
/// </summary>
/// <param name="RoiOptions">
///     Ordered fallback chain of ROI options. The resolver tries each in order and uses the
///     first that succeeds. If all anchor options fail, image-center crop is used.
/// </param>
public sealed record ImageEdits(IReadOnlyList<RoiOption> RoiOptions);
