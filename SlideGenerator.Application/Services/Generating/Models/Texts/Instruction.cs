namespace SlideGenerator.Application.Services.Generating.Models.Texts;

/// <summary>
///     Represents the base instruction for a text replacement.
/// </summary>
/// <param name="Placeholder">Placeholder token to replace in slide content.</param>
public abstract record Instruction(string Placeholder);
