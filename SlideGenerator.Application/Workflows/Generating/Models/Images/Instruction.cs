using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Models.Images;

public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);