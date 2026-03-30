using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Domain.Tasks.Models.Image;

public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);