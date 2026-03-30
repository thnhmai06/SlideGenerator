using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Domain.Tasks.Models.Image;

public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit);