/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Request.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Models.Slide;

namespace SlideGenerator.Generator.Domain.Models.Data;

/// <summary>
///     Represents the user-provided request to start a slide generation process.
/// </summary>
/// <param name="RecipeId">The database id of the recipe to use.</param>
/// <param name="Name">The display name of the generation job.</param>
/// <param name="OutputType">The desired file extension for the output presentations.</param>
/// <param name="SaveFolder">The root directory where generated presentations will be saved.</param>
/// <param name="AllowLocalPaths">
///     Gets whether local file paths are accepted.
///     When <see langword="true" />, a cell value that matches an existing local file bypasses download
///     and is hard-linked (or copied) to the destination instead.
/// </param>
public sealed record Request(
    int RecipeId,
    string Name,
    PresentationType OutputType,
    string SaveFolder,
    bool AllowLocalPaths = false)
{
    /// <summary>
    ///     Gets the validated save folder path.
    /// </summary>
    public string SaveFolder { get; init; } = !string.IsNullOrWhiteSpace(SaveFolder)
        ? Path.GetFullPath(SaveFolder)
        : throw new ArgumentException("Save folder cannot be null or whitespace.", nameof(SaveFolder));
}
