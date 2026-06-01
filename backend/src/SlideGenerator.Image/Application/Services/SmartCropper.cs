/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: SmartCropper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Application.Services;

/// <summary>
///     Walks a fallback chain of <see cref="RoiOption" /> items, delegating each option to the
///     appropriate cropper. Returns the first successful result, or <see langword="null" /> if
///     all options fail.
/// </summary>
internal sealed class SmartCropper(
    IAnchorCropper anchorCropper,
    IInterestCropper interestCropper,
    ILogger<SmartCropper>? logger = null)
    : ISmartCropper
{
    public async ValueTask<IImage?> CropAsync(IImage image, Size targetSize, params RoiOption[] options)
    {
        if (options.Length == 0) return null;
        if (targetSize.Width <= 0 || targetSize.Height <= 0) return null;

        logger?.LogDebug("Cropping image ({W}x{H}) → target {T}, {N} options",
            image.Info.Width, image.Info.Height, targetSize, options.Length);

        foreach (var option in options)
            switch (option)
            {
                case InterestOption interest:
                {
                    var result = interestCropper.Crop(image, targetSize, interest.Type);
                    if (result is not null) return result;
                    break;
                }
                case AnchorOption anchor:
                {
                    var result = await anchorCropper.CropAsync(image, targetSize, anchor).ConfigureAwait(false);
                    if (result is not null) return result;
                    break;
                }
            }

        return null;
    }
}