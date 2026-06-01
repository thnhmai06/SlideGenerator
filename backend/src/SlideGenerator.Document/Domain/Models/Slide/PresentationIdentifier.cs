/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: PresentationIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Models.Slide;

/// <summary>
///     Uniquely identifies a PowerPoint presentation file.
/// </summary>
/// <param name="PresentationPath">The absolute or relative path to the presentation.</param>
/// <param name="PresentationPassword">Optional password if the presentation is encrypted.</param>
public record PresentationIdentifier(string PresentationPath, string? PresentationPassword = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the presentation.
    /// </summary>
    public string PresentationPath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = PresentationPath;

    /// <summary>
    ///     Determines the type of the presentation based on its file extension.
    /// </summary>
    /// <returns>The <see cref="PresentationType" /> corresponding to the file extension.</returns>
    public PresentationType GetPresentationType()
    {
        return PresentationTypeExtensions.FromExtension(Path.GetExtension(PresentationPath));
    }
}