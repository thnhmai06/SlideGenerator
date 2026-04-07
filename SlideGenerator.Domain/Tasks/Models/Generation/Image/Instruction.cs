using SlideGenerator.Domain.Slide.Models.Identifiers;

namespace SlideGenerator.Domain.Tasks.Models.Generation.Image;

public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);