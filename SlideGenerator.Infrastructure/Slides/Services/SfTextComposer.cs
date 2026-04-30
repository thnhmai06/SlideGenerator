using System.Text;
using System.Text.RegularExpressions;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using Stubble.Core;
using Stubble.Core.Builders;

namespace SlideGenerator.Infrastructure.Slides.Services;

/// <summary>
///     Replaces mustache-style placeholders in Syncfusion shapes using the Stubble library.
///     The scanning and rendering logic is identical to the legacy implementation;
///     only the text-run manipulation uses Syncfusion APIs.
/// </summary>
public sealed partial class SfTextComposer : ITextComposer
{
    private static readonly Regex MustachePattern = MustacheRegex();

    private readonly StubbleVisitorRenderer _renderer = new StubbleBuilder()
        .Configure(settings => settings.SetEncodingFunction(value => value))
        .Build();

    /// <inheritdoc />
    public IEnumerable<string> Scan(IReadOnlyShape shape)
    {
        return !string.IsNullOrWhiteSpace(shape.TextContent)
            ? ExtractKeys(shape.TextContent!)
            : [];
    }

    /// <inheritdoc />
    public int Replace(IShape shape, IReadOnlyDictionary<string, string> instructions)
    {
        if (shape is not SfShape sfShape)
            throw new ArgumentException("Shape is not supported.", nameof(shape));

        var textBody = sfShape.Core.TextBody;
        if (textBody == null || string.IsNullOrWhiteSpace(sfShape.TextContent) || instructions.Count == 0)
            return 0;

        var replacements = Utilities.SanitizeXmlValues(instructions);
        var changed = 0;

        foreach (var paragraph in textBody.Paragraphs)
        {
            var parts = paragraph.TextParts;
            if (parts.Count == 0)
                continue;

            // Collect full paragraph text across all parts
            var builder = new StringBuilder();
            foreach (var part in parts)
                builder.Append(part.Text ?? string.Empty);

            var originalText = builder.ToString();
            var renderedText = RenderText(originalText, replacements);
            if (renderedText == originalText)
                continue;

            changed += ExtractKeys(originalText).Count(instructions.ContainsKey);

            // Write the full replaced text into the first part, clear the rest
            parts[0].Text = renderedText;
            for (var i = 1; i < parts.Count; i++)
                parts[i].Text = string.Empty;
        }

        return changed;
    }

    private string RenderText(string text, Dictionary<string, string> instructions)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal))
            return text;

        try
        {
            return _renderer.Render(text, instructions);
        }
        catch
        {
            return MustachePattern.Replace(text, match =>
            {
                if (match.Groups.Count < 2)
                    return match.Value;

                var key = NormalizeKey(match.Groups[1].Value);
                return instructions.TryGetValue(key, out var value) ? value : match.Value;
            });
        }
    }

    private static IEnumerable<string> ExtractKeys(string text)
    {
        return MustachePattern.Matches(text)
            .Where(m => m.Groups.Count > 1)
            .Select(m => NormalizeKey(m.Groups[1].Value))
            .Where(key => !string.IsNullOrWhiteSpace(key) && !key.StartsWith('!') && !key.StartsWith('>'))
            .Distinct();
    }

    private static string NormalizeKey(string key) =>
        key.Trim().TrimStart('#', '/', '^', '&', '>');

    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex MustacheRegex();
}
