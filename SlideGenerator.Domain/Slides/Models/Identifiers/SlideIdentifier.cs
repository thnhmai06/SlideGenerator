namespace SlideGenerator.Domain.Slides.Models.Identifiers;

public sealed record SlideIdentifier(PresentationIdentifier Presentation, int Index)
{
    public int Index { get; init; } = Index > 0 
        ? Index : throw new ArgumentOutOfRangeException(nameof(Index), "Slide index must be greater than 0.");
    
    public ShapeIdentifier GetShape(uint id) => new(this, id);
}