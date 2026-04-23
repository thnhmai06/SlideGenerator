using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Models.Texts;

/// <summary>
///     Represents a specialized text instruction linking a placeholder to a specific column source.
/// </summary>
/// <param name="Placeholder">The placeholder string to replace.</param>
/// <param name="Source">The column identifier acting as the data source.</param>
public record SpecializedInstruction(string Placeholder, ColumnIdentifier Source);
