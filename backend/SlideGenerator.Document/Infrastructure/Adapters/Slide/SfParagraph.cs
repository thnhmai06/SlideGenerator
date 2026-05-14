/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfParagraph.cs
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
using ITextPart = SlideGenerator.Document.Domain.Abstractions.Slide.ITextPart;

namespace SlideGenerator.Document.Infrastructure.Adapters.Slide;

internal class SfParagraph(IParagraph core) : Domain.Abstractions.Slide.IParagraph
{
    internal readonly IParagraph Core = core;
    public IEnumerable<ITextPart> TextParts => Core.TextParts.Select(tp => new SfTextPart(tp));
    public int TextPartsCount => Core.TextParts.Count;

    public ITextPart AddTextPart(ITextPart textPart)
    {
        var coreTextPart = Core.AddTextPart(textPart.Text);

        if (textPart is SfTextPart sfTextPart)
        {
            var sourceFont = sfTextPart.Core.Font;
            var targetFont = coreTextPart.Font;

            targetFont.CapsType = sourceFont.CapsType;
            targetFont.Color = sourceFont.Color;
            targetFont.Bold = sourceFont.Bold;
            targetFont.Italic = sourceFont.Italic;
            targetFont.Subscript = sourceFont.Subscript;
            targetFont.Subscript = sourceFont.Subscript;
            targetFont.FontName = sourceFont.FontName;
            targetFont.FontSize = sourceFont.FontSize;
            targetFont.StrikeType = sourceFont.StrikeType;
            targetFont.Underline = sourceFont.Underline;
            targetFont.LanguageID = sourceFont.LanguageID;
            targetFont.HighlightColor = sourceFont.HighlightColor;
        }

        return new SfTextPart(coreTextPart);
    }

    public void RemoveAt(int index)
    {
        Core.TextParts.RemoveAt(index);
    }
}

