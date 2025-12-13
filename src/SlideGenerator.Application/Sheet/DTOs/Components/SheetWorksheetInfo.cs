namespace SlideGenerator.Application.Sheet.DTOs.Components;

public record SheetWorksheetInfo(string Name, List<string?> Headers, int RowCount);