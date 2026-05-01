/* LEGACY-OPENXML — replaced by SfTextComposer (Syncfusion.Presentation.NET)
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Drawing;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Infrastructure.Slides.Adapters;
using Stubble.Core;
using Stubble.Core.Builders;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slides.Services;

public partial class MustacheComposer : ITextComposer
{
    private static readonly Regex MustachePattern = MustacheRegex();

    private readonly StubbleVisitorRenderer _renderer = new StubbleBuilder()
        .Configure(settings => settings.SetEncodingFunction(value => value))
        .Build();

    public IEnumerable<string> Scan(IReadOnlyShape shape)
    {
        return
            !string.IsNullOrWhiteSpace(shape.TextContent)
                ? ExtractKeys(shape.TextContent!)
                : [];
    }

    public int Replace(IShape shape, IReadOnlyDictionary<string, string> instructions)
    {
        if (shape is not XmlShape xmlShape)
            throw new ArgumentException("Shape is not supported.", nameof(shape));
        if (xmlShape.Core is not Shape coreXmlShape)
            return 0;
        if (string.IsNullOrWhiteSpace(xmlShape.TextContent) || instructions.Count == 0)
            return 0;

        var replacements = Utilities.SanitizeXmlValues(instructions);
        var changed = 0;

        foreach (var paragraph in coreXmlShape.TextBody?.Descendants<Paragraph>() ?? [])
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

    private static string NormalizeKey(string key)
    {
        return key.Trim().TrimStart('#', '/', '^', '&', '>');
    }

    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex MustacheRegex();
}
*/
