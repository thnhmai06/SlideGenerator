/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: IMat.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Image.Domain.Entities;

/// <summary>
///     Represents an abstract matrix (image) for computer vision operations.
/// </summary>
/// <remarks>
///     This interface decouples the core logic from specific CV libraries like OpenCvSharp.
/// </remarks>
public interface IMat : IDisposable
{
    /// <summary>
    ///     Gets the width of the matrix.
    /// </summary>
    int Width { get; }

    /// <summary>
    ///     Gets the height of the matrix.
    /// </summary>
    int Height { get; }

    /// <summary>
    ///     Checks if the matrix is empty.
    /// </summary>
    bool Empty { get; }

    /// <summary>
    ///     Clones the current matrix.
    /// </summary>
    /// <returns>A new <see cref="IMat" /> instance that is a copy of this matrix.</returns>
    IMat Clone();
}