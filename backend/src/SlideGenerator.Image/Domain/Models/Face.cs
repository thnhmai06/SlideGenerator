/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Face.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;

namespace SlideGenerator.Image.Domain.Models;

/// <summary>
///     Represents a face with its bounding box, score, and eye landmarks (if available).
/// </summary>
/// <param name="Rect">The bounding box of the detected face.</param>
/// <param name="Score">The confidence score for this detection.</param>
/// <param name="RightEye">The right eye landmark (if provided by the model).</param>
/// <param name="LeftEye">The left eye landmark (if provided by the model).</param>
/// <param name="Nose">The nose landmark (if provided by the model).</param>
/// <param name="RightMouth">The right mouth corner landmark (if provided by the model).</param>
/// <param name="LeftMouth">The left mouth corner landmark (if provided by the model).</param>
public readonly record struct Face(
    Rectangle Rect,
    float Score,
    Point? RightEye = null,
    Point? LeftEye = null,
    Point? Nose = null,
    Point? RightMouth = null,
    Point? LeftMouth = null)
{
    /// <summary>
    ///     Gets the center point of the face-bounding box.
    /// </summary>
    public Point FaceCenter => new(Rect.X + (Rect.Width / 2), Rect.Y + (Rect.Height / 2));

    /// <summary>
    ///     Gets the center point between the left and right eyes, if both landmarks are available.
    /// </summary>
    /// <returns>A <see cref="Point" /> if both eyes are present; otherwise, <see langword="null" />.</returns>
    public Point? EyesCenter => RightEye.HasValue && LeftEye.HasValue
        ? new Point((RightEye.Value.X + LeftEye.Value.X) / 2, (RightEye.Value.Y + LeftEye.Value.Y) / 2)
        : null;

    /// <summary>
    ///     Gets the center point between the left and right mouth corners, if both landmarks are available.
    /// </summary>
    /// <returns>A <see cref="Point" /> if both mouth corners are present; otherwise, <see langword="null" />.</returns>
    public Point? MouthCenter => RightMouth.HasValue && LeftMouth.HasValue
        ? new Point((RightMouth.Value.X + LeftMouth.Value.X) / 2, (RightMouth.Value.Y + LeftMouth.Value.Y) / 2)
        : null;
}