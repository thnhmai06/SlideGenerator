using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using TaoSlideTotNghiep.Models.Presentations;
using Stubble.Core.Builders;

namespace TaoSlideTotNghiep.Models.Engines;

public static partial class TextReplacementEngine
{
    private const string TemplatePattern = @"\{\{([\w\d\s]+)\}\}";

    [GeneratedRegex(TemplatePattern)]
    private static partial Regex TemplateRegex();

    private static HashSet<string> ScanTextTemplate(string text)
    {
        HashSet<string> templates = [];
        var matches = TemplateRegex().Matches(text);
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                templates.Add(match.Groups[1].Value.Trim());
            }
        }
        return templates;
    }

    public static HashSet<string> ScanTextTemplate(SlidePart slidePart)
    {
        HashSet<string> templates = [];

        // PresentationText (quick)
        var allText = slidePart.Slide.InnerText;
        templates.UnionWith(ScanTextTemplate(allText));

        // DrawingText
        var drawingTexts = Presentation.GetSlideDrawingText(slidePart);
        foreach (var drawingText in drawingTexts)
        {
            var rawText = drawingText.Text;
            templates.UnionWith(ScanTextTemplate(rawText));
        }

        return templates;
    }

    public static async Task<uint> ReplaceTextTemplate(SlidePart slidePart, Dictionary<string, string> replacements)
    {
        var stubble = new StubbleBuilder().Build();
        uint replaced = 0;

        // PresentationText
        var presentationTexts = Presentation.GetSlidePresentationText(slidePart);
        foreach (var presText in presentationTexts)
        {
            var newText = await stubble.RenderAsync(presText.Text, replacements);
            if (newText != presText.Text)
            {
                presText.Text = newText;
                replaced++;
            }
        }

        // DrawingText
        var drawingTexts = Presentation.GetSlideDrawingText(slidePart);
        foreach (var drawingText in drawingTexts)
        {
            var newText = await stubble.RenderAsync(drawingText.Text, replacements);
            if (newText != drawingText.Text)
            {
                drawingText.Text = newText;
                replaced++;
            }
        }

        return replaced;
    }
}
