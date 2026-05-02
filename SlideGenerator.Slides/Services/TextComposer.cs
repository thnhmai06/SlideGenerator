using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Stubble.Core;
using Stubble.Core.Builders;
using Syncfusion.Presentation;

namespace SlideGenerator.Slides.Services;

/// <summary>
///     Replaces mustache-style placeholders in Syncfusion presentation shapes using Stubble template engine
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
public static partial class TextComposer
{
    private static readonly StubbleVisitorRenderer Renderer = new StubbleBuilder()
        .Configure(settings => settings.SetEncodingFunction(value => value))
        .Build();

    // Regex to find mustache tags: {{key}}, {{{key}}}, {{#key}}, {{^key}}, {{/key}}, {{>key}}
    private static readonly Regex TagRegex = TagPattern();

    /// <summary>
    ///     Extracts the unique mustache placeholder keys from the specified shape's text content.
    /// </summary>
    /// <param name="shape">The Syncfusion shape to scan for placeholders.</param>
    /// <returns>
    ///     An enumerable of unique placeholder keys (case-insensitive comparison).
    ///     Returns empty collection if shape has no text body or empty text.
    /// </returns>
    /// <remarks>
    ///     <para>This method:</para>
    ///     <list type="bullet">
    ///         <item>Ignores comment tags and closing tags</item>
    ///         <item>Normalizes special characters (#, ^, &amp;, &gt;)</item>
    ///         <item>Handles nested property access (key.property) by extracting the root key</item>
    ///         <item>Uses case-insensitive key comparison</item>
    ///     </list>
    /// </remarks>
    public static IEnumerable<string> Scan(IShape shape)
    {
        var text = shape.TextBody?.Text;
        if (string.IsNullOrWhiteSpace(text)) return [];

        var matches = TagRegex.Matches(text);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            var rawKey = match.Groups[1].Value.Trim();
            if (rawKey.StartsWith('!') || rawKey.StartsWith('/')) continue;

            // Normalize key: handles #, ^, &, >, and nested property access like key.prop
            var key = rawKey.TrimStart('#', '^', '&', '>', ' ').Split('.')[0];
            if (!string.IsNullOrWhiteSpace(key)) keys.Add(key);
        }

        return keys;
    }

    /// <summary>
    ///     Replaces mustache placeholders in the specified shape using provided instruction data.
    /// </summary>
    /// <param name="shape">The Syncfusion shape containing mustache template text.</param>
    /// <param name="instructions">
    ///     Dictionary of key-value pairs where keys match placeholder names
    ///     and values are intelligently parsed based on context.
    /// </param>
    /// <returns>
    ///     The number of placeholder keys from instructions that were found and replaced in the shape.
    ///     Returns 0 if no text body exists or instructions are empty.
    /// </returns>
    /// <remarks>
    ///     <para>This method:</para>
    ///     <list type="bullet">
    ///         <item>Builds a complete template from all paragraph text parts</item>
    ///         <item>Identifies complex keys (used in sections or property access)</item>
    ///         <item>Smart-parses values based on context (arrays, JSON, primitives)</item>
    ///         <item>Renders using Stubble template engine</item>
    ///         <item>Updates shape text only if rendering produces different output</item>
    ///         <item>Consolidates text into first paragraph/part to avoid duplication</item>
    ///     </list>
    /// </remarks>
    public static int Replace(IShape shape, IReadOnlyDictionary<string, string> instructions)
    {
        if (shape.TextBody == null || instructions.Count == 0)
            return 0;

        // 1. Build the full template from all paragraphs and text parts
        var templateBuilder = new StringBuilder();
        foreach (var paragraph in shape.TextBody.Paragraphs)
        foreach (var part in paragraph.TextParts)
            templateBuilder.Append(part.Text);

        var template = templateBuilder.ToString();
        if (string.IsNullOrWhiteSpace(template)) return 0;

        // 2. Identify keys used in complex contexts (sections, property access)
        var complexKeys = GetComplexKeys(template);

        // 3. Smart Parse instructions into a data dictionary
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in instructions)
        {
            var isComplex = complexKeys.Contains(kvp.Key);
            data[kvp.Key] = SmartParse(kvp.Value, isComplex);
        }

        // 4. Render the template
        var rendered = Renderer.Render(template, data);
        if (rendered == template) return 0;

        // 5. Update the shape's text content
        UpdateShapeText(shape, rendered);

        // Return the number of keys found in the original template that were in instructions
        return instructions.Keys.Count(k => template.Contains("{{" + k, StringComparison.OrdinalIgnoreCase)
                                            || template.Contains("{{#" + k, StringComparison.OrdinalIgnoreCase)
                                            || template.Contains("{{^" + k, StringComparison.OrdinalIgnoreCase));
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
    ///     Parsed value as appropriate type:
    ///     - Excel array ({v1, v2}) → List&lt;string&gt;
    ///     - JSON object or array → Dictionary&lt;string, object&gt; or List&lt;object&gt;
    ///     - Comma-separated (complex only) → List&lt;string&gt;
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
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return JsonToNative(doc.RootElement);
            }
            catch
            {
                // Not valid JSON, fall through
            }
        }

        // 3. Comma-Separated Array Check (Context-dependent)
        if (isComplex && trimmed.Contains(',') && !trimmed.Contains(':'))
        {
            return trimmed.Split(',')
                .Select(s => s.Trim())
                .ToList();
        }

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

    /// <summary>
    ///     Updates shape text by consolidating rendered content into the first paragraph/part
    ///     and clearing remaining text to avoid duplication.
    /// </summary>
    private static void UpdateShapeText(IShape shape, string newText)
    {
        var textBody = shape.TextBody;
        if (textBody.Paragraphs.Count == 0)
            textBody.AddParagraph();

        var firstPara = textBody.Paragraphs[0];
        if (firstPara.TextParts.Count == 0)
            firstPara.AddTextPart();

        // Set the full rendered text to the first part of the first paragraph
        firstPara.TextParts[0].Text = newText;

        // Clear all other text parts in the first paragraph
        for (var i = 1; i < firstPara.TextParts.Count; i++)
            firstPara.TextParts[i].Text = string.Empty;

        // Clear all other paragraphs to avoid duplication
        for (var i = 1; i < textBody.Paragraphs.Count; i++)
        {
            foreach (var part in textBody.Paragraphs[i].TextParts)
                part.Text = string.Empty;
        }
    }

    [GeneratedRegex(@"\{\{\{?([#\^/&!>]?\s*[\w\.\-]+)\s*\}?\}\}", RegexOptions.Compiled)]
    private static partial Regex TagPattern();
}