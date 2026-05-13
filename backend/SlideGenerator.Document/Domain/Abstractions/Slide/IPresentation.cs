/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IPresentation.cs
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
namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
/// Represents a PowerPoint presentation that can be modified and saved.
/// </summary>
public interface IPresentation : IReadOnlyPresentation
{
    /// <summary>
    /// Gets the collection of slides in the presentation.
    /// </summary>
    new IEnumerable<ISlide> Slides { get; }

    /// <summary>
    /// Removes the slide at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the slide to remove.</param>
    void RemoveSlideAt(int index);

    /// <summary>
    /// Clones the slide at the specified index and appends the clone to the end of the presentation.
    /// </summary>
    /// <param name="slideIndex">The 0-based index of the slide to clone.</param>
    void CloneSlide(int slideIndex);

    /// <summary>
    /// Removes encryption from the presentation.
    /// </summary>
    void RemoveEncryption();

    /// <summary>
    /// Removes write protection from the presentation.
    /// </summary>
    void RemoveWriteProtection();

    /// <summary>
    /// Saves the changes made to the presentation.
    /// </summary>
    void Save();

    /// <summary>
    /// Saves the presentation to the specified file path.
    /// </summary>
    /// <param name="path">The file path to save to.</param>
    void Save(string path);

    /// <summary>
    /// Saves the presentation to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    void Save(Stream stream);

    /// <inheritdoc />
    IEnumerable<IReadOnlySlide> IReadOnlyPresentation.Slides => Slides;
}






