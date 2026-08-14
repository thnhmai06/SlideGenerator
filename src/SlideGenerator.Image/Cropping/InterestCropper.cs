/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: InterestCropper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using SlideGenerator.Image.Loading;
using NetVipsEnums = NetVips.Enums;
using NetVipsImage = NetVips.Image;

namespace SlideGenerator.Image.Cropping;

/// <summary>
///     Specifies the content-aware crop strategy to use.
///     Maps to <c>NetVips.Enums.Interesting</c> (excluding <c>Centre</c>).
/// </summary>
public enum InterestType : byte
{
    /// <summary>Maximizes the image entropy (texture / detail density).</summary>
    Entropy,

    /// <summary>Detects salient regions — faces, eyes, and other areas of visual attention.</summary>
    Attention,

    /// <summary>Crops toward the low (top-left) region of the image.</summary>
    Low,

    /// <summary>Crops toward the high (bottom-right) region of the image.</summary>
    High,

    /// <summary>Attempts to include all interesting content in the crop.</summary>
    All
}

/// <summary>
///     Performs content-aware (interest-based) image cropping using a third-party library.
/// </summary>
public interface IInterestCropper
{
    /// <summary>
    ///     Crops and resizes <paramref name="image" /> to <paramref name="targetSize" /> using the
    ///     specified <paramref name="mode" /> to select the region of interest.
    ///     Returns <see langword="null" /> when <paramref name="targetSize" /> has a zero or
    ///     negative dimension.
    /// </summary>
    /// <param name="image">Source image. Not modified.</param>
    /// <param name="targetSize">Desired output dimensions.</param>
    /// <param name="mode">Interest strategy.</param>
    /// <returns>
    ///     A new <see cref="IImage" /> cropped to <paramref name="targetSize" />, or
    ///     <see langword="null" /> for trivial inputs.
    /// </returns>
    IImage? Crop(IImage image, Size targetSize, InterestType mode);
}

/// <summary>
///     Implements <see cref="IInterestCropper" /> using libvips <c>thumbnail</c>.
///     When the source is a <see cref="VipsImage" />, the native pipeline is used directly
///     (no byte round-trip). For any other <see cref="IImage" />, bytes are decoded via
///     <see cref="NetVipsImage.ThumbnailBuffer" />.
/// </summary>
internal sealed class LibvipsInterestCropper : IInterestCropper
{
    /// <inheritdoc />
    public IImage? Crop(IImage image, Size targetSize, InterestType mode)
    {
        if (targetSize.Width <= 0 || targetSize.Height <= 0) return null;

        if (image is VipsImage vips)
        {
            var result = vips.Native.ThumbnailImage(targetSize.Width, targetSize.Height,
                crop: mode.ToVips(), size: NetVipsEnums.Size.Both);
            return new VipsImage(result);
        }

        var bytes = image.ToPng();
        var fromBytes = NetVipsImage.ThumbnailBuffer(bytes, targetSize.Width,
            height: targetSize.Height, crop: mode.ToVips(), size: NetVipsEnums.Size.Both);
        return new VipsImage(fromBytes);
    }
}