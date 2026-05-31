/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiResolver.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Entities;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Application.Services;

/// <summary>
///     Resolves Region of Interest (ROI) for images using configurable algorithms.
/// </summary>
/// <remarks>
///     This service routes ROI calculation requests to appropriate calculator implementations
///     (Center or Rule of Thirds) and supports intelligent feature detection via face detection.
/// </remarks>
public sealed class RoiResolver(IFaceDetector faceDetector, IMatFactory matFactory, ILogger<RoiResolver>? logger = null)
    : IRoiResolver
{
    private readonly ReadOnlyDictionary<RoiType, RoiCalculator> _calculators =
        new Dictionary<RoiType, RoiCalculator>
        {
            { RoiType.Center, new CenterRoi(faceDetector, matFactory) },
            { RoiType.RuleOfThirds, new RuleOfThirdsRoi(faceDetector, matFactory) }
        }.AsReadOnly();

    public async ValueTask<Rectangle> CalculateRoiAsync(IImage image, Size targetSize,
        RoiOption option)
    {
        logger?.LogDebug("Calculating ROI using {Type} algorithm for image ({Width}x{Height}) targeting {TargetSize}",
            option.Type, image.Info.Width, image.Info.Height, targetSize);

        try
        {
            var roi = await GetCalculator(option.Type).CalculateRoiAsync(image, targetSize, option)
                .ConfigureAwait(false);
            logger?.LogDebug("Calculated ROI: {ROI}", roi);
            return roi;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to calculate ROI using {Type} algorithm", option.Type);
            throw;
        }
    }

    private RoiCalculator GetCalculator(RoiType key)
    {
        return _calculators.TryGetValue(key, out var calculator)
            ? calculator
            : throw new ArgumentOutOfRangeException(nameof(key), key, null);
    }
}