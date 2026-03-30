using DocumentFormat.OpenXml;
using SlideGenerator.Domain.Slide.Rules;
using Spire.Presentation;

namespace SlideGenerator.Infrastructure.Slide;

public static class Utilities
{
    public const int EmuPerPixel = 9525;
    
    extension(PresentationExtension? extension)
    {
        public FileFormat ToSpireDocType()
        {
            return extension switch
            {
                null => FileFormat.Auto,
                PresentationExtension.Pptx => FileFormat.Pptx2019,
                PresentationExtension.Ppsx => FileFormat.Ppsx2019,
                PresentationExtension.Potx => FileFormat.Potx,
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }

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