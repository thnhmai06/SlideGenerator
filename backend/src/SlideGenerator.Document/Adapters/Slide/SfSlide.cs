/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfSlide.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Security.Cryptography;
using System.Text;
using SlideGenerator.Document.Models.Slide;
using Syncfusion.Presentation;
using Syncfusion.PresentationRenderer;
using IReadOnlySlide = SlideGenerator.Document.Abstractions.Slide.IReadOnlySlide;
using IShape = SlideGenerator.Document.Abstractions.Slide.IShape;
using ISlide = SlideGenerator.Document.Abstractions.Slide.ISlide;

namespace SlideGenerator.Document.Adapters.Slide;

internal sealed class SfSlide(Syncfusion.Presentation.ISlide core) : ISlide
{
    internal Syncfusion.Presentation.ISlide Core { get; } = core;

    public IEnumerable<IShape> Shapes
        => Core.Shapes
            .OfType<Syncfusion.Presentation.IShape>()
            .Select(shape => new SfShape(shape));

    public SlideIdentifier Identifier => new(Core.SlideNumber);

    public byte[] GetPreview()
    {
        var renderer = new PresentationRenderer();
        using var stream = renderer.ConvertToImage(Core, ExportImageFormat.Png);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public ISlide Clone() => new SfSlide(Core.Clone());

    public bool Equals(IReadOnlySlide? other)
    {
        if (other is null) return false;
        if (other is not SfSlide sfOther) 
            return GetPreview().SequenceEqual(other.GetPreview());
        if (Shapes.Count() != other.Shapes.Count()) return false;

        var sb = new StringBuilder();

        // this Hash
        sb.Append(Core.SlideNumber);
        foreach (var s in Core.Shapes.OfType<Syncfusion.Presentation.IShape>())
        {
            sb.Append($"|{s.Left:F2},{s.Top:F2},{s.Width:F2},{s.Height:F2}");
            if (s.TextBody?.Paragraphs == null) continue;
            foreach (var p in s.TextBody.Paragraphs)
                sb.Append(p.Text ?? "");
        }
        var thisHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));

        // other Hash
        sb.Clear();
        sb.Append(sfOther.Core.SlideNumber);
        foreach (var s in sfOther.Core.Shapes.OfType<Syncfusion.Presentation.IShape>())
        {
            sb.Append($"|{s.Left:F2},{s.Top:F2},{s.Width:F2},{s.Height:F2}");
            if (s.TextBody?.Paragraphs == null) continue;
            foreach (var p in s.TextBody.Paragraphs)
                sb.Append(p.Text ?? "");
        }
        var otherHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));

        return thisHash == otherHash;
    }
}