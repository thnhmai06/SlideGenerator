namespace SlideGenerator.Domain.Slides.Rules;

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
    /// <summary>Gets the set of supported input presentation extensions.</summary>
    public static IReadOnlySet<PresentationExtension> InputExtensions { get; } = new HashSet<PresentationExtension>
    {
        PresentationExtension.Pptx,
        PresentationExtension.Potx
    };

    /// <summary>Gets the set of supported output presentation extensions.</summary>
    public static IReadOnlySet<PresentationExtension> OutputExtensions { get; } = new HashSet<PresentationExtension>
    {
        PresentationExtension.Pptx,
        PresentationExtension.Ppsx
    };

    /// <summary>
    ///     Parses a string file extension into a <see cref="PresentationExtension" /> enum value.
    /// </summary>
    /// <param name="fileExtension">The string file extension (e.g., ".pptx").</param>
    /// <returns>The corresponding <see cref="PresentationExtension" />.</returns>
    /// <exception cref="ArgumentException">Thrown when the file extension is unsupported.</exception>
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

    /// <summary>
    ///     Provides instance methods for <see cref="PresentationExtension" /> values.
    /// </summary>
    extension(PresentationExtension extension)
    {
        /// <summary>
        ///     Converts the enum value to its corresponding string file extension.
        /// </summary>
        /// <returns>A lowercase string file extension including the dot (e.g., ".pptx").</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the enum value is out of range.</exception>
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
}