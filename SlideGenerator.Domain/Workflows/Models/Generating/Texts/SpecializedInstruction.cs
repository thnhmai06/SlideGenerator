using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Domain.Workflows.Models.Generating.Texts;

public record SpecializedInstruction(string Placeholder, ColumnIdentifier Source);