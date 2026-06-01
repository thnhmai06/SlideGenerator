/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingRequest.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Generator.Domain.Models;

/// <summary>
///     Represents the user-provided request to start a slide generation process.
/// </summary>
/// <param name="RecipeId">The database id of the recipe to use.</param>
/// <param name="Name">The display name of the generation job.</param>
/// <param name="OutputType">The desired file extension for the output presentations.</param>
/// <param name="SaveFolder">The root directory where generated presentations will be saved.</param>
/// <param name="DownloadAssetsPath">
///     Custom directory for raw downloaded images.
///     When <see langword="null" />, images are written to the default assets folder and deleted once the edit step
///     completes.
///     When set, images are written to the specified path and kept after processing.
/// </param>
/// <param name="EditAssetsPath">
///     Custom directory for cropped/resized images.
///     When <see langword="null" />, images are written to the default assets folder and deleted once they are embedded in
///     slides.
///     When set, images are written to the specified path and kept after processing.
/// </param>
public sealed record GeneratingRequest(
    int RecipeId,
    string Name,
    PresentationType OutputType,
    string SaveFolder,
    string? DownloadAssetsPath = null,
    string? EditAssetsPath = null,
    bool AllowLocalImagePaths = false)
{
    /// <summary>
    ///     Gets the validated save folder path.
    /// </summary>
    public string SaveFolder { get; init; } = string.IsNullOrWhiteSpace(SaveFolder)
        ? throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder))
        : SaveFolder;

    /// <summary>
    ///     Gets the optional path where assets are downloaded during the generation process.
    /// </summary>
    public string? DownloadAssetsPath { get; init; } = !string.IsNullOrWhiteSpace(DownloadAssetsPath)
        ? DownloadAssetsPath
        : null;

    /// <summary>
    ///     Gets the optional path where assets are edited during the generation process.
    /// </summary>
    public string? EditAssetsPath { get; init; } = !string.IsNullOrWhiteSpace(EditAssetsPath)
        ? EditAssetsPath
        : null;

    /// <summary>
    ///     Gets whether local file paths are accepted as image sources.
    ///     When <see langword="true" />, a cell value that matches an existing local file bypasses HTTP download
    ///     and is hard-linked (or copied) to the destination instead.
    /// </summary>
    public bool AllowLocalImagePaths { get; init; } = AllowLocalImagePaths;
}