/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfSlide.cs
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
using Syncfusion.Presentation;
using IShape = SlideGenerator.Document.Domain.Abstractions.Slide.IShape;
using ISlide = SlideGenerator.Document.Domain.Abstractions.Slide.ISlide;

namespace SlideGenerator.Document.Infrastructure.Adapters.Slide;

internal sealed class SfSlide(Syncfusion.Presentation.ISlide core) : ISlide
{
    public int Number => core.SlideNumber;

    public IEnumerable<IShape> Shapes
    {
        get
        {
            return core.Shapes
                .OfType<Syncfusion.Presentation.IShape>()
                .Select(shape => new SfShape(shape));
        }
    }

    public int ShapesCount => core.Shapes.Count;

    public byte[] GetPreview()
    {
        using var stream = core.ConvertToImage(ExportImageFormat.Png);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
