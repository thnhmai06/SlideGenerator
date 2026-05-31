/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: MagickImageInfo.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using ImageMagick;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Infrastructure.Adapters;

internal class MagickImageInfo(IMagickImageInfo core) : IImageInfo
{
    public uint Width => core.Width;
    public uint Height => core.Height;
}