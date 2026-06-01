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

using NetVips;
using OpenCvSharp;
using SlideGenerator.Image.Application.Models;

namespace SlideGenerator.Image.Infrastructure;

/// <summary>
///     Provides utility extension methods for converting between different image library types.
/// </summary>
internal static class Utilities
{
    /// <summary>
    ///     Converts a <see cref="System.Drawing.Size" /> to an OpenCvSharp <see cref="Size" />.
    /// </summary>
    /// <param name="size">The source size.</param>
    /// <returns>A new <see cref="Size" /> instance.</returns>
    public static Size ToOpenCv(this System.Drawing.Size size)
    {
        return new Size(size.Width, size.Height);
    }

    /// <summary>
    ///     Maps <see cref="InterestType" /> to libvips <see cref="Enums.Interesting" />.
    /// </summary>
    /// <param name="mode">The interest mode to map.</param>
    /// <returns>The corresponding libvips interesting enum value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the mode is not recognized.</exception>
    public static Enums.Interesting ToVips(this InterestType mode)
    {
        return mode switch
        {
            InterestType.Entropy => Enums.Interesting.Entropy,
            InterestType.Attention => Enums.Interesting.Attention,
            InterestType.Low => Enums.Interesting.Low,
            InterestType.High => Enums.Interesting.High,
            InterestType.All => Enums.Interesting.All,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}