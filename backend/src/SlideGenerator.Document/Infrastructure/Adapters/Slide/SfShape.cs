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

internal sealed class SfShape(Syncfusion.Presentation.IShape core) : IShape
{
    private const float EmuPerPixel = 9525.0f;

    public string Name => core.ShapeName;
    public string DisplayText => core.TextBody?.Text ?? string.Empty;

    public RectangleF Bounds => new(
        (float)core.Left / EmuPerPixel,
        (float)core.Top / EmuPerPixel,
        (float)core.Width / EmuPerPixel,
        (float)core.Height / EmuPerPixel);

    public IEnumerable<IParagraph> Paragraph =>
        core.TextBody.Paragraphs.Select(paragraph => new SfParagraph(paragraph));

    public int ParagraphsCount => core.TextBody.Paragraphs.Count;

    public byte[]? ImageData
    {
        get => GetOrSetImageData();
        set => GetOrSetImageData(value);
    }

    public IParagraph AddParagraph()
    {
        var coreParagraph = core.TextBody.AddParagraph();
        return new SfParagraph(coreParagraph);
    }

    public void ClearParagraph()
    {
        core.TextBody.Paragraphs.Clear();
    }

    private byte[]? GetOrSetImageData(byte[]? imageBytes = null)
    {
        // Picture
        if (core is IPicture picture)
        {
            if (imageBytes != null)
                picture.ImageData = imageBytes;
            return picture.ImageData;
        }

        // BlipFill
        if (core.Fill.FillType == FillType.Picture)
        {
            if (imageBytes != null)
                core.Fill.PictureFill.ImageBytes = imageBytes;
            return core.Fill.PictureFill.ImageBytes;
        }

        return null;
    }
}