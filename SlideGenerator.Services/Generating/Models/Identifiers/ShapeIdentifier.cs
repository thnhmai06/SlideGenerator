namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record ShapeIdentifier(string PresentationFilePath, uint SlideIndex, uint ShapeId)
    : SlideIdentifier(PresentationFilePath, SlideIndex);