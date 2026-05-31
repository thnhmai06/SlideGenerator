/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IFaceDetector.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Domain.Models;

namespace SlideGenerator.Image.Application.Abstractions;

/// <summary>
///     Defines a mechanism for detecting faces within image data.
/// </summary>
public interface IFaceDetector : IDisposable
{
    /// <summary>
    ///     Detects faces in the specified image matrix.
    /// </summary>
    public Task<IReadOnlyList<Face>> DetectAsync(IMat mat);
}