namespace SlideGenerator.Generating.Models;

/// <summary>
///     Represents spreadsheet input source and optional selected sheets.
/// </summary>
/// <param name="FilePath">Primary spreadsheet file path.</param>
/// <param name="SelectedSheets">Selected sheets to process (<see langword="null"/> means all sheets).</param>
public sealed record SheetConfig(string FilePath, IReadOnlyList<string>? SelectedSheets);
