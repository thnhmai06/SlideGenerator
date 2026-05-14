/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ImageComposer.cs
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
using SlideGenerator.Document.Application.Abstractions;
using Syncfusion.Presentation;
using IShape = Syncfusion.Presentation.IShape;

namespace SlideGenerator.Document.Application.Services;

/// <summary>
///     Replaces image content in Syncfusion presentation shapes.
/// </summary>
public sealed class ImageComposer : IImageComposer
{
    /// <inheritdoc />
    public void Replace(IShape shape, Stream imageStream)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(imageStream);

        using var buffer = new MemoryStream();
        imageStream.CopyTo(buffer);
        var imageBytes = buffer.ToArray();

        if (shape is IPicture picture)
        {
            picture.ImageData = imageBytes;
            return;
        }

        if (shape.Fill.FillType != FillType.Picture)
            throw new InvalidOperationException($"Shape '{shape.ShapeName}' is not an image shape.");

        shape.Fill.PictureFill.ImageBytes = imageBytes;
    }
}
