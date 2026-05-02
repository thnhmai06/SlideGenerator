// using System.Text;
// using Syncfusion.Presentation;
// using Stubble.Core;
// using Stubble.Core.Builders;
//
// namespace SlideGenerator.Slides.Services;
//
// /// <summary>
// ///     Replaces mustache-style placeholders in Syncfusion shapes using the Stubble library.
// /// </summary>
// public sealed class SfTextComposer
// {
//     private readonly StubbleVisitorRenderer _renderer = new StubbleBuilder()
//         .Configure(settings => settings.SetEncodingFunction(value => value))
//         .Build();
//
//     /// <summary>
//     ///     Extracts the unique mustache keys from the specified shape's text content.
//     /// </summary>
//     public static IEnumerable<string> Scan(IShape shape)
//     {
//         var text = shape.TextBody?.Text;
//         return !string.IsNullOrWhiteSpace(text) ? ExtractKeys(text) : [];
//     }
//
//     /// <summary>
//     ///     Replaces placeholders in the specified shape using the provided instructions.
//     /// </summary>
//     public int Replace(IShape shape, IReadOnlyDictionary<string, string> instructions)
//     {
//         var textBody = shape.TextBody;
//         if (textBody == null || string.IsNullOrWhiteSpace(textBody.Text) || instructions.Count == 0)
//             return 0;
//
//         var replacements = SanitizeXmlValues(instructions);
//         var changed = 0;
//
//         foreach (var paragraph in textBody.Paragraphs)
//         {
//             var parts = paragraph.TextParts;
//             if (parts.Count == 0)
//                 continue;
//
//             var builder = new StringBuilder();
//             foreach (var part in parts)
//                 builder.Append(part.Text ?? string.Empty);
//
//             var originalText = builder.ToString();
//             var renderedText = RenderText(originalText, replacements);
//             if (renderedText == originalText)
//                 continue;
//
//             changed += ExtractKeys(originalText).Count(instructions.ContainsKey);
//
//             parts[0].Text = renderedText;
//             for (var i = 1; i < parts.Count; i++)
//                 parts[i].Text = string.Empty;
//         }
//
//         return changed;
//     }
//
//     private string RenderText(string text, Dictionary<string, string> instructions)
//     {
//         if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal)) return text;
//         return _renderer.Render(text, instructions);
//     }
//
//     private static HashSet<string> ExtractKeys(string text)
//     {
//         if (string.IsNullOrEmpty(text)) return [];
//
//         var keys = new HashSet<string>();
//         var currentIndex = 0;
//         int startIndex;
//
//         while ((startIndex = text.IndexOf("{{", currentIndex, StringComparison.Ordinal)) != -1)
//         {
//             var keyStart = startIndex + 2;
//             var endIndex = text.IndexOf("}}", keyStart, StringComparison.Ordinal);
//
//             if (endIndex != -1)
//             {
//                 var rawKey = text.Substring(keyStart, endIndex - keyStart);
//                 var key = NormalizeKey(rawKey);
//
//                 if (!string.IsNullOrWhiteSpace(key) && !key.StartsWith('!') && !key.StartsWith('>'))
//                 {
//                     keys.Add(key);
//                 }
//
//                 currentIndex = endIndex + 2;
//             }
//             else break; // has no closing "}}"
//         }
//
//         return keys;
//     }
//
//     private static string NormalizeKey(string key) => key.Trim().TrimStart('#', '/', '^', '&', '>');
//
//     private static Dictionary<string, string> SanitizeXmlValues(IReadOnlyDictionary<string, string> values)
//     {
//         return values.ToDictionary(
//             kvp => kvp.Key,
//             kvp => System.Security.SecurityElement.Escape(kvp.Value));
//     }
// }