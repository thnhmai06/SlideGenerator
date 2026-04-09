using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Domain.Workflows.Models.Generating.Images;

public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);