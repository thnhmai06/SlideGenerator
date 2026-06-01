/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Rules.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;

namespace SlideGenerator.Image.Domain;

/// <summary>
///     Provides domain rules and constants for the SlideGenerator Image processing.
/// </summary>
public static class Rules
{
    /// <summary>
    ///     The confidence threshold for face detection.
    /// </summary>
    public const float FaceConfidence = 0.8f;

    /// <summary>
    ///     The expected input size for the face detection model.
    /// </summary>
    public static readonly Size FaceInputSize = new(416, 416);
}