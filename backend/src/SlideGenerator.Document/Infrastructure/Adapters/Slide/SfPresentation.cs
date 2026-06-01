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

using SlideGenerator.Document.Domain.Models.Slide;
using Syncfusion.Presentation;
using ISlide = SlideGenerator.Document.Domain.Abstractions.Slide.ISlide;

namespace SlideGenerator.Document.Infrastructure.Adapters.Slide;

/// <summary>
///     Wraps a Syncfusion IPresentation and its FileStream for proper disposal and saving.
/// </summary>
internal sealed class SfPresentation(
    IPresentation core,
    PresentationIdentifier identifier,
    FileStream? fileStream = null) : Domain.Abstractions.Slide.IPresentation
{
    public IEnumerable<ISlide> Slides
    {
        get { return core.Slides.Select(slide => new SfSlide(slide)); }
    }

    public int SlidesCount => core.Slides.Count;

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

    public void CloneSlide(int slideIndex)
    {
        core.Slides.Add(core.Slides[slideIndex].Clone());
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
            core.Save(identifier.PresentationPath);
        else
            core.Save(fileStream);
    }
}