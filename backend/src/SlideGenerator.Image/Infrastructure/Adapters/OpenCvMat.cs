/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: OpenCvMat.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using OpenCvSharp;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Infrastructure.Adapters;

/// <summary>
///     Adapter for <see cref="OpenCvSharp.Mat" /> implementing <see cref="IMat" />.
/// </summary>
internal sealed class OpenCvMat(Mat mat) : IMat
{
    internal readonly Mat Mat = mat;

    public int Width => Mat.Width;
    public int Height => Mat.Height;
    public bool Empty => Mat.Empty();

    public IMat Clone()
    {
        return new OpenCvMat(Mat.Clone());
    }

    public void Dispose()
    {
        Mat.Dispose();
    }
}