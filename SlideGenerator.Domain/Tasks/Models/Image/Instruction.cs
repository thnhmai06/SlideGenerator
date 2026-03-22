using SlideGenerator.Domain.Tasks.Models.Image.Edits;
using SlideGenerator.Domain.Tasks.Models.Slide;

namespace SlideGenerator.Domain.Tasks.Models.Image;

public abstract record Instruction(ShapeIdentifier Target, EditOptions Edit);