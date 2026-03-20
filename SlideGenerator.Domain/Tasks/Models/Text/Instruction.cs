namespace SlideGenerator.Domain.Tasks.Models.Text;

/// <param name="Placeholder">Placeholder token to replace in slide content.</param>
public abstract record Instruction(string Placeholder);