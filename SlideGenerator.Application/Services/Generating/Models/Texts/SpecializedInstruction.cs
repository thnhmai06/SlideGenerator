namespace SlideGenerator.Application.Services.Generating.Models.Texts;

/// <summary>
///     Represents a specialized text instruction containing the actual value to be replaced.
/// </summary>
/// <param name="Placeholder">The placeholder string to find and replace.</param>
/// <param name="Value">The raw string value to be used for replacement.</param>
public record SpecializedInstruction(string Placeholder, string Value);