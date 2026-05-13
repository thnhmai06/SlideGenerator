/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: OpenCvMat.cs
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

using OpenCvSharp;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Infrastructure.Adapters;

/// <summary>
///     Adapter for <see cref="OpenCvSharp.Mat" /> implementing <see cref="IMat" />.
/// </summary>
public sealed class OpenCvMat(Mat mat) : IMat
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