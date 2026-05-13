/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: MustacheEngine.cs
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
using System.Text.Json;
using System.Text.RegularExpressions;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Logging.Domain.Abstractions;
using Stubble.Core;
using Stubble.Core.Builders;

namespace SlideGenerator.Document.Infrastructure.Services;

/// <summary>
///     Replaces mustache-style placeholders in text using the Stubble template engine
///     with intelligent context-aware data parsing.
/// </summary>
/// <remarks>
///     <para>Supported Template Syntax:</para>
///     <list type="bullet">
///         <item>Simple replacement: {{key}}</item>
///         <item>Raw output (no encoding): {{{key}}} or {{&amp;key}}</item>
///         <item>Sections: {{#key}}...{{/key}}</item>
///         <item>Inverted sections: {{^key}}...{{/key}}</item>
///         <item>Partials: {{>key}}</item>
///         <item>Comments: {{!comment}}</item>
///     </list>
///     <para>Data types are intelligently parsed from string values:</para>
///     <list type="bullet">
///         <item>Excel arrays: {v1, v2, v3} → List of strings</item>
///         <item>JSON objects: {{...}} with colons → Parsed JSON structure</item>
///         <item>Comma-separated (complex keys only): v1,v2,v3 → List of strings</item>
///     </list>
/// </remarks>
public sealed partial class MustacheEngine(ISystemLogger logger) : ITemplateEngine
{
    private static readonly StubbleVisitorRenderer Renderer = new StubbleBuilder()
        .Configure(settings => settings.SetEncodingFunction(value => value))
        .Build();

    // Regex to find mustache tags: {{key}}, {{{key}}}, {{#key}}, {{^key}}, {{/key}}, {{>key}}
    private static readonly Regex TagRegex = TagPattern();

    /// <inheritdoc />
    public HashSet<string> ScanPlaceholders(string templateText)
    {
        if (string.IsNullOrWhiteSpace(templateText)) return [];

        logger.Debug("Scanning template text for mustache placeholders");

        var matches = TagRegex.Matches(templateText);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            var rawKey = match.Groups[1].Value.Trim();
            if (rawKey.StartsWith('!') || rawKey.StartsWith('/')) continue;

            // Normalize key: handles #, ^, &, >, and nested property access like key.prop
            var key = rawKey.TrimStart('#', '^', '&', '>', ' ').Split('.')[0];
            if (!string.IsNullOrWhiteSpace(key)) keys.Add(key);
        }

        if (keys.Count > 0)
            logger.Debug("Found {Count} unique placeholders in template text: {Keys}",
                keys.Count, string.Join(", ", keys));

        return keys;
    }

    /// <inheritdoc />
    public string Render(string templateText, IReadOnlyDictionary<string, string> resolvedValue)
    {
        if (string.IsNullOrWhiteSpace(templateText) || resolvedValue.Count == 0)
            return templateText;

        logger.Debug("Attempting text replacement with {Count} values", resolvedValue.Count);

        // 1. Identify keys used in complex contexts (sections, property access)
        var complexKeys = GetComplexKeys(templateText);

        // 2. Smart Parse instructions into a data dictionary
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in resolvedValue)
        {
            var isComplex = complexKeys.Contains(kvp.Key);
            data[kvp.Key] = SmartParse(kvp.Value, isComplex);
        }

        // 3. Render the template
        var rendered = Renderer.Render(templateText, data);

        logger.Debug(rendered == templateText
            ? "Rendering produced no changes"
            : "Successfully rendered template text");

        return rendered;
    }

    /// <summary>
    ///     Identifies keys used in complex Mustache contexts (sections or property access).
    /// </summary>
    /// <remarks>
    ///     Complex keys are those used with:
    ///     - Section tags: {{#key}} or {{^key}}
    ///     - Property access: {{obj.prop}}
    ///     These keys need to be parsed into objects or arrays rather than simple strings.
    /// </remarks>
    private static HashSet<string> GetComplexKeys(string template)
    {
        var complexKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = TagRegex.Matches(template);
        foreach (Match match in matches)
        {
            var raw = match.Groups[1].Value.Trim();
            if (raw.StartsWith('#') || raw.StartsWith('^'))
                complexKeys.Add(raw.TrimStart('#', '^', ' ').Split('.')[0]);
            else if (raw.Contains('.'))
                complexKeys.Add(raw.Split('.')[0]);
        }

        return complexKeys;
    }

    /// <summary>
    ///     Intelligently parses a string value into appropriate CLR type based on format and context.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="isComplex">Whether this value is used in complex contexts (affects parsing strategy).</param>
    /// <returns>
    ///     Parsed value as the appropriate type:
    ///     - Excel array ({v1, v2}) → List of string
    ///     - JSON object or array → Dictionary of string, object or List of object
    ///     - Comma-separated (complex only) → List of string
    ///     - Other → Original string value
    /// </returns>
    private static object SmartParse(string value, bool isComplex)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var trimmed = value.Trim();

        // 1. Excel Array Check: {v1, v2} and NO :
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}') && trimmed.Contains(',') && !trimmed.Contains(':'))
        {
            var inner = trimmed.Substring(1, trimmed.Length - 2);
            return inner.Split(',')
                .Select(s => s.Trim().Trim('"'))
                .ToList();
        }

        // 2. JSON Check: {} with : or []
        if ((trimmed.StartsWith('{') && trimmed.Contains(':')) || trimmed.StartsWith('['))
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return JsonToNative(doc.RootElement);
            }
            catch
            {
                // Not valid JSON, fall through
            }

        // 3. Comma-Separated Array Check (Context-dependent)
        if (isComplex && trimmed.Contains(',') && !trimmed.Contains(':'))
            return trimmed.Split(',')
                .Select(s => s.Trim())
                .ToList();

        return value;
    }

    /// <summary>
    ///     Converts a JsonElement recursively into native CLR types.
    /// </summary>
    private static object JsonToNative(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in element.EnumerateObject())
                    dict[prop.Name] = JsonToNative(prop.Value);
                return dict;
            case JsonValueKind.Array:
                var list = element.EnumerateArray().Select(JsonToNative).ToList();
                return list;
            case JsonValueKind.String: return element.GetString() ?? string.Empty;
            case JsonValueKind.Number: return element.GetRawText();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return string.Empty;
            case JsonValueKind.Undefined:
            default: return element.GetRawText();
        }
    }

    [GeneratedRegex(@"\{\{\{?([#\^/&!>]?\s*[\w\.\-]+)\s*\}?\}\}", RegexOptions.Compiled)]
    private static partial Regex TagPattern();
}






