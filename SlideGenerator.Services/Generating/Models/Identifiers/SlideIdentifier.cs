namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record SlideIdentifier(string PresentationPath, int SlideIndex, string? PresentationPassword = null)
    : PresentationIdentifier(PresentationPath, PresentationPassword)
{
    public int SlideIndex { get; init; } = Math.Max(1, SlideIndex);
}
