namespace SlideGenerator.Domain.Slide.Rules;

public enum PresentationExtension
{
    Potx, // Template
    Pptx, // Normal
    Ppsx // Slideshow
}

public static class PresentationExtensions
{
    internal static IReadOnlySet<PresentationExtension> OutputExtensions { get; } = new HashSet<PresentationExtension>
    {
        PresentationExtension.Pptx,
        PresentationExtension.Ppsx
    };

    extension(PresentationExtension extension)
    {
        public string ToFileExtension()
        {
            return extension switch
            {
                PresentationExtension.Potx => ".potx",
                PresentationExtension.Pptx => ".pptx",
                PresentationExtension.Ppsx => ".ppsx",
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }
    }

    public static PresentationExtension FromFileExtension(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".potx" => PresentationExtension.Potx,
            ".pptx" => PresentationExtension.Pptx,
            ".ppsx" => PresentationExtension.Ppsx,
            _ => throw new ArgumentException($"Unsupported file extension: {fileExtension}", nameof(fileExtension))
        };
    }
}