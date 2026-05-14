/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: RuleOfThirdsRoi.cs
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

using System.Drawing;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Image.Domain.Entities;

namespace SlideGenerator.Image.Application.Entities;

/// <summary>
///     Calculates rule-of-thirds ROI with face and eye landmark fallbacks.
/// </summary>
internal sealed class RuleOfThirdsRoi(IFaceDetector faceDetector, IMatFactory matFactory) : RoiCalculator
{
    public override async ValueTask<Rectangle> CalculateRoiAsync(IImage image, Size targetSize, RoiOption option)
    {
        if (option.Type != RoiType.RuleOfThirds)
            throw new ArgumentException($"Invalid ROI type '{option.Type}' for {nameof(RuleOfThirdsRoi)}.",
                nameof(option.Type));

        var ruleOption = option as RuleOfThirdsOption;
        var pivot = ruleOption?.Pivot;
        var sourceSize = new Size((int)image.Width, (int)image.Height);

        using var mat = matFactory.Create(image);
        var faces = await faceDetector.DetectAsync(mat).ConfigureAwait(false);
        if (faces.Count <= 0) return Utilities.CalculateAnchoredRectangle(sourceSize, targetSize);

        var eyeCenter = faces.Centroid(face => face.EyesCenter);
        if (eyeCenter.HasValue)
            return Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, eyeCenter.Value, pivot);

        var faceCenter = faces.Centroid(face => face.FaceCenter);
        return faceCenter.HasValue
            ? Utilities.CalculateAnchoredRectangle(sourceSize, targetSize, faceCenter.Value, pivot)
            : Utilities.CalculateAnchoredRectangle(sourceSize, targetSize);
    }
}