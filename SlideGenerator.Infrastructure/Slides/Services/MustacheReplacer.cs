using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Drawing;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using Stubble.Core;
using Stubble.Core.Builders;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
/// Replaces mustache-style placeholders in Open XML shapes using the Stubble library.
/// </summary>
public partial class MustacheReplacer : ITextReplacer
{
    /// <summary>
    /// Regular expression pattern for matching mustache placeholders.
    /// </summary>
    private static readonly Regex MustachePattern = MustacheRegex();

    /// <summary>
    /// The Stubble renderer used for processing templates.
    /// </summary>
    private readonly StubbleVisitorRenderer _renderer = new StubbleBuilder()
        .Configure(settings => settings.SetEncodingFunction(value => value))
        .Build();

    /// <summary>
    /// Scans a shape for mustache-style keys.
    /// </summary>
    /// <param name="sample">The shape to scan.</param>
    /// <returns>An enumerable collection of keys found in the shape's text content.</returns>
    public IEnumerable<string> Scan(IReadOnlyShape sample)
    {
        return
            !string.IsNullOrWhiteSpace(sample.TextContent)
                ? ExtractKeys(sample.TextContent!)
                : [];
    }

    /// <summary>
    /// Replaces placeholders in a shape with values provided in the instructions.
    /// </summary>
    /// <param name="sample">The shape to update.</param>
    /// <param name="instructions">A dictionary of keys and their replacement values.</param>
    /// <returns>The number of placeholders successfully replaced.</returns>
    /// <exception cref="ArgumentException">Thrown when the shape is not supported.</exception>
    public int Replace(IShape sample, IReadOnlyDictionary<string, string> instructions)
    {
        if (sample is not XmlShape xmlShape)
            throw new ArgumentException("Shape is not supported.", nameof(sample));
        if (xmlShape.Core is not Shape shape)
            return 0;
        if (string.IsNullOrWhiteSpace(xmlShape.TextContent) || instructions.Count == 0)
            return 0;

        var replacements = Utilities.SanitizeXmlValues(instructions);
        var changed = 0;

        foreach (var paragraph in shape.TextBody?.Descendants<Paragraph>() ?? [])
        {
            var runs = paragraph.Descendants<Run>().ToList();
            if (runs.Count == 0)
                continue;

            var builder = new StringBuilder();
            foreach (var run in runs)
                builder.Append(run.Text?.Text ?? string.Empty);
            var originalText = builder.ToString();
            var renderedText = RenderText(originalText, replacements);
            if (renderedText == originalText)
                continue;

            changed += ExtractKeys(originalText).Count(instructions.ContainsKey);

            runs[0].Text ??= new Text();
            runs[0].Text!.Text = renderedText;

            for (var index = 1; index < runs.Count; index++)
                if (runs[index].Text != null)
                    runs[index].Text!.Text = string.Empty;
        }

        return changed;
    }

    /// <summary>
    /// Renders the text by replacing mustache placeholders with values.
    /// </summary>
    /// <param name="text">The template text.</param>
    /// <param name="instructions">The replacement values.</param>
    /// <returns>The rendered text.</returns>
    private string RenderText(string text, Dictionary<string, string> instructions)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal))
            return text;

        try
        {
            // Stubble
            return _renderer.Render(text, instructions);
        }
        catch
        {
            // Regex (fallback)
            return MustachePattern.Replace(text, match =>
            {
                if (match.Groups.Count < 2)
                    return match.Value;

                var key = NormalizeKey(match.Groups[1].Value);
                return instructions.TryGetValue(key, out var value) ? value : match.Value;
            });
        }
    }

    /// <summary>
    /// Extracts mustache keys from a given text string.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>An enumerable collection of unique keys.</returns>
    private static IEnumerable<string> ExtractKeys(string text)
    {
        return MustachePattern.Matches(text)
            .Where(m => m.Groups.Count > 1)
            .Select(m => NormalizeKey(m.Groups[1].Value))
            .Where(key => !string.IsNullOrWhiteSpace(key) && !key.StartsWith('!') && !key.StartsWith('>'))
            .Distinct();
    }

    /// <summary>
    /// Normalizes a mustache key by trimming whitespace and removing special characters.
    /// </summary>
    /// <param name="key">The key to normalize.</param>
    /// <returns>The normalized key.</returns>
    private static string NormalizeKey(string key)
    {
        return key.Trim().TrimStart('#', '/', '^', '&', '>');
    }


    /// <summary>
    /// Gets the compiled regular expression for mustache placeholders.
    /// </summary>
    /// <returns>A <see cref="Regex"/> object.</returns>
    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex MustacheRegex();
}