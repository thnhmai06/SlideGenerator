/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: Presentation.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Presentations.Identifiers;
using Syncfusion.Presentation;
using SyncfusionPresentation = Syncfusion.Presentation.IPresentation;

namespace SlideGenerator.Document.Presentations.Components;

/// <summary>
///     Represents a read-only view of a PowerPoint presentation.
/// </summary>
public interface IReadOnlyPresentation : IDisposable
{
    /// <summary>
    ///     Gets the collection of slides in the presentation.
    /// </summary>
    IEnumerable<IReadOnlySlide> Slides { get; }

    /// <summary>
    ///     Gets the identifier of presentation.
    /// </summary>
    PresentationIdentifier Identifier { get; }

    /// <summary>
    ///     Gets the total number of slides in the presentation.
    /// </summary>
    int SlidesCount { get; }

    /// <summary>
    ///     Gets a value indicating whether the presentation has write protection enabled.
    /// </summary>
    bool IsWriteProtected { get; }
}

/// <summary>
///     Represents a PowerPoint presentation that can be modified and saved.
/// </summary>
public interface IPresentation : IReadOnlyPresentation
{
    /// <summary>
    ///     Gets the collection of slides in the presentation.
    /// </summary>
    new IEnumerable<ISlide> Slides { get; }

    /// <inheritdoc />
    IEnumerable<IReadOnlySlide> IReadOnlyPresentation.Slides => Slides;

    /// <summary>
    ///     Removes the slide at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the slide to remove.</param>
    void RemoveSlideAt(int index);

    /// <summary>
    ///     Appends <paramref name="slide" /> to the end of this presentation, preserving source formatting.
    ///     <paramref name="slide" /> must be a detached clone obtained via <see cref="IReadOnlySlide.Clone" />.
    /// </summary>
    /// <param name="slide">Detached slide clone to insert.</param>
    void AddSlide(IReadOnlySlide slide);

    /// <summary>
    ///     Removes encryption from the presentation.
    /// </summary>
    void RemoveEncryption();

    /// <summary>
    ///     Removes write protection from the presentation.
    /// </summary>
    void RemoveWriteProtection();

    /// <summary>
    ///     Saves the changes made to the presentation.
    /// </summary>
    void Save();

    /// <summary>
    ///     Saves the presentation to the specified file path.
    /// </summary>
    /// <param name="path">The file path to save to.</param>
    void Save(string path);

    /// <summary>
    ///     Saves the presentation to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    void Save(Stream stream);
}

/// <summary>
///     Wraps a Syncfusion IPresentation and its FileStream for proper disposal and saving.
/// </summary>
internal sealed class SfPresentation(
    SyncfusionPresentation core,
    PresentationIdentifier identifier,
    FileStream? fileStream = null) : IPresentation
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
