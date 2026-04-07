using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Models.Identifiers;

namespace SlideGenerator.Domain.Tasks.Models.Generation.Image;

public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit);