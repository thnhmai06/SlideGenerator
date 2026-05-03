namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record ShapeIdentifier(string PresentationPath, int SlideIndex, string ShapeName, string? PresentationPassword = null)
    : SlideIdentifier(PresentationPath, SlideIndex, PresentationPassword);