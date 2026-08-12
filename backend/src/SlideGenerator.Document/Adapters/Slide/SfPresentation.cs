/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfPresentation.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Models.Slide;
using Syncfusion.Presentation;
using IReadOnlySlide = SlideGenerator.Document.Abstractions.Slide.IReadOnlySlide;
using ISlide = SlideGenerator.Document.Abstractions.Slide.ISlide;

namespace SlideGenerator.Document.Adapters.Slide;

/// <summary>
///     Wraps a Syncfusion IPresentation and its FileStream for proper disposal and saving.
/// </summary>
internal sealed class SfPresentation(
    IPresentation core,
    PresentationIdentifier identifier,
    FileStream? fileStream = null) : Abstractions.Slide.IPresentation
{
    public IEnumerable<ISlide> Slides
    {
        get { return core.Slides.Select(slide => new SfSlide(slide)); }
    }

    public PresentationIdentifier Identifier { get; } = identifier;

    public int SlidesCount => core.Slides.Count;

    public bool IsWriteProtected => core.IsWriteProtected;

    /// <summary>
    ///     Disposes of the presentation and any underlying file streams.
    /// </summary>
    public void Dispose()
    {
        core.Dispose();
        fileStream?.Dispose();
    }

    public void RemoveSlideAt(int index)
    {
        core.Slides.RemoveAt(index);
    }

    public void AddSlide(IReadOnlySlide slide)
    {
        core.Slides.Add(((SfSlide)slide).Core, PasteOptions.SourceFormatting);
    }

    public void RemoveEncryption()
    {
        core.RemoveEncryption();
    }

    public void RemoveWriteProtection()
    {
        core.RemoveWriteProtection();
    }

    public void Save(string path)
    {
        core.Save(path);
    }

    public void Save(Stream stream)
    {
        core.Save(stream);
    }

    /// <summary>
    ///     Saves the presentation to its original location.
    /// </summary>
    public void Save()
    {
        if (fileStream == null)
            core.Save(Identifier.PresentationPath);
        else
            core.Save(fileStream);
    }
}