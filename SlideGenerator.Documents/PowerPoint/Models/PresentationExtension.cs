namespace SlideGenerator.Documents.PowerPoint.Models;

/// <summary>
///     Specifies the supported file extensions for presentations.
/// </summary>
public enum PresentationExtension
{
    /// <summary>PowerPoint Template (.potx)</summary>
    Potx,

    /// <summary>Standard PowerPoint Presentation (.pptx)</summary>
    Pptx,

    /// <summary>PowerPoint Slideshow (.ppsx)</summary>
    Ppsx
}

/// <summary>
///     Provides extension methods and utilities for <see cref="PresentationExtension" />.
/// </summary>
public static class PresentationExtensions
{
    public static IReadOnlySet<PresentationExtension> InputExtensions { get; } = new HashSet<PresentationExtension>
    {
        PresentationExtension.Pptx,
        PresentationExtension.Potx
    };

    public static IReadOnlySet<PresentationExtension> OutputExtensions { get; } = new HashSet<PresentationExtension>
    {
        PresentationExtension.Pptx,
        PresentationExtension.Ppsx
    };

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

    public static string ToFileExtension(this PresentationExtension extension)
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
