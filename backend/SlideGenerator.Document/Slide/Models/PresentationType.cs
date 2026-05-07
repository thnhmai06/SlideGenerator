/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: PresentationType.cs
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

namespace SlideGenerator.Document.Slide.Models;

/// <summary>
///     Specifies the supported file extensions for presentations.
/// </summary>
public enum PresentationType
{
    /// <summary>PowerPoint Template (.potx)</summary>
    Potx,

    /// <summary>Standard PowerPoint Presentation (.pptx)</summary>
    Pptx,

    /// <summary>PowerPoint Slideshow (.ppsx)</summary>
    Ppsx
}

/// <summary>
///     Provides extension methods and utilities for <see cref="PresentationType" />.
/// </summary>
public static class PresentationTypeExtensions
{
    public static PresentationType FromExtension(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".potx" => PresentationType.Potx,
            ".pptx" => PresentationType.Pptx,
            ".ppsx" => PresentationType.Ppsx,
            _ => throw new ArgumentException($"Unsupported file extension: {fileExtension}", nameof(fileExtension))
        };
    }

    public static string ToExtension(this PresentationType type)
    {
        return type switch
        {
            PresentationType.Potx => ".potx",
            PresentationType.Pptx => ".pptx",
            PresentationType.Ppsx => ".ppsx",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}