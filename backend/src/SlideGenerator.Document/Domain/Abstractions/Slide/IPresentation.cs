/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IPresentation.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

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