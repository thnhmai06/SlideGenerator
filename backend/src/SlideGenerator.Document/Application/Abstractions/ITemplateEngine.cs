/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ITemplateEngine.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Application.Abstractions;

/// <summary>
///     Represents a service for handling operations related to template-based document generation.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    ///     Identifies and extracts all unique placeholders within a given template text.
    ///     Placeholders are typically marked with specific syntax and are used to dynamically replace content in a template.
    /// </summary>
    /// <param name="templateText">The text content of the template from which to scan and extract placeholders.</param>
    /// <returns>A set of unique placeholder identifiers present within the provided template text.</returns>
    HashSet<string> ScanPlaceholders(string templateText);

    /// <summary>
    ///     Renders the provided template text by replacing placeholders with their corresponding resolved values.
    ///     The replacement values are supplied through a dictionary where the key is the placeholder name
    ///     and the value is the content to replace it with.
    /// </summary>
    /// <param name="templateText">The template text containing placeholders to be replaced.</param>
    /// <param name="resolvedValue">A dictionary containing the mappings of placeholder names to their replacement values.</param>
    /// <returns>The fully rendered text with all placeholders replaced by their respective values.</returns>
    string Render(string templateText, IReadOnlyDictionary<string, string> resolvedValue);
}