/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: PresentationIdentifier.cs
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
