/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: Paragraph.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SyncfusionParagraph = Syncfusion.Presentation.IParagraph;

namespace SlideGenerator.Document.Presentations.Components;

/// <summary>
///     Represents a read-only view of a paragraph in a shape.
/// </summary>
public interface IReadOnlyParagraph
{
    /// <summary>
    ///     Gets the collection of text parts within the paragraph.
    /// </summary>
    IEnumerable<IReadOnlyTextPart> TextParts { get; }

    /// <summary>
    ///     Gets the total number of text parts in the paragraph.
    /// </summary>
    int TextPartsCount { get; }
}

/// <summary>
///     Represents a paragraph in a shape that can be modified.
/// </summary>
public interface IParagraph : IReadOnlyParagraph
{
    /// <summary>
    ///     Gets the collection of text parts within the paragraph.
    /// </summary>
    new IEnumerable<ITextPart> TextParts { get; }

    /// <inheritdoc />
    IEnumerable<IReadOnlyTextPart> IReadOnlyParagraph.TextParts => TextParts;

    /// <summary>
    ///     Adds a text part to the end of the paragraph.
    /// </summary>
    /// <param name="textPart">The text part to add.</param>
    /// <returns>The added text part.</returns>
    ITextPart AddTextPart(ITextPart textPart);

    /// <summary>
    ///     Removes the text part at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the text part to remove.</param>
    void RemoveAt(int index);
}

internal class SfParagraph(SyncfusionParagraph core) : IParagraph
{
    internal readonly SyncfusionParagraph Core = core;
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
            targetFont.Superscript = sourceFont.Superscript;
            targetFont.FontName = sourceFont.FontName;
            targetFont.FontSize = sourceFont.FontSize;
            targetFont.StrikeType = sourceFont.StrikeType;
            targetFont.Underline = sourceFont.Underline;
            targetFont.LanguageID = sourceFont.LanguageID;
            if (sourceFont.HighlightColor.A != 0)
                targetFont.HighlightColor = sourceFont.HighlightColor;
        }

        return new SfTextPart(coreTextPart);
    }

    public void RemoveAt(int index)
    {
        Core.TextParts.RemoveAt(index);
    }
}
