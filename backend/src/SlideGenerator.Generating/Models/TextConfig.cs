namespace SlideGenerator.Generating.Models;

/// <summary>
///     Represents a text binding configuration for replacement.
/// </summary>
/// <param name="Placeholder">Placeholder token to replace in slide content.</param>
/// <param name="Columns">Candidate columns used to resolve replacement value.</param>
public sealed record TextConfig(string Placeholder, IReadOnlyList<string> Columns);