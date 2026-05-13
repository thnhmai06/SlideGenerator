/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: GeneratingRequest.cs
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
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generating.Domain.Models.Dto;

namespace SlideGenerator.Generating.Domain.Models;

/// <summary>
///     Represents the user-provided request to start a slide generation process.
/// </summary>
/// <param name="Recipe">The mapping recipe defining data sources and targets.</param>
/// <param name="Name">The display name of the generation job.</param>
/// <param name="OutputType">The desired file extension for the output presentations.</param>
/// <param name="SaveFolder">The root directory where generated presentations will be saved.</param>
/// <param name="DeleteDownloadImage">True to delete raw downloaded images after processing.</param>
/// <param name="DeleteEditImage">True to delete cropped/resized images after they are embedded in slides.</param>
public sealed record GeneratingRequest(
    Recipe Recipe,
    string Name,
    PresentationType OutputType,
    string SaveFolder,
    bool DeleteDownloadImage = false,
    bool DeleteEditImage = true)
{
    /// <summary>
    ///     Gets the validated save folder path.
    /// </summary>
    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;

    /// <summary>
    ///     Gets the runtime file path for user-facing workflow logs.
    /// </summary>
    public string? WorkflowLogFilePath { get; init; }
}





