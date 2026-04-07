using SlideGenerator.Domain.Sheet.Models;

namespace SlideGenerator.Domain.Tasks.Models.Generation.Text;

public record SpecializedInstruction(string Placeholder, ColumnIdentifier Source);