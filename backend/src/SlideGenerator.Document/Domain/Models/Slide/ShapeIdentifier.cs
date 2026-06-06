/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ShapeIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Models.Slide;

/// <summary>
///     Identifies a shape by its name within an already-known slide context.
///     Use alongside <see cref="SlideIdentifier" /> and <see cref="PresentationIdentifier" />
///     when parent context is supplied separately.
/// </summary>
/// <param name="ShapeName">The unique name of the shape (e.g., "Rectangle 1").</param>
public record ShapeIdentifier(string ShapeName);