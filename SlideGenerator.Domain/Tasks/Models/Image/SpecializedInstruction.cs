using SlideGenerator.Domain.Tasks.Models.Image.Edits;
using SlideGenerator.Domain.Tasks.Models.Sheet;
using SlideGenerator.Domain.Tasks.Models.Slide;

namespace SlideGenerator.Domain.Tasks.Models.Image;

public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit);