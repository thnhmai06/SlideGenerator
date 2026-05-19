/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: EditOptions.cs
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

using SlideGenerator.Image.Application.Models;

namespace SlideGenerator.Summarization.Domain.Models.Recipes;

/// <summary>
///     Defines the processing rules for image transformations.
/// </summary>
/// <param name="RoiOption">The algorithm to use for Region of Interest (ROI) detection and cropping.</param>
public sealed record EditOptions(RoiOption RoiOption);