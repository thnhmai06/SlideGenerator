using DocumentFormat.OpenXml;

namespace SlideGenerator.Domain.Tasks.Models;

public enum OutputExtension
{
    Potx,
    Pptx
}

public static class OutputExtensionExtensions
{
    extension(OutputExtension extension)
    {
        public string ToFileExtension()
        {
            return extension switch
            {
                OutputExtension.Potx => ".potx",
                OutputExtension.Pptx => ".pptx",
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }

        public PresentationDocumentType ToPresentationDocumentType()
        {
            return extension switch
            {
                OutputExtension.Potx => PresentationDocumentType.Template,
                OutputExtension.Pptx => PresentationDocumentType.Presentation,
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }
    }
}