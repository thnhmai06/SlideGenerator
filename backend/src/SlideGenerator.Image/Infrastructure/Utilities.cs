/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Utilities.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using OpenCvSharp;

namespace SlideGenerator.Image.Infrastructure;

internal static class Utilities
{
    public static Size ToOpenCv(this System.Drawing.Size size)
    {
        return new Size(size.Width, size.Height);
    }
}