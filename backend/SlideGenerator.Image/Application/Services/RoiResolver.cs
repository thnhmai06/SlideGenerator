/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RoiResolver.cs
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
using System.Collections.ObjectModel;
using System.Drawing;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Entities;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Image.Application.Services;

/// <summary>
///     Resolves Region of Interest (ROI) for images using configurable algorithms.
/// </summary>
/// <remarks>
///     This service routes ROI calculation requests to appropriate calculator implementations
///     (Center or Rule of Thirds) and supports intelligent feature detection via face detection.
/// </remarks>
public sealed class RoiResolver(IFaceDetector faceDetector, IMatFactory matFactory, ISystemLogger logger) : IRoiResolver
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
        logger.Debug("Calculating ROI using {Type} algorithm for image ({Width}x{Height}) targeting {TargetSize}",
            option.Type, image.Width, image.Height, targetSize);

        try
        {
            var roi = await GetCalculator(option.Type).CalculateRoiAsync(image, targetSize, option)
                .ConfigureAwait(false);
            logger.Debug("Calculated ROI: {ROI}", roi);
            return roi;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to calculate ROI using {Type} algorithm", option.Type);
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






