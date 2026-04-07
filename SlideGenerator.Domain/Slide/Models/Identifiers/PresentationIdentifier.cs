namespace SlideGenerator.Domain.Slide.Models.Identifiers;

public sealed record PresentationIdentifier(string FilePath)
{
    public string FilePath { get; init; } = !string.IsNullOrWhiteSpace(FilePath)
        ? FilePath
        : throw new ArgumentException("File path cannot be null or whitespace.", nameof(FilePath));
    
    public SlideIdentifier GetSlide(int index) => new(this, index);
}