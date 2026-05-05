/*
 * Copyright (C) 2026 Thành Mai
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

using ImageMagick;
using Microsoft.Extensions.Logging;
using Syncfusion.Presentation;

namespace SlideGenerator.Document.Slide.Services;

/// <summary>
///     Provides utility methods for replacing image content in Syncfusion presentation shapes.
/// </summary>
/// <remarks>
///     This composer handles image replacement for both IPicture shapes and shapes with picture fill,
///     supporting multiple input formats: MagickImage, Stream, and byte arrays.
/// </remarks>
public sealed class ImageComposer(ILogger<ImageComposer> logger)
{
    /// <summary>
    ///     Replaces the image content of a shape with the provided MagickImage.
    /// </summary>
    /// <param name="shape">The Syncfusion shape to modify.</param>
    /// <param name="image">The MagickImage containing the new image data.</param>
    /// <returns>The number of images replaced (1 for success, 0 for no eligible shape).</returns>
    /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
    /// <remarks>
    ///     The MagickImage is automatically converted to a byte array for storage in the shape.
    ///     Supports both IPicture shapes and shapes with picture fill.
    /// </remarks>
    public int Replace(IShape shape, MagickImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var imageBytes = image.ToByteArray();
        return Replace(shape, imageBytes);
    }

    /// <summary>
    ///     Replaces the image content of a shape with the provided image stream.
    /// </summary>
    /// <param name="shape">The Syncfusion shape to modify.</param>
    /// <param name="imageStream">The stream containing the new image data.</param>
    /// <returns>The number of images replaced (1 for success, 0 for no eligible shape).</returns>
    /// <exception cref="ArgumentNullException">Thrown when imageStream is null.</exception>
    /// <remarks>
    ///     This method reads the entire stream into memory before updating the shape.
    ///     The stream position is not reset after reading.
    /// </remarks>
    public int Replace(IShape shape, Stream imageStream)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        using var ms = new MemoryStream();
        imageStream.CopyTo(ms);
        var imageBytes = ms.ToArray();
        return Replace(shape, imageBytes);
    }

    /// <summary>
    ///     Replaces the image content of a shape with the provided byte array.
    /// </summary>
    /// <param name="shape">The Syncfusion shape to modify.</param>
    /// <param name="imageBytes">The byte array containing the new image data.</param>
    /// <returns>The number of images replaced (1 for IPicture or picture fill, 0 if shape type not supported).</returns>
    /// <remarks>
    ///     <para>This method supports two shape types:</para>
    ///     <list type="bullet">
    ///         <item>IPicture shapes: Direct image replacement via ImageData property</item>
    ///         <item>Shapes with picture fill (FillType.Picture): Picture fill replacement via PictureFill.ImageBytes</item>
    ///     </list>
    ///     <para>Other shape types return 0 (no replacement attempted).</para>
    /// </remarks>
    public int Replace(IShape shape, byte[] imageBytes)
    {
        logger.LogDebug("Attempting image replacement for shape '{ShapeName}' (Type: {Type})", shape.ShapeName,
            shape.GetType().Name);

        // Picture
        if (shape is IPicture picture)
        {
            picture.ImageData = imageBytes;
            logger.LogDebug("Successfully replaced image for IPicture shape '{ShapeName}'", shape.ShapeName);
            return 1;
        }

        // BlipFill
        if (shape.Fill.FillType == FillType.Picture)
        {
            shape.Fill.PictureFill.ImageBytes = imageBytes;
            logger.LogDebug("Successfully replaced image for picture-fill shape '{ShapeName}'", shape.ShapeName);
            return 1;
        }

        logger.LogWarning(
            "Shape '{ShapeName}' is not an IPicture and does not have picture fill. Skipping replacement.",
            shape.ShapeName);
        return 0;
    }
}