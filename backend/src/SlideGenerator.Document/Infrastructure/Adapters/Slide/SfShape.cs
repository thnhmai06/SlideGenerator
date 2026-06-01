/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfShape.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Syncfusion.Presentation;
using IParagraph = SlideGenerator.Document.Domain.Abstractions.Slide.IParagraph;
using IShape = SlideGenerator.Document.Domain.Abstractions.Slide.IShape;

namespace SlideGenerator.Document.Infrastructure.Adapters.Slide;

internal sealed class SfShape(Syncfusion.Presentation.IShape shape) : IShape
{
    private const float EmuPerPixel = 9525.0f;

    public string Name => shape.ShapeName;
    public string DisplayText => shape.TextBody?.Text ?? string.Empty;

    public RectangleF Bounds => new(
        (float)shape.Left / EmuPerPixel,
        (float)shape.Top / EmuPerPixel,
        (float)shape.Width / EmuPerPixel,
        (float)shape.Height / EmuPerPixel);

    public IEnumerable<IParagraph> Paragraph =>
        shape.TextBody.Paragraphs.Select(paragraph => new SfParagraph(paragraph));

    public int ParagraphsCount => shape.TextBody.Paragraphs.Count;

    public byte[]? ImageData
    {
        get => GetOrSetImageData();
        set => GetOrSetImageData(value);
    }

    public IParagraph AddParagraph()
    {
        var coreParagraph = shape.TextBody.AddParagraph();
        return new SfParagraph(coreParagraph);
    }

    public void ClearParagraph()
    {
        shape.TextBody.Paragraphs.Clear();
    }

    private byte[]? GetOrSetImageData(byte[]? imageBytes = null)
    {
        // Picture
        if (shape is IPicture picture)
        {
            if (imageBytes != null)
                picture.ImageData = imageBytes;
            return picture.ImageData;
        }

        // BlipFill
        if (shape.Fill.FillType == FillType.Picture)
        {
            if (imageBytes != null)
                shape.Fill.PictureFill.ImageBytes = imageBytes;
            return shape.Fill.PictureFill.ImageBytes;
        }

        return null;
    }
}