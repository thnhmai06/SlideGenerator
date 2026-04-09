namespace SlideGenerator.Domain.Workflows.Models.Generating.Texts;

/// <param name="Placeholder">Placeholder token to replace in slide content.</param>
public abstract record Instruction(string Placeholder);