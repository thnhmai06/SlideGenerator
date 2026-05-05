namespace SlideGenerator.Documents.Slides.Models;

/// <summary>
///     Specifies the supported file extensions for presentations.
/// </summary>
public enum PresentationType
{
    /// <summary>PowerPoint Template (.potx)</summary>
    Potx,

    /// <summary>Standard PowerPoint Presentation (.pptx)</summary>
    Pptx,

    /// <summary>PowerPoint Slideshow (.ppsx)</summary>
    Ppsx
}

/// <summary>
///     Provides extension methods and utilities for <see cref="PresentationType" />.
/// </summary>
public static class PresentationTypeExtensions
{
    public static PresentationType FromExtension(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".potx" => PresentationType.Potx,
            ".pptx" => PresentationType.Pptx,
            ".ppsx" => PresentationType.Ppsx,
            _ => throw new ArgumentException($"Unsupported file extension: {fileExtension}", nameof(fileExtension))
        };
    }

    public static string ToExtension(this PresentationType type)
    {
        return type switch
        {
            PresentationType.Potx => ".potx",
            PresentationType.Pptx => ".pptx",
            PresentationType.Ppsx => ".ppsx",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
