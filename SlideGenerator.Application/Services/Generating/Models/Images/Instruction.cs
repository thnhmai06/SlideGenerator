using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);