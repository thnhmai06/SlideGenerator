using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Domain.Workflows.Models.Generating.Images;

public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit);