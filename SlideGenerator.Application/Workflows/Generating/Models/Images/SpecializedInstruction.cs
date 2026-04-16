using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Models.Images;

public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit);