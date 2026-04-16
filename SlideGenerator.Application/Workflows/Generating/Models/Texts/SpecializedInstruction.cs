using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Workflows.Generating.Models.Texts;

public record SpecializedInstruction(string Placeholder, ColumnIdentifier Source);