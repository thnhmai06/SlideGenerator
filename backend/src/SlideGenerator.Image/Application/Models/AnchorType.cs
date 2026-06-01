/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: AnchorType.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Application.Models;

/// <summary>
///     Specifies which point on the subject to use as the anchor for ROI calculation.
/// </summary>
public enum AnchorType
{
    /// <summary>
    ///     Image center. Origin = image center; unit X = image width, unit Y = image height.
    ///     <c>(0, 0)</c> = center, <c>(-0.5, -0.5)</c> = top-left, <c>(0.5, 0.5)</c> = bottom-right.
    /// </summary>
    Image,

    /// <summary>
    ///     Face bounding-box center. Origin = FaceCenter; unit X = rect.Width, unit Y = rect.Height.
    /// </summary>
    Face,

    /// <summary>
    ///     Eye midpoint. Origin = EyesCenter; unit = <c>dist(leftEye, rightEye)</c> for both axes.
    ///     <c>(-0.5, 0)</c> = left eye, <c>(0.5, 0)</c> = right eye.
    /// </summary>
    Eyes,

    /// <summary>
    ///     Nose landmark. Origin = Nose tip; unit X = face rect.Width, unit Y = face rect.Height.
    /// </summary>
    Nose,

    /// <summary>
    ///     Mouth midpoint. Origin = MouthCenter; unit = <c>dist(leftMouth, rightMouth)</c> for both axes.
    ///     <c>(-0.5, 0)</c> = left corner, <c>(0.5, 0)</c> = right corner.
    /// </summary>
    Mouth
}