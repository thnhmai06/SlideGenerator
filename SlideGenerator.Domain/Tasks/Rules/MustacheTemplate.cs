using System.Text.RegularExpressions;

namespace SlideGenerator.Domain.Tasks.Rules;

public static partial class MustacheTemplate
{
    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")] // {{ placeholder }}
    private static partial Regex Pattern();
    
    /// <summary>
    ///     Scans text for Mustache template placeholders.
    /// </summary>
    /// <returns>A set of placeholder names found in the text.</returns>
    public static HashSet<string> Scan(string text)
    {
        HashSet<string> templates = [];
        var matches = Pattern().Matches(text);
        foreach (Match match in matches)
            if (match.Groups.Count > 1)
                templates.Add(match.Groups[1].Value);
        return templates;
    }
}