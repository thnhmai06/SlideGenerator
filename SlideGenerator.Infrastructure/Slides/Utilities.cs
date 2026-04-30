using System.Xml;

namespace SlideGenerator.Infrastructure.Slides;

/// <summary>
///     Shared utility helpers for Syncfusion-based slide processing.
/// </summary>
public static class Utilities
{
    /// <summary>The conversion factor from English Metric Units (EMU) to pixels (96 DPI).</summary>
    public const float EmuPerPixel = 9525f;

    /// <summary>
    ///     Sanitizes all string values in a replacement dictionary so they contain only valid XML characters.
    ///     Required because Syncfusion still serializes text into an OOXML stream.
    /// </summary>
    public static Dictionary<string, string> SanitizeXmlValues(IReadOnlyDictionary<string, string> replacements)
    {
        return replacements.ToDictionary(kvp => kvp.Key, kvp => SanitizeXmlValue(kvp.Value));
    }

    /// <summary>Removes characters that are illegal in XML 1.0.</summary>
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
}
