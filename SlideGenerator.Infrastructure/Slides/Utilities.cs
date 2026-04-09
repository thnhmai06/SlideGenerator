using System.Xml;
using DocumentFormat.OpenXml;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Infrastructure.Slides;

public static class Utilities
{
    public const int EmuPerPixel = 9525;
    
    extension(PresentationExtension? extension)
    {
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

    public static Dictionary<string, string> SanitizeXmlValues(IReadOnlyDictionary<string, string> replacements)
    {
        return replacements.ToDictionary(kvp => kvp.Key, kvp => SanitizeXmlValue(kvp.Value));
    }

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