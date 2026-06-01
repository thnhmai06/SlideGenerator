/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: LibvipsInterestCropper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Image.Infrastructure.Adapters;
using NetVipsEnums = NetVips.Enums;
using NetVipsImage = NetVips.Image;

namespace SlideGenerator.Image.Infrastructure.Services;

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