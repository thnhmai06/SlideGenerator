using System.Xml;
using DocumentFormat.OpenXml;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Infrastructure.Slides;

/// <summary>
///     Provides utility methods for manipulating XML and OpenXML data within slides.
/// </summary>
public static class Utilities
{
    /// <summary>The conversion factor from English Metric Units (EMU) to pixels.</summary>
    public const int EmuPerPixel = 9525;

    /// <summary>
    ///     Sanitizes all string values within a dictionary of replacements to ensure they contain only valid XML characters.
    /// </summary>
    /// <param name="replacements">The original dictionary of replacement texts.</param>
    /// <returns>A new dictionary with sanitized values.</returns>
    public static Dictionary<string, string> SanitizeXmlValues(IReadOnlyDictionary<string, string> replacements)
    {
        return replacements.ToDictionary(kvp => kvp.Key, kvp => SanitizeXmlValue(kvp.Value));
    }

    /// <summary>
    ///     Sanitizes a string to contain only valid XML characters.
    /// </summary>
    /// <param name="value">The string to sanitize.</param>
    /// <returns>A safe string that can be serialized to XML.</returns>
    public static string SanitizeXmlValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.All(XmlConvert.IsXmlChar))
            return value;

        var buffer = new char[value.Length];
        var count = 0;
        foreach (var ch in value.Where(XmlConvert.IsXmlChar))
            buffer[count++] = ch;

        return new string(buffer, 0, count);
    }

    /// <summary>
    ///     Extension methods for <see cref="PresentationExtension" />.
    /// </summary>
    extension(PresentationExtension? extension)
    {
        /// <summary>
        ///     Converts a <see cref="PresentationExtension" /> to its corresponding OpenXML
        ///     <see cref="PresentationDocumentType" />.
        /// </summary>
        /// <returns>The OpenXML document type enum value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the extension is unknown.</exception>
        public PresentationDocumentType ToXmlDocType()
        {
            return extension switch
            {
                null => PresentationDocumentType.Presentation,
                PresentationExtension.Potx => PresentationDocumentType.Template,
                PresentationExtension.Pptx => PresentationDocumentType.Presentation,
                PresentationExtension.Ppsx => PresentationDocumentType.Slideshow,
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }
    }
}