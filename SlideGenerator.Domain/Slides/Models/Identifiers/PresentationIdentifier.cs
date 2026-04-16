namespace SlideGenerator.Domain.Slides.Models.Identifiers;

public sealed record PresentationIdentifier(string FilePath)
{
    public string FilePath { get; init; } = !string.IsNullOrWhiteSpace(FilePath)
        ? FilePath
        : throw new ArgumentException("File path cannot be null or whitespace.", nameof(FilePath));

    public SlideIdentifier GetSlide(int index)
    {
        return new SlideIdentifier(this, index);
    }
}