using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Drawing;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Slide.Entities.Shape;
using SlideGenerator.Infrastructure.Slide.Adapters;
using Stubble.Core;
using Stubble.Core.Builders;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Services;

public partial class MustacheReplacer : ITextReplacer
{
    private readonly StubbleVisitorRenderer _renderer = new StubbleBuilder()
        .Configure(settings => settings.SetEncodingFunction(value => value))
        .Build();

    private static readonly Regex MustachePattern = MustacheRegex();

    public IEnumerable<string> Scan(IReadOnlyShape sample)
    {
        return
            !string.IsNullOrWhiteSpace(sample.TextContent)
                ? ExtractKeys(sample.TextContent!)
                : [];
    }

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

    private static IEnumerable<string> ExtractKeys(string text)
    {
        return MustachePattern.Matches(text)
            .Where(m => m.Groups.Count > 1)
            .Select(m => NormalizeKey(m.Groups[1].Value))
            .Where(key => !string.IsNullOrWhiteSpace(key) && !key.StartsWith('!') && !key.StartsWith('>'))
            .Distinct();
    }

    private static string NormalizeKey(string key)
    {
        return key.Trim().TrimStart('#', '/', '^', '&', '>');
    }


    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex MustacheRegex();
}