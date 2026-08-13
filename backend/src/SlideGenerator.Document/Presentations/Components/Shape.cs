/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: Shape.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using SlideGenerator.Document.Presentations.Identifiers;
using Syncfusion.Presentation;

namespace SlideGenerator.Document.Presentations.Components;

/// <summary>
///     Represents a read-only view of a shape on a PowerPoint slide.
/// </summary>
public interface IReadOnlyShape
{
    /// <summary>
    ///     Gets the name of the shape.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the identifier of the shape.
    /// </summary>
    ShapeIdentifier Identifier { get; }

    /// <summary>
    ///     Gets the text displayed within the shape, if any.
    /// </summary>
    string DisplayText { get; }

    /// <summary>
    ///     Gets the bounding box in pixels of the shape.
    /// </summary>
    RectangleF Bounds { get; }

    /// <summary>
    ///     Gets the raw image data of the shape if it is a picture; otherwise, null.
    /// </summary>
    byte[]? ImageData { get; }

    /// <summary>
    ///     Gets the collection of paragraphs contained within the shape.
    /// </summary>
    IEnumerable<IReadOnlyParagraph> Paragraph { get; }

    /// <summary>
    ///     Gets the total number of paragraphs in the shape.
    /// </summary>
    int ParagraphsCount { get; }
}

/// <summary>
///     Represents a shape on a PowerPoint slide that can be modified.
/// </summary>
public interface IShape : IReadOnlyShape
{
    /// <summary>
    ///     Gets or sets the raw image data of the shape.
    /// </summary>
    new byte[]? ImageData { get; set; }

    /// <summary>
    ///     Gets the collection of paragraphs contained within the shape.
    /// </summary>
    new IEnumerable<IParagraph> Paragraph { get; }

    /// <inheritdoc />
    byte[]? IReadOnlyShape.ImageData => ImageData;

    /// <inheritdoc />
    IEnumerable<IReadOnlyParagraph> IReadOnlyShape.Paragraph => Paragraph;

    /// <summary>
    ///     Appends a new empty paragraph to this shape and returns it.
    /// </summary>
    IParagraph AddParagraph();

    /// <summary>
    ///     Clears all paragraphs from the shape.
    /// </summary>
    void ClearParagraph();
}

internal sealed class SfShape(Syncfusion.Presentation.IShape core) : IShape
{
    private const float EmuPerPixel = 9525.0f;

    public string Name => core.ShapeName;
    public ShapeIdentifier Identifier => new(Name);
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
